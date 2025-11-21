using WebApplication1.DTO.Request;
using WebApplication1.Model;

namespace WebApplication1.Mappers;

/// <summary>
/// Mapper interface for Paper entity conversions
/// </summary>
public interface IPaperMapper
{
    /// <summary>
    /// Maps a CreatePaperRequest DTO to a Paper entity
    /// </summary>
    Paper MapToEntity(CreatePaperRequest request);
    
    /// <summary>
    /// Maps a Paper entity to response (currently same as entity, but allows future DTO)
    /// </summary>
    Paper MapToResponse(Paper paper);
}

