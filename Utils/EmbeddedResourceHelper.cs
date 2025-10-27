using System.IO;
using System.Linq;
using System.Reflection;

namespace SharedCockpitClient.Utils
{
    public static class EmbeddedResourceHelper
    {
        private const string ResourcePrefix = "SharedCockpitClient.SDKResources.";

        public static Stream? GetResource(string relativePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = ResourcePrefix + relativePath.Replace("\\", ".").Replace("/", ".");
            return assembly.GetManifestResourceStream(resourceName);
        }

        public static string[] ListResources(string? extensionFilter = null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly
                .GetManifestResourceNames()
                .Where(n => n.StartsWith(ResourcePrefix))
                .ToArray();

            if (string.IsNullOrWhiteSpace(extensionFilter))
            {
                return names;
            }

            extensionFilter = extensionFilter.StartsWith('.') ? extensionFilter : "." + extensionFilter;
            return names
                .Where(name => name.EndsWith(extensionFilter, System.StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}
