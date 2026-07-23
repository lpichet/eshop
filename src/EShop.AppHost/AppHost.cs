var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure owned by the eShop team.
var postgres = builder.AddPostgres("postgres");
var catalogDb = postgres.AddDatabase("catalogdb");
var redis = builder.AddRedis("basketcache");

// The catalog and basket teams ship container images (GHCR) plus Aspire hosting
// integrations (GitHub Packages). We orchestrate their published artifacts here —
// no source from their repos is checked out.
var catalogApi = builder.AddCatalogApi("catalog-api", catalogDb)
    // The eShop team replaces the catalog team's built-in sample data with its own
    // storefront catalog. E2E tests override this again via --Catalog:SeedFile.
    .WithSeedData(builder.Configuration["Catalog:SeedFile"] ?? "../../seed/eshop-catalog.json");

var basketApi = builder.AddBasketApi("basket-api", redis);

builder.AddProject<Projects.EShop_WebApp>("webapp")
    .WithExternalHttpEndpoints()
    .WithReference(catalogApi.GetEndpoint("http"))
    .WithReference(basketApi.GetEndpoint("grpc"))
    .WaitFor(catalogApi)
    .WaitFor(basketApi)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
