using BasketApi.Grpc;

namespace EShop.WebApp.Services;

/// <summary>
/// Per-circuit basket facade over the basket team's gRPC API.
/// A fixed buyer id keeps the demo simple (and shows off Redis persistence:
/// the basket survives page reloads and webapp restarts).
/// </summary>
public class BasketState(Basket.BasketClient basketClient)
{
    public const string BuyerId = "demo-shopper";

    public event Action? Changed;

    public async Task<CustomerBasketResponse> GetAsync() =>
        await basketClient.GetBasketAsync(new GetBasketRequest { BuyerId = BuyerId });

    public async Task AddItemAsync(CatalogItem product)
    {
        var basket = await GetAsync();
        var update = new UpdateBasketRequest { BuyerId = BuyerId };
        update.Items.AddRange(basket.Items);

        var existing = update.Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            update.Items.Add(new BasketItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = (double)product.Price,
                Quantity = 1
            });
        }

        await basketClient.UpdateBasketAsync(update);
        Changed?.Invoke();
    }

    public async Task SetQuantityAsync(int productId, int quantity)
    {
        var basket = await GetAsync();
        var update = new UpdateBasketRequest { BuyerId = BuyerId };
        update.Items.AddRange(basket.Items);

        var item = update.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            update.Items.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
        }

        await basketClient.UpdateBasketAsync(update);
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        await basketClient.DeleteBasketAsync(new DeleteBasketRequest { BuyerId = BuyerId });
        Changed?.Invoke();
    }
}
