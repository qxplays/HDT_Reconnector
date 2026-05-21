using BgMetaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BgMetaApi.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize(AuthenticationSchemes = "Cookies,Basic", Roles = "Admin")]
public sealed class UploadController : ControllerBase
{
    private readonly MetaFileStore _store;

    public UploadController(MetaFileStore store) => _store = store;

    [HttpPost("heroes")]
    [RequestSizeLimit(32 * 1024 * 1024)]
    public async Task<IActionResult> UploadHeroes(CancellationToken ct)
    {
        var json = await ReadJsonBodyAsync(ct);
        if (json == null)
            return BadRequest(new { error = "Send JSON file (multipart field 'file') or raw application/json body" });

        var result = await _store.SaveHeroesAsync(json, ct);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            kind = "heroes",
            sizeBytes = result.SizeBytes,
            updatedUtc = result.UpdatedUtc
        });
    }

    [HttpPost("trinkets")]
    [RequestSizeLimit(32 * 1024 * 1024)]
    public async Task<IActionResult> UploadTrinkets(CancellationToken ct)
    {
        var json = await ReadJsonBodyAsync(ct);
        if (json == null)
            return BadRequest(new { error = "Send JSON file (multipart field 'file') or raw application/json body" });

        var result = await _store.SaveTrinketsAsync(json, ct);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            kind = "trinkets",
            sizeBytes = result.SizeBytes,
            updatedUtc = result.UpdatedUtc
        });
    }

    private async Task<string?> ReadJsonBodyAsync(CancellationToken ct)
    {
        if (Request.HasFormContentType)
        {
            var file = Request.Form.Files.GetFile("file");
            if (file == null || file.Length == 0)
                return null;

            using var reader = new StreamReader(file.OpenReadStream());
            return await reader.ReadToEndAsync(ct);
        }

        if (Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(Request.Body);
            return await reader.ReadToEndAsync(ct);
        }

        return null;
    }
}
