using IntelliDocs.ReviewPortal.Components;
using IntelliDocs.ReviewPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl =
    builder.Configuration["ReviewApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "ReviewApi:BaseUrl configuration is required.");

builder.Services.AddHttpClient<ReviewApiClient>(
    client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();