using System.Net.Http.Json;

namespace EShop.WebApp.Services;

public class CatalogClient(HttpClient http)
{
    public async Task<CatalogPage> GetItemsAsync(int pageIndex = 0, int pageSize = 50, string? brand = null)
    {
        var url = $"/api/catalog/items?pageIndex={pageIndex}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(brand))
        {
            url += $"&brand={Uri.EscapeDataString(brand)}";
        }

        return await http.GetFromJsonAsync<CatalogPage>(url)
               ?? new CatalogPage(0, pageSize, 0, []);
    }

    public Task<List<string>?> GetBrandsAsync() =>
        http.GetFromJsonAsync<List<string>>("/api/catalog/brands");
}

public record CatalogItem(int Id, string Name, string Description, decimal Price, string Brand, string Type, int AvailableStock);

public record CatalogPage(int PageIndex, int PageSize, int Count, List<CatalogItem> Data);
