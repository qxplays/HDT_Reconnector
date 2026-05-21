using System.Security.Claims;
using BgMetaApi.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BgMetaApi.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly AdminOptions _admin;

    public LoginModel(IOptions<AdminOptions> admin) => _admin = admin.Value;

    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Admin/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string username, string password)
    {
        if (username != _admin.Username || password != _admin.Password)
        {
            Error = "Invalid username or password";
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Admin/Index");
    }
}
