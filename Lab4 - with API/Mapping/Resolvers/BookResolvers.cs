using AutoMapper;
using Lab3.DTO;
using Lab3.Model;

namespace Lab3.Mapping.Resolvers;

public class CategoryDisplayResolver : IValueResolver<Book, BookProfileDto, string>
{
    public string Resolve(Book source, BookProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            BookCategory.Fiction => "Fiction & Literature",
            BookCategory.NonFiction => "Non-Fiction",
            BookCategory.Technical => "Technical & Professional",
            BookCategory.Children => "Children's Books",
            _ => "Uncategorized"
        };
    }
}

public class PriceFormatterResolver : IValueResolver<Book, BookProfileDto, string>
{
    public string Resolve(Book source, BookProfileDto destination, string destMember, ResolutionContext context)
    {
        // Use the already-mapped Price from destination (which includes discount logic)
        var priceToFormat = source.Category == BookCategory.Children ? source.Price * 0.9m : source.Price;
        return priceToFormat.ToString("C2");
    }
}

public class PublishedAgeResolver : IValueResolver<Book, BookProfileDto, string>
{
    public string Resolve(Book source, BookProfileDto destination, string destMember, ResolutionContext context)
    {
        var daysSincePublished = (DateTime.UtcNow - source.PublishedDate).TotalDays;

        return daysSincePublished switch
        {
            < 30 => "New Release",
            < 365 => $"{Math.Floor(daysSincePublished / 30)} months old",
            < 1825 => $"{Math.Floor(daysSincePublished / 365)} years old",
            _ => "Classic"
        };
    }
}

public class AuthorInitialsResolver : IValueResolver<Book, BookProfileDto, string>
{
    public string Resolve(Book source, BookProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Author))
            return "?";

        var names = source.Author.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return names.Length switch
        {
            0 => "?",
            1 => names[0][0].ToString().ToUpper(),
            _ => $"{names[0][0].ToString().ToUpper()}{names[^1][0].ToString().ToUpper()}"
        };
    }
}

public class AvailabilityStatusResolver : IValueResolver<Book, BookProfileDto, string>
{
    public string Resolve(Book source, BookProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
            return "Out of Stock";

        return source.StockQuantity switch
        {
            0 => "Unavailable",
            1 => "Last Copy",
            <= 5 => "Limited Stock",
            _ => "In Stock"
        };
    }
}

public class ConditionalCoverImageResolver : IValueResolver<Book, BookProfileDto, string?>
{
    public string? Resolve(Book source, BookProfileDto destination, string? destMember, ResolutionContext context)
    {
        return source.Category == BookCategory.Children ? null : source.CoverImageUrl;
    }
}

public class ConditionalPriceResolver : IValueResolver<Book, BookProfileDto, decimal>
{
    public decimal Resolve(Book source, BookProfileDto destination, decimal destMember, ResolutionContext context)
    {
        return source.Category == BookCategory.Children ? source.Price * 0.9m : source.Price;
    }
}
