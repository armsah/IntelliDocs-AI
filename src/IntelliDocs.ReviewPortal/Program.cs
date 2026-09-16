using IntelliDocs.ReviewPortal.Components;
using Microsoft.Identity.Web.UI;
using IntelliDocs.ReviewPortal.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var applicationInsightsConnectionString =
    builder.Configuration[
        "APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (!string.IsNullOrWhiteSpace(
        applicationInsightsConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString =
                applicationInsightsConnectionString;
        });
}

builder.Services
    .AddAuthentication(
        OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl =
    builder.Configuration["ReviewApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "ReviewApi:BaseUrl configuration is required.");

builder.Services.AddTransient<
    ReviewApiAuthorizationHandler>();

builder.Services
    .AddHttpClient<ReviewApiClient>(
        client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
    .AddHttpMessageHandler<
        ReviewApiAuthorizationHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();