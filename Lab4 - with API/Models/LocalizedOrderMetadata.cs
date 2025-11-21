namespace Lab3.Models;

public class LocalizedCategoryInfo
{
    public string CategoryKey { get; set; } = null!;
    public string LocalizedCategoryName { get; set; } = null!;
    public string CategoryDescription { get; set; } = null!;
    public string Culture { get; set; } = null!;
}

public class LocalizedTerms
{
    public Dictionary<string, string> Terms { get; set; } = new();
    public string Culture { get; set; } = null!;
}
