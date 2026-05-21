using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace BgMetaDumper
{
    internal static class MetaApiUploader
    {
        public static string DefaultApiUrl =>
            Environment.GetEnvironmentVariable("BGMETA_API_URL") ?? "http://hsbg.qxplays.ru";

        public static string DefaultUser =>
            Environment.GetEnvironmentVariable("META_ADMIN_USERNAME") ?? "admin";

        public static string DefaultPassword =>
            Environment.GetEnvironmentVariable("META_ADMIN_PASSWORD") ?? "admin";

        public static int UploadDirectory(string dir, string apiUrl, string user, string password)
        {
            var heroes = Path.Combine(dir, "heroes.json");
            var trinkets = Path.Combine(dir, "trinkets.json");
            if (!File.Exists(heroes) || !File.Exists(trinkets))
            {
                Console.Error.WriteLine("Expected heroes.json and trinkets.json in:");
                Console.Error.WriteLine(dir);
                return 1;
            }

            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) })
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

                UploadFile(http, apiUrl.TrimEnd('/') + "/api/upload/heroes", heroes, "heroes");
                UploadFile(http, apiUrl.TrimEnd('/') + "/api/upload/trinkets", trinkets, "trinkets");
            }

            Console.WriteLine("Done. Plugin: Debug offers → Reload meta (or restart HDT).");
            return 0;
        }

        private static void UploadFile(HttpClient http, string url, string path, string label)
        {
            var bytes = File.ReadAllBytes(path);
            using (var content = new MultipartFormDataContent())
            {
                var file = new ByteArrayContent(bytes);
                file.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Add(file, "file", Path.GetFileName(path));

                Console.WriteLine($"POST {label} ({bytes.Length} bytes) → {url}");
                var response = http.PostAsync(url, content).GetAwaiter().GetResult();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine(response.IsSuccessStatusCode
                    ? $"[OK] {label}: {(int)response.StatusCode}"
                    : $"[FAIL] {label}: {(int)response.StatusCode} {body}");
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
