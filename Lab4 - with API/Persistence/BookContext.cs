 
using Lab3.Model;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Persistence;

public class BookContext(DbContextOptions<BookContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }
    public DbSet<BookLocalization> BookLocalizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure BookLocalization
        modelBuilder.Entity<BookLocalization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BookId, e.CultureCode }).IsUnique();
            entity.Property(e => e.LocalizedTitle).IsRequired().HasMaxLength(500);
            entity.Property(e => e.LocalizedDescription).HasMaxLength(2000);
            entity.Property(e => e.CultureCode).IsRequired().HasMaxLength(10);

            // Relationship
            entity.HasOne(e => e.Book)
                  .WithMany(b => b.Localizations)
                  .HasForeignKey(e => e.BookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}