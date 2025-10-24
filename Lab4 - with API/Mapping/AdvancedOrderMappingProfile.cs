using AutoMapper;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Mapping.Resolvers;
using Lab3.Model;

namespace Lab3.Mapping;

public class AdvancedOrderMappingProfile : Profile
{
    public AdvancedOrderMappingProfile()
    {
        // CreateOrderProfileRequest -> Order
        CreateMap<CreateOrderProfileRequest, Order>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => Enum.Parse<OrderCategory>(src.Category, true)));

        // Order -> OrderProfileDto with custom resolvers
        CreateMap<Order, OrderProfileDto>()
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<OrderCategoryDisplayResolver>())
            .ForMember(dest => dest.Price, opt => opt.MapFrom<ConditionalOrderPriceResolver>())
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<OrderPriceFormatterResolver>())
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom<ConditionalOrderCoverImageResolver>())
            .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<OrderPublishedAgeResolver>())
            .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<OrderAuthorInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<OrderAvailabilityStatusResolver>());
    }
}
