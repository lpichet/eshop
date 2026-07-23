using EShop.WebApp.Components;
using EShop.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Catalog API: HTTP + Aspire service discovery ("catalog-api" resolves to the
// endpoint injected by the AppHost).
builder.Services.AddHttpClient<CatalogClient>(client =>
    client.BaseAddress = new Uri("http://catalog-api"));

// Basket API: gRPC over the service-discovered named endpoint
// ("_grpc" targets the basket team's HTTP/2-only gRPC endpoint).
builder.Services.AddGrpcClient<BasketApi.Grpc.Basket.BasketClient>(options =>
    options.Address = new Uri("http://_grpc.basket-api"));

builder.Services.AddScoped<BasketState>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
