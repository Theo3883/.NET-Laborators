using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model;

public class Paper
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = default!;
    
    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = default!;
    
    [Required]
    public DateTime PublishedOn { get; set; }
}

