using WebApplication1.Model;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Persistence;

public class PaperContext(DbContextOptions<PaperContext> options) : DbContext(options)
{
    public DbSet<Paper> Papers => Set<Paper>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

public static class PaperContextExtensions
{
    public static void SeedData(this PaperContext context)
    {
        // Check if data already exists
        if (context.Papers.Any())
        {
            return;
        }

        // Seed at least 5 papers
        var papers = new[]
        {
            new Paper
            {
                Title = "Introduction to Machine Learning",
                Author = "John Smith",
                PublishedOn = new DateTime(2023, 5, 15)
            },
            new Paper
            {
                Title = "Deep Learning Fundamentals",
                Author = "Jane Doe",
                PublishedOn = new DateTime(2023, 8, 22)
            },
            new Paper
            {
                Title = "Natural Language Processing in Practice",
                Author = "Robert Johnson",
                PublishedOn = new DateTime(2024, 1, 10)
            },
            new Paper
            {
                Title = "Computer Vision Algorithms",
                Author = "Emily Davis",
                PublishedOn = new DateTime(2024, 3, 5)
            },
            new Paper
            {
                Title = "Reinforcement Learning Applications",
                Author = "Michael Brown",
                PublishedOn = new DateTime(2024, 6, 18)
            },
            new Paper
            {
                Title = "Neural Networks and Backpropagation",
                Author = "Sarah Wilson",
                PublishedOn = new DateTime(2024, 9, 30)
            },
            new Paper
            {
                Title = "Quantum Computing Basics",
                Author = "David Lee",
                PublishedOn = new DateTime(2024, 11, 12)
            }
        };

        context.Papers.AddRange(papers);
        context.SaveChanges();
    }
}

