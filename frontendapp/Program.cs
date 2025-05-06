using frontendapp.Components;
using frontendapp.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.UI;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// ########################################### Entra Auth Begin ###########################################
// This is required to be instantiated before the OpenIdConnectOptions starts getting configured.
// By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
// For instance, 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' instead of 'roles' claim.
// This flag ensures that the ClaimsIdentity claims collection will be built from the claims in the token
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi(
        [
            builder.Configuration.GetSection("DownstreamApi:Scopes:Read").Get<string>()!,
            builder.Configuration.GetSection("DownstreamApi:Scopes:Write").Get<string>()!
        ]
    )
    .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();

if (builder.Configuration.GetConnectionString("Redis") != null)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "RedisDemos_"; // unique to the app
    });

    builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(builder.Configuration.GetSection("RedisOptions"));
}
else
{
    builder.Services.AddDistributedMemoryCache(); // NOT RECOMMENDED FOR PRODUCTION! Use a persistent cache like Redis
}

builder.Services.AddAuthorization(options =>
{
    // Optional: Add a policy for TestGroup if using policy-based authorization
    options.AddPolicy("DemoAdmin", policy =>
        policy.RequireClaim("groups", builder.Configuration["AdminGroupId"]));
});

builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();
builder.Services.AddRazorPages(); //https://github.com/dotnet/aspnetcore/issues/52245
// ########################################### Entra Auth End ###########################################

builder.Services.AddScoped<WeatherForecastService>();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ########################################### Entra Auth Begin ###########################################
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// ########################################### Entra Auth End ###########################################

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
