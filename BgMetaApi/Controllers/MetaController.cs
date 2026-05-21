using BgMetaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BgMetaApi.Controllers;

[ApiController]
[Route("api/meta")]
[AllowAnonymous]
public sealed class MetaController : ControllerBase
{
    private readonly MetaFileStore _store;

    public MetaController(MetaFileStore store) => _store = store;

    [HttpGet("heroes")]
    [Produces("application/json")]
    public async Task<IActionResult> GetHeroes(CancellationToken ct)
    {
        var json = await _store.ReadHeroesJsonAsync(ct);
        if (json == null)
            return NotFound(new { error = "heroes.json not uploaded yet" });

        return Content(json, "application/json");
    }

    [HttpGet("trinkets")]
    [Produces("application/json")]
    public async Task<IActionResult> GetTrinkets(CancellationToken ct)
    {
        var json = await _store.ReadTrinketsJsonAsync(ct);
        if (json == null)
            return NotFound(new { error = "trinkets.json not uploaded yet" });

        return Content(json, "application/json");
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        var heroes = _store.GetHeroesInfo();
        var trinkets = _store.GetTrinketsInfo();
        return Ok(new
        {
            dataDirectory = _store.DataDirectory,
            heroes = new { heroes.Exists, heroes.SizeBytes, heroes.UpdatedUtc },
            trinkets = new { trinkets.Exists, trinkets.SizeBytes, trinkets.UpdatedUtc }
        });
    }
}
