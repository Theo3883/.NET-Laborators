 
using Lab3.Model;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Persistence;

public class BookContext(DbContextOptions<BookContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}