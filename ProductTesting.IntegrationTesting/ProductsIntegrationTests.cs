using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;
using ProductTesting.Ef;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductTesting.Models;
using Microsoft.AspNetCore.Hosting;

namespace ProductTesting.IntegrationTesting;

public class ProductsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> factory;
    private HttpClient client = null!;
    private IServiceScope scope = null!;
    private AppDbContext dbContext = null!;

    public ProductsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder((IWebHostBuilder builder) =>
        {
            builder.ConfigureServices((IServiceCollection services) =>
            {
                var descriptor = services.SingleOrDefault(
                    (ServiceDescriptor d) => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>((DbContextOptionsBuilder options) =>
                {
                    options.UseInMemoryDatabase($"TestDb");
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        client = factory.CreateClient();
        scope = factory.Services.CreateScope();
        dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContext.Database.EnsureDeletedAsync();
        dbContext.Dispose();
        scope.Dispose();
        client.Dispose();
    }

        [Theory]
        [InlineData("Laptop", 999.99, "Gaming laptop")]
        [InlineData("Mouse", 29.99, null)]
        [InlineData("Keyboard", 79.99, "Mechanical keyboard")]
        public async Task CreateProduct_ReturnsCreated_WithVariousValidInputs(string name, decimal price, string? description)
        {
            // Arrange
            var newProduct = new Product { Name = name, Price = price, Description = description };

            // Act
            var response = await client.PostAsJsonAsync("/api/products", newProduct);
            var createdProduct = await response.Content.ReadFromJsonAsync<Product>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            createdProduct.Should().NotBeNull();
            createdProduct!.Name.Should().Be(name);
            createdProduct.Price.Should().Be(price);
            createdProduct.Description.Should().Be(description);
        }

    [Theory]
    [InlineData("", 99.99)]
    [InlineData("Product", -10)]
    [InlineData("  ", 50)]
    public async Task CreateProduct_ReturnsBadRequest_WithInvalidInputs(string name, decimal price)
    {
        // Arrange
        var invalidProduct = new Product { Name = name, Price = price };

        // Act
        var response = await client.PostAsJsonAsync("/api/products", invalidProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsCorrectCount_AfterMultipleCreations()
    {
        // Arrange - Create 3 products
        var products = new[]
        {
            new Product { Name = "Product 1", Price = 10m },
            new Product { Name = "Product 2", Price = 20m },
            new Product { Name = "Product 3", Price = 30m }
        };

        foreach (var product in products)
        {
            await client.PostAsJsonAsync("/api/products", product);
        }

        // Act
        var response = await client.GetAsync("/api/products");
        var returnedProducts = await response.Content.ReadFromJsonAsync<List<Product>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returnedProducts.Should().HaveCount(3);
        returnedProducts.Should().OnlyContain(p => p.Price > 0);
    }

    [Theory]
    [MemberData(nameof(GetUpdateScenarios))]
    public async Task UpdateProduct_HandlesVariousScenarios(bool createProduct, Product updateData, HttpStatusCode expectedStatus)
    {
        var targetId = 0;

        // Arrange
        if (createProduct) // Create a product with ID 1
        {
            var product = new Product { Name = "Original", Price = 100m };
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();
            targetId = product.Id;
        }

        // Act
        var response = await client.PutAsJsonAsync($"/api/products/{targetId}", updateData);

        // Assert
         response.StatusCode.Should().Be(expectedStatus);
    }

    public static IEnumerable<object[]> GetUpdateScenarios()
    {
        yield return new object[] { true, new Product { Name = "Updated", Price = 150m }, HttpStatusCode.OK };
        yield return new object[] { false, new Product { Name = "Updated", Price = 150m }, HttpStatusCode.BadRequest };
        yield return new object[] { true, new Product { Name = "", Price = 150m }, HttpStatusCode.BadRequest };
        yield return new object[] { true, new Product { Name = "Updated", Price = -50m }, HttpStatusCode.BadRequest };
    }
}