using System.Text.Json;
using BgMetaApi.Configuration;
using Microsoft.Extensions.Options;

namespace BgMetaApi.Services;

public sealed class MetaFileStore
{
    private readonly string _heroesPath;
    private readonly string _trinketsPath;
    private readonly string _dataDir;

    public MetaFileStore(IOptions<MetaStorageOptions> options, IWebHostEnvironment env)
    {
        var opt = options.Value;
        _dataDir = Path.GetFullPath(
            string.IsNullOrWhiteSpace(opt.DataDirectory)
                ? Path.Combine(env.ContentRootPath, "data")
                : opt.DataDirectory);

        Directory.CreateDirectory(_dataDir);
        _heroesPath = Path.Combine(_dataDir, opt.HeroesFileName);
        _trinketsPath = Path.Combine(_dataDir, opt.TrinketsFileName);
    }

    public string DataDirectory => _dataDir;

    public MetaFileInfo GetHeroesInfo() => GetInfo(_heroesPath);

    public MetaFileInfo GetTrinketsInfo() => GetInfo(_trinketsPath);

    public async Task<string?> ReadHeroesJsonAsync(CancellationToken ct = default) =>
        await ReadJsonAsync(_heroesPath, ct);

    public async Task<string?> ReadTrinketsJsonAsync(CancellationToken ct = default) =>
        await ReadJsonAsync(_trinketsPath, ct);

    public async Task<MetaUploadResult> SaveHeroesAsync(string json, CancellationToken ct = default) =>
        await SaveAsync(_heroesPath, "heroes", json, ct);

    public async Task<MetaUploadResult> SaveTrinketsAsync(string json, CancellationToken ct = default) =>
        await SaveAsync(_trinketsPath, "trinkets", json, ct);

    private static MetaFileInfo GetInfo(string path)
    {
        if (!File.Exists(path))
            return new MetaFileInfo { Exists = false, Path = path };

        var fi = new FileInfo(path);
        return new MetaFileInfo
        {
            Exists = true,
            Path = path,
            SizeBytes = fi.Length,
            UpdatedUtc = fi.LastWriteTimeUtc
        };
    }

    private static async Task<string?> ReadJsonAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            return null;

        return await File.ReadAllTextAsync(path, ct);
    }

    private async Task<MetaUploadResult> SaveAsync(string path, string kind, string json, CancellationToken ct)
    {
        var validation = MetaJsonValidator.Validate(kind, json);
        if (!validation.Success)
            return MetaUploadResult.Fail(validation.Error!);

        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, json, ct);
        File.Move(tmp, path, overwrite: true);

        var info = GetInfo(path);
        return MetaUploadResult.Ok(info.SizeBytes, info.UpdatedUtc ?? DateTime.UtcNow);
    }
}

public sealed class MetaFileInfo
{
    public bool Exists { get; set; }
    public string Path { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}

public sealed class MetaUploadResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long SizeBytes { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public static MetaUploadResult Ok(long size, DateTime updated) =>
        new() { Success = true, SizeBytes = size, UpdatedUtc = updated };

    public static MetaUploadResult Fail(string error) =>
        new() { Success = false, Error = error };
}
