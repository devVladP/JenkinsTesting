using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ProductTesting.Ef;
using ProductTesting.Models;
using ProductTesting.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ProductDb"));

builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";

        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        if (exception is ArgumentNullException or ArgumentException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "An error occurred" });
        }
    });
});

// CRUD Endpoints
app.MapGet("/api/products", async (IProductService service, CancellationToken ct) =>
{
    var products = await service.GetAllProductsAsync(ct);
    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/api/products/{id}", async (int id, IProductService service, CancellationToken ct) =>
{
    var product = await service.GetProductByIdAsync(id, ct);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProduct")
.WithOpenApi();

app.MapPost("/api/products", async (Product product, IProductService service, CancellationToken ct) =>
{
    var id = await service.CreateProductAsync(product, ct);

    return Results.Created($"/api/products/{id}", product);
})
.WithName("CreateProduct")
.WithOpenApi();

app.MapPut("/api/products/{id}", async (int id, Product updatedProduct, IProductService service, CancellationToken ct) =>
{
    await service.UpdateProductAsync(id, updatedProduct, ct);

    return Results.Ok();
})
.WithName("UpdateProduct")
.WithOpenApi();

app.MapDelete("/api/products/{id}", async (int id, IProductService service, CancellationToken ct) =>
{
    await service.DeleteProductAsync(id, ct);

    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithOpenApi();

app.Run();
// Make Program accessible for testing
public partial class Program { }