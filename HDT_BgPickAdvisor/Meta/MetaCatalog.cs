using System;
using System.IO;
using System.Reflection;

namespace HDT_BgPickAdvisor.Meta
{
    /// <summary>BgMetaApi endpoints for plugin meta.</summary>
    internal static class MetaCatalog
    {
        public const string ApiUrlFileName = "meta-api.url";

        /// <summary>Production BgMetaApi (override via meta-api.url or BGMETA_API_URL).</summary>
        public const string DefaultApiBaseUrl = "http://hsbg.qxplays.ru";

        public static string PluginDirectory
        {
            get
            {
                try
                {
                    var loc = Assembly.GetExecutingAssembly().Location;
                    return string.IsNullOrEmpty(loc) ? "" : Path.GetDirectoryName(loc);
                }
                catch
                {
                    return "";
                }
            }
        }

        public static string ApiBaseUrl => ResolveApiBaseUrl();

        public static string HeroesUrl => ApiBaseUrl + "/api/meta/heroes";

        public static string TrinketsUrl => ApiBaseUrl + "/api/meta/trinkets";

        public static string ApiUrlFilePath =>
            string.IsNullOrEmpty(PluginDirectory) ? null : Path.Combine(PluginDirectory, ApiUrlFileName);

        public static bool IsRemoteConfigured =>
            !string.IsNullOrWhiteSpace(ApiBaseUrl) && !IsPlaceholderUrl(ApiBaseUrl);

        public static bool IsPlaceholderUrl(string url) =>
            string.IsNullOrWhiteSpace(url) ||
            url.IndexOf("your-meta-server", StringComparison.OrdinalIgnoreCase) >= 0 ||
            url.IndexOf("example.com", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string ResolveApiBaseUrl()
        {
            try
            {
                var env = Environment.GetEnvironmentVariable("BGMETA_API_URL");
                if (!string.IsNullOrWhiteSpace(env))
                    return env.Trim().TrimEnd('/');

                var path = ApiUrlFilePath;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var line = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(line))
                        return line.TrimEnd('/');
                }
            }
            catch
            {
                // ignored
            }

            return DefaultApiBaseUrl;
        }
    }
}
