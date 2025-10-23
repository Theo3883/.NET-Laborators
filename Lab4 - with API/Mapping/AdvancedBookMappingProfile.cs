using AutoMapper;
using Lab3.DTO;
using Lab3.DTO.Request;
using Lab3.Mapping.Resolvers;
using Lab3.Model;

namespace Lab3.Mapping;

public class AdvancedBookMappingProfile : Profile
{
    public AdvancedBookMappingProfile()
    {
        // CreateBookProfileRequest -> Book
        CreateMap<CreateBookProfileRequest, Book>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsAvailable, opt => opt.Ignore()); // Computed property

        // Book -> BookProfileDto
        CreateMap<Book, BookProfileDto>()
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.Price, opt => opt.MapFrom<ConditionalPriceResolver>())
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom<ConditionalCoverImageResolver>())
            .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<PublishedAgeResolver>())
            .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<AuthorInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>());
    }
}
