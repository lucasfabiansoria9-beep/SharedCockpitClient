#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharedCockpitClient
{
    public static class EmbeddedResourceHelper
    {
        private const string ResourcePrefix = "SharedCockpitClient.SDKResources.";

        public static Stream? GetResource(string relativePath)
        {
            var resourceName = ResourcePrefix + relativePath.Replace("\\", ".").Replace("/", ".");
            foreach (var assembly in EnumerateAssemblies())
            {
                try
                {
                    var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                        return stream;
                }
                catch
                {
                    // Ignorar ensamblados problemáticos
                }
            }

            return null;
        }

        public static string[] ListResources(string? extensionFilter = null)
        {
            var names = new List<string>();
            foreach (var assembly in EnumerateAssemblies())
            {
                try
                {
                    names.AddRange(assembly
                        .GetManifestResourceNames()
                        .Where(n => n.StartsWith(ResourcePrefix)));
                }
                catch
                {
                    // Ignorar ensamblados no accesibles
                }
            }

            if (string.IsNullOrWhiteSpace(extensionFilter))
            {
                return names.ToArray();
            }

            extensionFilter = extensionFilter.StartsWith('.') ? extensionFilter : "." + extensionFilter;
            return names
                .Where(name => name.EndsWith(extensionFilter, System.StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static IEnumerable<Assembly> EnumerateAssemblies()
        {
            var executing = Assembly.GetExecutingAssembly();
            yield return executing;

            if (!AppEnvironment.EnableAssemblyScanCatalog)
            {
                // Catálogo deshabilitado por defecto para evitar reflejar assemblies externos
                yield break;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null || ReferenceEquals(assembly, executing))
                    continue;

                Assembly? target = null;
                try
                {
                    if (ShouldScan(assembly))
                        target = assembly;
                }
                catch
                {
                    target = null;
                }

                if (target != null)
                    yield return target;
            }
        }

        private static bool ShouldScan(Assembly asm)
        {
            var name = asm.GetName().Name ?? string.Empty;
            if (name.StartsWith("SharedCockpitClient", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.Equals("Microsoft.FlightSimulator.SimConnect", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.StartsWith("Microsoft.WindowsAPICodePack", StringComparison.OrdinalIgnoreCase))
                return false;
            if (name.StartsWith("SharpDX", StringComparison.OrdinalIgnoreCase))
                return false;
            if (name.StartsWith("Xceed", StringComparison.OrdinalIgnoreCase))
                return false;
            if (name.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase))
                return false;
            return false;
        }
    }
}
