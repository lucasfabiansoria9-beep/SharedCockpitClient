using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Tools
{
    public sealed record SimVarCatalog(IReadOnlyList<SimVarDescriptor> SimVars, IReadOnlyList<SimEventDescriptor> SimEvents)
    {
        public static readonly SimVarCatalog Empty = new(Array.Empty<SimVarDescriptor>(), Array.Empty<SimEventDescriptor>());
    }

    public static class SimVarCatalogGenerator
    {
        private const string ResourcePrefix = "SharedCockpitClient.SDKResources.";

        private static readonly Lazy<SimVarCatalog?> s_cachedCatalog = new(GenerateInternal, isThreadSafe: true);

        private static readonly Regex sHeaderEventRegex = new(@"K:[A-Z0-9_]+", RegexOptions.Compiled);
        private static readonly Regex sHeaderTokenRegex = new(@"(?<![A-Z0-9_])([A-Z][A-Z0-9_]*_EVENT_[A-Z0-9_]+)(?![A-Z0-9_])", RegexOptions.Compiled);

        public static bool TryGetCatalog(out SimVarCatalog catalog)
        {
            var value = s_cachedCatalog.Value;
            if (value is null)
            {
                catalog = SimVarCatalog.Empty;
                return false;
            }

            catalog = value;
            return catalog.SimVars.Count > 0 || catalog.SimEvents.Count > 0;
        }

        public static string GenerateJson(bool indented = true)
        {
            if (!TryGetCatalog(out var catalog))
            {
                catalog = SimVarCatalog.Empty;
            }

            var payload = new
            {
                SimVars = catalog.SimVars.Select(v => new
                {
                    v.Path,
                    v.Name,
                    v.Units,
                    DataType = v.DataType.ToString().ToUpperInvariant(),
                    v.Writable,
                    v.Category,
                    v.Index,
                    v.EventWrite,
                    v.MinDelta
                }),
                SimEvents = catalog.SimEvents.Select(e => new
                {
                    e.Path,
                    e.EventName,
                    e.Category
                })
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = indented
            });
        }

        private static SimVarCatalog? GenerateInternal()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = EmbeddedResourceHelper.ListResources();
                if (resourceNames.Length == 0)
                {
                    return SimVarCatalog.Empty;
                }

                var simVars = new Dictionary<string, SimVarDescriptor>(StringComparer.OrdinalIgnoreCase);
                var simEvents = new Dictionary<string, SimEventDescriptor>(StringComparer.OrdinalIgnoreCase);

                foreach (var resourceName in resourceNames)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream is null)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(resourceName);
                    if (string.IsNullOrEmpty(extension))
                    {
                        continue;
                    }

                    extension = extension.ToLowerInvariant();
                    switch (extension)
                    {
                        case ".xml":
                            ParseXml(stream, resourceName, simVars, simEvents);
                            break;
                        case ".json":
                            ParseJson(stream, resourceName, simVars, simEvents);
                            break;
                        case ".h":
                            ParseHeader(stream, resourceName, simEvents);
                            break;
                    }
                }

                return new SimVarCatalog(simVars.Values.ToList(), simEvents.Values.ToList());
            }
            catch
            {
                return null;
            }
        }

        private static void ParseXml(Stream stream, string resourceName, IDictionary<string, SimVarDescriptor> simVars, IDictionary<string, SimEventDescriptor> simEvents)
        {
            XDocument? document;
            try
            {
                document = XDocument.Load(stream);
            }
            catch
            {
                return;
            }

            foreach (var element in document.Descendants())
            {
                var nameAttr = GetAttribute(element, "name");
                if (string.IsNullOrWhiteSpace(nameAttr))
                {
                    continue;
                }

                if (LooksLikeEvent(element))
                {
                    var eventId = GetAttribute(element, "id") ?? GetAttribute(element, "event") ?? nameAttr;
                    var category = GetAttribute(element, "category") ?? GuessCategory(resourceName);
                    var path = $"SimEvents.{eventId}";
                    if (!simEvents.ContainsKey(path))
                    {
                        simEvents[path] = new SimEventDescriptor(path, NormalizeEventName(eventId), category);
                    }

                    continue;
                }

                var units = GetAttribute(element, "units") ?? GetAttribute(element, "unit") ?? string.Empty;
                var typeStr = GetAttribute(element, "type") ?? GetAttribute(element, "datatype");
                var categoryAttr = GetAttribute(element, "category") ?? GuessCategory(resourceName);
                var writableAttr = GetAttribute(element, "writable") ?? GetAttribute(element, "settable");
                var eventWrite = GetAttribute(element, "eventwrite") ?? GetAttribute(element, "event") ?? GetAttribute(element, "setevent");
                var minDeltaAttr = GetAttribute(element, "mindelta");

                var descriptor = CreateDescriptorFromMetadata(nameAttr, units, typeStr, writableAttr, categoryAttr, eventWrite, minDeltaAttr);
                var path = descriptor.Path;
                if (!simVars.ContainsKey(path))
                {
                    simVars[path] = descriptor;
                }
            }
        }

        private static void ParseJson(Stream stream, string resourceName, IDictionary<string, SimVarDescriptor> simVars, IDictionary<string, SimEventDescriptor> simEvents)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(stream);
            }
            catch
            {
                return;
            }

            using (document)
            {
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        ReadJsonEntry(element, resourceName, simVars, simEvents);
                    }
                }
                else if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (document.RootElement.TryGetProperty("SimVars", out var varsElement) && varsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in varsElement.EnumerateArray())
                        {
                            ReadJsonEntry(element, resourceName, simVars, simEvents);
                        }
                    }

                    if (document.RootElement.TryGetProperty("SimEvents", out var eventsElement) && eventsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in eventsElement.EnumerateArray())
                        {
                            var name = element.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;
                            name ??= element.TryGetProperty("event", out var eventProperty) ? eventProperty.GetString() : null;
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }

                            var category = element.TryGetProperty("category", out var categoryProp)
                                ? categoryProp.GetString() ?? GuessCategory(resourceName)
                                : GuessCategory(resourceName);
                            var path = element.TryGetProperty("path", out var pathProperty) && pathProperty.ValueKind == JsonValueKind.String
                                ? pathProperty.GetString()
                                : $"SimEvents.{name}";

                            if (string.IsNullOrWhiteSpace(path))
                            {
                                path = $"SimEvents.{name}";
                            }

                            if (!simEvents.ContainsKey(path))
                            {
                                simEvents[path] = new SimEventDescriptor(path, NormalizeEventName(name), category);
                            }
                        }
                    }
                }
            }
        }

        private static void ReadJsonEntry(JsonElement element, string resourceName, IDictionary<string, SimVarDescriptor> simVars, IDictionary<string, SimEventDescriptor> simEvents)
        {
            var name = element.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (element.TryGetProperty("event", out var eventProperty) && eventProperty.ValueKind == JsonValueKind.String)
            {
                var eventId = eventProperty.GetString();
                if (!string.IsNullOrWhiteSpace(eventId))
                {
                    var category = element.TryGetProperty("category", out var categoryProp)
                        ? categoryProp.GetString() ?? GuessCategory(resourceName)
                        : GuessCategory(resourceName);
                    var path = element.TryGetProperty("path", out var pathProp) && pathProp.ValueKind == JsonValueKind.String
                        ? pathProp.GetString()
                        : $"SimEvents.{name}";
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        path = $"SimEvents.{name}";
                    }

                    if (!simEvents.ContainsKey(path))
                    {
                        simEvents[path] = new SimEventDescriptor(path, NormalizeEventName(eventId!), category);
                    }
                    return;
                }
            }

            var units = element.TryGetProperty("unit", out var unitProperty)
                ? unitProperty.GetString()
                : element.TryGetProperty("units", out var unitsProperty) ? unitsProperty.GetString() : string.Empty;
            var type = element.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
            var writableValue = element.TryGetProperty("writable", out var writableProperty) && writableProperty.ValueKind == JsonValueKind.True || (writableProperty.ValueKind == JsonValueKind.String && bool.TryParse(writableProperty.GetString(), out var b) && b);
            if (!writableValue && element.TryGetProperty("settable", out var settableProperty))
            {
                writableValue = settableProperty.ValueKind == JsonValueKind.True || (settableProperty.ValueKind == JsonValueKind.String && bool.TryParse(settableProperty.GetString(), out var b) && b);
            }

            var category = element.TryGetProperty("category", out var categoryProperty)
                ? categoryProperty.GetString() ?? GuessCategory(resourceName)
                : GuessCategory(resourceName);
            var eventWrite = element.TryGetProperty("eventWrite", out var eventWriteProperty)
                ? eventWriteProperty.GetString()
                : element.TryGetProperty("event", out var eventPropForWrite) ? eventPropForWrite.GetString() : null;
            var minDelta = element.TryGetProperty("minDelta", out var minDeltaProperty)
                ? minDeltaProperty.GetDouble()
                : (double?)null;
            int? index = null;
            if (element.TryGetProperty("index", out var indexProperty) && indexProperty.ValueKind == JsonValueKind.Number)
            {
                index = indexProperty.GetInt32();
            }

            if (element.TryGetProperty("path", out var pathProperty) && pathProperty.ValueKind == JsonValueKind.String)
            {
                var customPath = pathProperty.GetString();
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    var descriptor = new SimVarDescriptor(customPath, name!, units ?? string.Empty, ParseDataType(type), writableValue, category, index, eventWrite, minDelta);
                    simVars[descriptor.Path] = descriptor;
                    return;
                }
            }

            var metadataDescriptor = CreateDescriptorFromMetadata(name!, units ?? string.Empty, type, writableValue ? "true" : null, category, eventWrite, minDelta?.ToString(CultureInfo.InvariantCulture));
            simVars[metadataDescriptor.Path] = metadataDescriptor;
        }

        private static void ParseHeader(Stream stream, string resourceName, IDictionary<string, SimEventDescriptor> simEvents)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var content = reader.ReadToEnd();

            foreach (Match match in sHeaderEventRegex.Matches(content))
            {
                var eventName = match.Value;
                var path = $"SimEvents.{eventName.Replace(':', '_')}";
                if (!simEvents.ContainsKey(path))
                {
                    simEvents[path] = new SimEventDescriptor(path, eventName, GuessCategory(resourceName));
                }
            }

            foreach (Match match in sHeaderTokenRegex.Matches(content))
            {
                var token = match.Groups[1].Value;
                var path = $"SimEvents.{token}";
                if (!simEvents.ContainsKey(path))
                {
                    simEvents[path] = new SimEventDescriptor(path, NormalizeEventName(token), GuessCategory(resourceName));
                }
            }
        }

        private static SimVarDescriptor CreateDescriptorFromMetadata(string name, string? units, string? type, string? writable, string? category, string? eventWrite, string? minDelta)
        {
            var descriptorName = name.Trim();
            var descriptorPath = $"SimVars.{descriptorName}";
            int? index = ParseIndex(descriptorName);
            var dataType = ParseDataType(type);
            var canWrite = ParseBool(writable);
            var delta = ParseDouble(minDelta);
            var descriptorCategory = string.IsNullOrWhiteSpace(category) ? "General" : category!;

            return new SimVarDescriptor(descriptorPath, descriptorName, units ?? string.Empty, dataType, canWrite, descriptorCategory, index, eventWrite, delta);
        }

        private static bool ParseBool(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("settable", StringComparison.OrdinalIgnoreCase);
        }

        private static double? ParseDouble(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
        }

        private static int? ParseIndex(string name)
        {
            var colonIndex = name.LastIndexOf(':');
            if (colonIndex >= 0 && colonIndex < name.Length - 1)
            {
                var segment = name[(colonIndex + 1)..];
                if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }
            }

            return null;
        }

        private static SimDataType ParseDataType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return SimDataType.Float64;
            }

            return type.Trim().ToUpperInvariant() switch
            {
                "FLOAT32" or "FLOAT" or "FLOAT32LE" => SimDataType.Float32,
                "INT32" or "INT" or "INT16" or "ENUM" => SimDataType.Int32,
                "BOOL" or "BOOLEAN" => SimDataType.Bool,
                "STRING" or "STRING256" or "STRING128" or "STRING64" => SimDataType.String256,
                _ => SimDataType.Float64
            };
        }

        private static bool LooksLikeEvent(XElement element)
        {
            var name = element.Name.LocalName;
            if (name.Contains("event", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (element.Attributes().Any(a => a.Name.LocalName.Contains("event", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static string GuessCategory(string resourceName)
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                return "General";
            }

            var trimmed = resourceName[ResourcePrefix.Length..];
            var firstSeparator = trimmed.IndexOf('.');
            if (firstSeparator < 0)
            {
                return "General";
            }

            var segment = trimmed[..firstSeparator];
            return string.IsNullOrWhiteSpace(segment) ? "General" : segment;
        }

        private static string NormalizeEventName(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return eventId;
            }

            return eventId.StartsWith("K:", StringComparison.OrdinalIgnoreCase) ? eventId : $"K:{eventId}";
        }

        private static string? GetAttribute(XElement element, string name)
        {
            var attribute = element.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            return attribute?.Value;
        }
    }
}
