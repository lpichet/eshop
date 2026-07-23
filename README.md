# eShop — Aspire multi-team demo

Demo storefront that shows how **[Aspire](https://aspire.dev) 13.4** lets independent teams ship services on their own cadence while one team composes them into a product. Inspired by [dotnet/eshop](https://github.com/dotnet/eshop).

## The cast (three independent repos)

| Repo | Team | Ships |
|---|---|---|
| [eshop-catalog-api](https://github.com/lpichet/eshop-catalog-api) | Catalog | Container image on GHCR + `EShop.Catalog.Hosting` NuGet (GitHub Packages) |
| [eshop-basket-api](https://github.com/lpichet/eshop-basket-api) | Basket | Container image on GHCR + `EShop.Basket.Hosting` NuGet (GitHub Packages) |
| **this repo** | eShop (storefront) | Blazor web app + the Aspire AppHost that composes everything |

The AppHost never checks out the other teams' source. It consumes their **published contracts**:

```csharp
var catalogApi = builder.AddCatalogApi("catalog-api", catalogDb)   // ghcr.io image + wiring
    .WithSeedData("../../seed/eshop-catalog.json");                 // replace the team's sample data

var basketApi = builder.AddBasketApi("basket-api", redis);
```

`AddCatalogApi` / `AddBasketApi` / `WithSeedData` come from the teams' hosting-integration packages — that's the cross-team API surface.

## Run it

Prereqs: .NET 10 SDK, Docker (or Podman), [Aspire CLI](https://aspire.dev), GitHub CLI (`gh auth login`).

```bash
# GitHub Packages needs auth even for public feeds:
export GITHUB_USERNAME=lpichet
export GITHUB_TOKEN=$(gh auth token)

aspire run --project src/EShop.AppHost
# or: dotnet run --project src/EShop.AppHost
```

The dashboard opens with: PostgreSQL + Redis containers, the two team images pulled from GHCR, and the Blazor webapp from source. Browse the storefront, add items to the basket, then restart the `webapp` resource from the dashboard — the basket survives (it lives in the basket team's Redis).

## Demo talking points

1. **Independent repos, one composition** — each team also has its own AppHost for the inner loop; this repo pulls their published images.
2. **Data seeding** — the catalog API seeds its database from JSON on first start.
3. **Seed replacement as a contract** — `WithSeedData()` (from the catalog team's package) bind-mounts *our* JSON into *their* container. This storefront ships its own catalog ([seed/eshop-catalog.json](seed/eshop-catalog.json)); compare with the catalog team's built-in sample data.
4. **Integration tests** — each team repo boots its own AppHost with real containers (`Aspire.Hosting.Testing`).
5. **End-to-end tests** — [tests/EShop.E2ETests](tests/EShop.E2ETests) boots this whole composition (team images included) and replaces the seed a third time with test-only data, proving the same extension serves dev, demo and test scenarios.

```bash
dotnet test   # runs the e2e suite locally (same env vars as above)
```

## Versioning

The AppHost floats on the teams' latest contracts (`Version="1.0.*"` + image tag `latest`) to keep the demo fresh. Pin exact versions for anything real — `AddCatalogApi("catalog-api", catalogDb, tag: "1.0.42")`.
