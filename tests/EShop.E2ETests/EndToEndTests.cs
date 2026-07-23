using System.Net.Http.Json;
using Aspire.Hosting;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace EShop.E2ETests;

/// <summary>
/// Boots the REAL composition once for all tests: the catalog and basket teams'
/// container images pulled from GHCR, PostgreSQL, Redis, and the webapp from source.
/// The catalog seed is replaced through the catalog team's WithSeedData() extension
/// (via the --Catalog:SeedFile switch the AppHost exposes), so assertions run against
/// data that only exists in this test run.
/// </summary>
public sealed class EShopAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(10);

    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "e2e-catalog.json");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.EShop_AppHost>(
            [$"--Catalog:SeedFile={seedPath}"]);
        appHost.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        App = await appHost.BuildAsync();
        await App.StartAsync();

        await App.ResourceNotifications.WaitForResourceHealthyAsync("catalog-api").WaitAsync(StartupTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("basket-api").WaitAsync(StartupTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("webapp").WaitAsync(StartupTimeout);
    }

    public async Task DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}

public class EndToEndTests(EShopAppFixture fixture) : IClassFixture<EShopAppFixture>
{
    [Fact]
    public async Task CatalogApi_ServesTheReplacedSeed()
    {
        using var client = fixture.App.CreateHttpClient("catalog-api");
        var page = await client.GetFromJsonAsync<CatalogPage>("/api/catalog/items?pageSize=50");

        Assert.NotNull(page);
        Assert.Equal(3, page.Count);
        Assert.All(page.Data, i => Assert.Equal("E2E Labs", i.Brand));
    }

    [Fact]
    public async Task HomePage_RendersProducts_FromTheReplacedSeed()
    {
        using var client = fixture.App.CreateHttpClient("webapp");
        var html = await client.GetStringAsync("/");

        Assert.Contains("E2E Carbon Unicycle", html);
        Assert.Contains("E2E Quantum Tent", html);
    }

    [Fact]
    public async Task BasketPage_IsServed()
    {
        using var client = fixture.App.CreateHttpClient("webapp");
        using var response = await client.GetAsync("/basket");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record CatalogPage(int PageIndex, int PageSize, int Count, List<CatalogItemDto> Data);

    private sealed record CatalogItemDto(int Id, string Name, string? Description, decimal Price, string Brand, string Type, int AvailableStock);
}
