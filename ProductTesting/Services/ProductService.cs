using Microsoft.EntityFrameworkCore;
using ProductTesting.Ef;
using ProductTesting.Models;

namespace ProductTesting.Services;

public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync(CancellationToken ct);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct);
    Task<int> CreateProductAsync(Product product, CancellationToken ct);
    Task UpdateProductAsync(int id, Product product, CancellationToken ct);
    Task DeleteProductAsync(int id, CancellationToken ct);
}

public class ProductService(
    AppDbContext context
    ) : IProductService
{
    public async Task<List<Product>> GetAllProductsAsync(CancellationToken ct)
    {
        return await context.Products.ToListAsync(ct);
    }

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct)
    {
        return await context.Products.FindAsync(id, ct);
    }

    public async Task<int> CreateProductAsync(Product product, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ArgumentNullException(nameof(Product.Name));

        if (product.Price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(Product.Price));

        context.Products.Add(product);
        await context.SaveChangesAsync(ct);

        return product.Id;
    }

    public async Task UpdateProductAsync(int id, Product updatedProduct, CancellationToken ct)
    {
        var product = await context.Products.FindAsync(id, ct);
        if (product is null)
            throw new ArgumentException($"No entity for id {id}");

        if (string.IsNullOrWhiteSpace(updatedProduct.Name))
            throw new ArgumentException($"Name cannot be null or empty: {updatedProduct.Name}");

        if (updatedProduct.Price < 0)
            throw new ArgumentException($"Price cannot be less than 0. Price: {updatedProduct.Price}");

        product.Name = updatedProduct.Name;
        product.Price = updatedProduct.Price;
        product.Description = updatedProduct.Description;

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct)
    {
        var product = await context.Products.FindAsync(id, ct);
        if (product is null)
            throw new ArgumentException($"No entity for id {id}");

        context.Products.Remove(product);
        await context.SaveChangesAsync(ct);
    }
}
