using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model;

public class Paper
{
    [Key]
    public int Id { get; init; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Author { get; init; } = null!;
    
    [Required]
    public DateTime PublishedOn { get; init; }
}

