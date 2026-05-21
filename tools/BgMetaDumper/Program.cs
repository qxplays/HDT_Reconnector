using System;
using System.IO;
using System.Linq;
using System.Text;

namespace BgMetaDumper
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static int Run(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "help")
            {
                PrintHelp();
                return 0;
            }

            var cmd = args[0].ToLowerInvariant();
            switch (cmd)
            {
                case "paths":
                    PrintPaths();
                    return 0;
                case "upload":
                    return Upload(args.Skip(1).ToArray());
                default:
                    Console.Error.WriteLine($"Unknown command: {cmd}");
                    PrintHelp();
                    return 1;
            }
        }

        private static int Upload(string[] args)
        {
            var dir = ReadPathArg(args) ?? Environment.CurrentDirectory;
            var api = ReadStringFlag(args, "--api") ?? MetaApiUploader.DefaultApiUrl;
            var user = ReadStringFlag(args, "--user") ?? MetaApiUploader.DefaultUser;
            var pass = ReadStringFlag(args, "--password") ?? MetaApiUploader.DefaultPassword;

            if (!Directory.Exists(dir))
            {
                Console.Error.WriteLine($"Directory not found: {dir}");
                return 1;
            }

            return MetaApiUploader.UploadDirectory(dir, api, user, pass);
        }

        private static void PrintPaths()
        {
            Console.WriteLine($"API (default): {MetaApiUploader.DefaultApiUrl}");
            Console.WriteLine("Env: BGMETA_API_URL, META_ADMIN_USERNAME, META_ADMIN_PASSWORD");
            Console.WriteLine();
            Console.WriteLine("Upload expects in folder:");
            Console.WriteLine("  heroes.json");
            Console.WriteLine("  trinkets.json");
        }

        private static void PrintHelp()
        {
            var help = new StringBuilder();
            help.AppendLine("BgMetaDumper — upload heroes.json / trinkets.json to BgMetaApi");
            help.AppendLine();
            help.AppendLine("Commands:");
            help.AppendLine("  paths");
            help.AppendLine("  upload [folder] [--api url] [--user name] [--password pass]");
            help.AppendLine();
            help.AppendLine("Examples:");
            help.AppendLine("  BgMetaDumper upload C:\\meta-dumps");
            help.AppendLine("  BgMetaDumper upload --api http://192.168.1.10:5080");
            Console.WriteLine(help);
        }

        private static string ReadPathArg(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-", StringComparison.Ordinal))
                {
                    if (IsFlagWithValue(args, i, "--api", "--user", "--password"))
                        i++;
                    continue;
                }

                return args[i];
            }

            return null;
        }

        private static string ReadStringFlag(string[] args, string flag)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        private static bool IsFlagWithValue(string[] args, int index, params string[] flags)
        {
            foreach (var flag in flags)
            {
                if (string.Equals(args[index], flag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
