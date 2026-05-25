using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HDT_BgReplay.UI
{
    internal static class CardArtService
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> MemoryCache =
            new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

        private static readonly string DiskCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HearthstoneDeckTracker", "Plugins", "BgReplay", "card-cache");

        public static string GetRenderUrl(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return null;

            return $"https://art.hearthstonejson.com/v1/render/latest/enUS/256x/{cardId}.png";
        }

        public static void LoadPortrait(string cardId, Action<BitmapImage> onReady)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                onReady?.Invoke(null);
                return;
            }

            if (MemoryCache.TryGetValue(cardId, out var cached))
            {
                onReady?.Invoke(cached);
                return;
            }

            var diskPath = Path.Combine(DiskCacheDir, cardId + ".png");
            if (File.Exists(diskPath))
            {
                var fromDisk = LoadBitmap(diskPath, cardId);
                if (fromDisk != null)
                {
                    MemoryCache[cardId] = fromDisk;
                    onReady?.Invoke(fromDisk);
                    return;
                }
            }

            var url = GetRenderUrl(cardId);
            if (string.IsNullOrEmpty(url))
            {
                onReady?.Invoke(null);
                return;
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(DiskCacheDir);
                    using (var client = new WebClient())
                    {
                        var bytes = client.DownloadData(url);
                        File.WriteAllBytes(diskPath, bytes);
                        var img = LoadBitmap(diskPath, cardId);
                        if (img != null)
                            MemoryCache[cardId] = img;

                        Application.Current?.Dispatcher.BeginInvoke(new Action(() => onReady?.Invoke(img)));
                    }
                }
                catch
                {
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() => onReady?.Invoke(null)));
                }
            });
        }

        private static BitmapImage LoadBitmap(string path, string cardId)
        {
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(path, UriKind.Absolute);
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch
            {
                return null;
            }
        }
    }
}
