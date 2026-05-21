using BgMetaApi.Auth;
using BgMetaApi.Configuration;
using BgMetaApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

ConfigureAdminOptions(builder);
ConfigureMetaStorage(builder);

builder.Services.AddSingleton<MetaFileStore>();
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        "Basic", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
    options.AddPolicy("Admin", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, "Basic");
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Admin"));

app.Run();

static void ConfigureAdminOptions(WebApplicationBuilder builder)
{
    builder.Services.Configure<AdminOptions>(opt =>
    {
        opt.Username = Environment.GetEnvironmentVariable("META_ADMIN_USERNAME")
            ?? builder.Configuration["Admin:Username"]
            ?? "admin";
        opt.Password = Environment.GetEnvironmentVariable("META_ADMIN_PASSWORD")
            ?? builder.Configuration["Admin:Password"]
            ?? "admin";
    });
}

static void ConfigureMetaStorage(WebApplicationBuilder builder)
{
    builder.Services.Configure<MetaStorageOptions>(opt =>
    {
        opt.DataDirectory = Environment.GetEnvironmentVariable("META_DATA_DIR")
            ?? builder.Configuration["Meta:DataDirectory"]
            ?? "data";
        opt.HeroesFileName = builder.Configuration["Meta:HeroesFileName"] ?? "heroes.json";
        opt.TrinketsFileName = builder.Configuration["Meta:TrinketsFileName"] ?? "trinkets.json";
    });
}
