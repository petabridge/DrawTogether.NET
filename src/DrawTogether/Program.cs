using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DrawTogether.Components;
using DrawTogether.Components.Account;
using DrawTogether.Config;
using DrawTogether.Data;
using DrawTogether.Email;
using Microsoft.AspNetCore.ResponseCompression;
using MudBlazor.Services;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;

// get ASP.NET Environment
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// If you also want WebAssembly SSR, include this line:
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddDrawTogetherSettings(builder.Configuration);
builder.Services.AddEmailServices<ApplicationUser>(builder.Configuration); // add email services

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.AddSqlServerDbContext<ApplicationDbContext>("DefaultConnection");

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var requireConfirmedAccount = builder.Configuration.GetValue<bool>("DrawTogether:RequireEmailConfirmation");

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddSignalR().AddJsonProtocol();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.ConfigureAkka(builder.Configuration, 
    (configurationBuilder, provider) =>
    {
        var options = provider.GetRequiredService<AkkaSettings>();
        configurationBuilder.AddPetabridgeCmd(
            options: options.PbmOptions,
            hostConfiguration: cmd =>
            {
                cmd.RegisterCommandPalette(ClusterCommands.Instance);
                cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
            });
    });

builder.Services.AddDrawTogetherOtel();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCompression();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.Run();
