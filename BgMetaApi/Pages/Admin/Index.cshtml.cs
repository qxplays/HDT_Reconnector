using BgMetaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BgMetaApi.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class IndexModel : PageModel
{
    private readonly MetaFileStore _store;

    public IndexModel(MetaFileStore store) => _store = store;

    public MetaFileInfo Heroes { get; private set; } = new();
    public MetaFileInfo Trinkets { get; private set; } = new();
    public string DataDirectory { get; private set; } = "";
    public string PublicBaseUrl { get; private set; } = "";
    public string? Message { get; private set; }
    public string? Error { get; private set; }

    public void OnGet(string? msg, string? err)
    {
        Refresh();
        Message = msg;
        Error = err;
    }

    public async Task<IActionResult> OnPostUploadHeroesAsync(IFormFile file, CancellationToken ct)
    {
        return await UploadAsync("heroes", file, _store.SaveHeroesAsync, ct);
    }

    public async Task<IActionResult> OnPostUploadTrinketsAsync(IFormFile file, CancellationToken ct)
    {
        return await UploadAsync("trinkets", file, _store.SaveTrinketsAsync, ct);
    }

    private async Task<IActionResult> UploadAsync(
        string kind,
        IFormFile file,
        Func<string, CancellationToken, Task<MetaUploadResult>> save,
        CancellationToken ct)
    {
        if (file.Length == 0)
            return RedirectToPage(new { err = "Empty file" });

        string json;
        using (var reader = new StreamReader(file.OpenReadStream()))
            json = await reader.ReadToEndAsync(ct);

        var result = await save(json, ct);
        if (!result.Success)
            return RedirectToPage(new { err = result.Error });

        return RedirectToPage(new { msg = $"{kind} uploaded ({result.SizeBytes} bytes)" });
    }

    private void Refresh()
    {
        Heroes = _store.GetHeroesInfo();
        Trinkets = _store.GetTrinketsInfo();
        DataDirectory = _store.DataDirectory;
        PublicBaseUrl = $"{Request.Scheme}://{Request.Host}";
    }
}
