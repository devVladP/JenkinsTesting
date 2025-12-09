using Microsoft.EntityFrameworkCore;
using ProductTesting.Models;

namespace ProductTesting.Ef;

public class AppDbContext(
    DbContextOptions options
    ) : DbContext(options)
{
    public virtual DbSet<Product> Products { get; set; }
}
