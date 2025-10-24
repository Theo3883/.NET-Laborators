using AutoMapper;
using Lab3.DTO;
using Lab3.Model;

namespace Lab3.Mapping.Resolvers;

public class OrderCategoryDisplayResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            OrderCategory.Fiction => "Fiction & Literature",
            OrderCategory.NonFiction => "Non-Fiction",
            OrderCategory.Technical => "Technical & Professional",
            OrderCategory.Children => "Children's Orders",
            _ => "Uncategorized"
        };
    }
}

public class OrderPriceFormatterResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        // Use the already-mapped Price from destination (which includes discount logic)
        var priceToFormat = source.Category == OrderCategory.Children ? source.Price * 0.9m : source.Price;
        return priceToFormat.ToString("C2");
    }
}

public class OrderPublishedAgeResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
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

public class OrderAuthorInitialsResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
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

public class OrderAvailabilityStatusResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
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

public class ConditionalOrderCoverImageResolver : IValueResolver<Order, OrderProfileDto, string?>
{
    public string? Resolve(Order source, OrderProfileDto destination, string? destMember, ResolutionContext context)
    {
        // Return null for Children category (content filtering)
        // Return actual URL for Fiction, NonFiction, Technical categories
        return source.Category == OrderCategory.Children ? null : source.CoverImageUrl;
    }
}

public class ConditionalOrderPriceResolver : IValueResolver<Order, OrderProfileDto, decimal>
{
    public decimal Resolve(Order source, OrderProfileDto destination, decimal destMember, ResolutionContext context)
    {
        // Apply 10% discount for Children category
        // Return actual price for all other categories
        return source.Category == OrderCategory.Children ? source.Price * 0.9m : source.Price;
    }
}
