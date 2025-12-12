using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Moq;
using Xunit;
using ProductTesting.Ef;
using ProductTesting.Models;
using ProductTesting.Services;

namespace ProductTesting.UnitTesting;

public class ProductServiceTests
{
    private readonly Mock<AppDbContext> mockContext;
    private readonly Mock<DbSet<Product>> mockDbSet;
    private readonly ProductService service;

    public ProductServiceTests()
    {
        mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        mockDbSet = new Mock<DbSet<Product>>();

        mockContext.Setup(c => c.Products).Returns(mockDbSet.Object);

        service = new ProductService(mockContext.Object);
    }

    #region Create Tests

    [Fact]
    public async Task CreateProductAsync_ReturnsProductId_WhenProductIsValid()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Price = 99.99m,
            Description = "Test Description"
        };

        mockDbSet.Setup(d => d.Add(It.IsAny<Product>()))
            .Callback<Product>(p => p.Id = 1);

        mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var productId = await service.CreateProductAsync(product, CancellationToken.None);

        // Assert
        productId.Should().Be(1);
        mockDbSet.Verify(d => d.Add(It.Is<Product>(p =>
            p.Name == "Test Product" &&
            p.Price == 99.99m &&
            p.Description == "Test Description")), Times.Once);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ThrowsArgumentNullException_WhenNameIsEmpty()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            Price = 99.99m
        };

        // Act
        Func<Task> act = async () => await service.CreateProductAsync(product, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName(nameof(Product.Name));

        mockDbSet.Verify(d => d.Add(It.IsAny<Product>()), Times.Never);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_ThrowsArgumentNullException_WhenNameIsNull()
    {
        // Arrange
        var product = new Product
        {
            Name = null!,
            Price = 99.99m
        };

        // Act
        Func<Task> act = async () => await service.CreateProductAsync(product, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName(nameof(Product.Name));
    }

    [Fact]
    public async Task CreateProductAsync_ThrowsArgumentException_WhenPriceIsNegative()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Price = -10m
        };

        // Act
        Func<Task> act = async () => await service.CreateProductAsync(product, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(Product.Price));

        mockDbSet.Verify(d => d.Add(It.IsAny<Product>()), Times.Never);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteProductAsync_DeletesProduct_WhenProductExists()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", Price = 50m };

        mockDbSet.Setup(d => d.FindAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Product?>(product));

        mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await service.DeleteProductAsync(1, CancellationToken.None);

        // Assert
        mockDbSet.Verify(d => d.FindAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        mockDbSet.Verify(d => d.Remove(It.Is<Product>(p => p.Id == 1)), Times.Once);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_ThrowsArgumentException_WhenProductDoesNotExist()
    {
        // Arrange
        mockDbSet.Setup(d => d.FindAsync(new object[] { 999 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await service.DeleteProductAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("No entity for id 999");

        mockDbSet.Verify(d => d.Remove(It.IsAny<Product>()), Times.Never);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateProductAsync_UpdatesProduct_WhenProductExistsAndDataIsValid()
    {
        // Arrange
        var existingProduct = new Product
        {
            Id = 1,
            Name = "Original Product",
            Price = 100m,
            Description = "Original Description"
        };

        var updatedProduct = new Product
        {
            Id = 1,
            Name = "Updated Product",
            Price = 150m,
            Description = "Updated Description"
        };

        mockDbSet.Setup(d => d.FindAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Product?>(existingProduct));

        mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await service.UpdateProductAsync(1, updatedProduct, CancellationToken.None);

        // Assert
        existingProduct.Name.Should().Be("Updated Product");
        existingProduct.Price.Should().Be(150m);
        existingProduct.Description.Should().Be("Updated Description");

        mockDbSet.Verify(d => d.FindAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ThrowsArgumentException_WhenProductDoesNotExist()
    {
        // Arrange
        var updatedProduct = new Product
        {
            Name = "Updated Product",
            Price = 150m
        };

        mockDbSet.Setup(d => d.FindAsync(new object[] { 999 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await service.UpdateProductAsync(999, updatedProduct, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("No entity for id 999");

        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ThrowsArgumentException_WhenNameIsEmpty()
    {
        // Arrange
        var existingProduct = new Product { Id = 1, Name = "Original", Price = 100m };

        var updatedProduct = new Product
        {
            Id = 1,
            // TODO: change it to empty
            Name = "Not empty",
            Price = 150m
        };

        mockDbSet.Setup(d => d.FindAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Product?>(existingProduct));

        // Act
        Func<Task> act = async () => await service.UpdateProductAsync(1, updatedProduct, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Name cannot be null or empty: ");

        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ThrowsArgumentException_WhenNameIsNull()
    {
        // Arrange
        var existingProduct = new Product { Id = 1, Name = "Original", Price = 100m };

        var updatedProduct = new Product
        {
            Id = 1,
            Name = null!,
            Price = 150m
        };

        mockDbSet.Setup(d => d.FindAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        Func<Task> act = async () => await service.UpdateProductAsync(1, updatedProduct, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateProductAsync_ThrowsArgumentException_WhenPriceIsNegative()
    {
        // Arrange
        var existingProduct = new Product { Id = 1, Name = "Original", Price = 100m };

        var updatedProduct = new Product
        {
            Id = 1,
            Name = "Updated",
            Price = -50m
        };

        mockDbSet.Setup(d => d.FindAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        Func<Task> act = async () => await service.UpdateProductAsync(1, updatedProduct, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Price cannot be less than 0. Price: -50");

        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}