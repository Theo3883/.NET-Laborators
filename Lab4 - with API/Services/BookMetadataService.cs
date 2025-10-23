using System.Xml.Linq;
using Lab3.Model;

namespace Lab3.Services;


/// Service for loading and managing multi-language book metadata from XML resources
public interface IBookMetadataService
{
    string GetCategoryName(BookCategory category, string cultureCode);
    string GetCategoryDescription(BookCategory category, string cultureCode);
    string GetAvailabilityStatus(int stockQuantity, string cultureCode);
    string GetLabel(string labelKey, string cultureCode);
    Dictionary<string, string> GetAllCategoryNames(string cultureCode);
    bool IsCultureSupported(string cultureCode);
}

public class BookMetadataService : IBookMetadataService
{
    private readonly ILogger<BookMetadataService> _logger;
    private readonly Dictionary<string, XDocument> _cachedResources = new();
    private readonly string _resourcePath;
    private const string DefaultCulture = "en-US";

    public BookMetadataService(ILogger<BookMetadataService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _resourcePath = Path.Combine(environment.ContentRootPath, "Resources");
        LoadAllResources();
    }

    private void LoadAllResources()
    {
        var cultures = new[] { "en-US", "es", "fr", "de", "ja" };
        
        foreach (var culture in cultures)
        {
            try
            {
                var filePath = Path.Combine(_resourcePath, $"BookMetadata.{culture}.xml");
                if (File.Exists(filePath))
                {
                    var doc = XDocument.Load(filePath);
                    _cachedResources[culture] = doc;
                    _logger.LogInformation("Loaded resource file for culture: {Culture}", culture);
                }
                else
                {
                    _logger.LogWarning("Resource file not found for culture: {Culture}", culture);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load resource file for culture: {Culture}", culture);
            }
        }

        if (!_cachedResources.ContainsKey(DefaultCulture))
        {
            _logger.LogCritical("Default culture resource ({DefaultCulture}) not loaded!", DefaultCulture);
        }
    }

    public bool IsCultureSupported(string cultureCode)
    {
        var normalizedCulture = NormalizeCulture(cultureCode);
        return _cachedResources.ContainsKey(normalizedCulture);
    }

    public string GetCategoryName(BookCategory category, string cultureCode)
    {
        var doc = GetResourceDocument(cultureCode);
        var categoryNode = doc?.Root?
            .Element("categories")?
            .Elements("category")
            .FirstOrDefault(c => c.Attribute("id")?.Value == category.ToString());

        return categoryNode?.Element("name")?.Value ?? category.ToString();
    }

    public string GetCategoryDescription(BookCategory category, string cultureCode)
    {
        var doc = GetResourceDocument(cultureCode);
        var categoryNode = doc?.Root?
            .Element("categories")?
            .Elements("category")
            .FirstOrDefault(c => c.Attribute("id")?.Value == category.ToString());

        return categoryNode?.Element("description")?.Value ?? string.Empty;
    }

    public string GetAvailabilityStatus(int stockQuantity, string cultureCode)
    {
        var doc = GetResourceDocument(cultureCode);
        var statusNode = doc?.Root?.Element("ui")?.Element("availabilityStatus");

        if (statusNode == null) return "Unknown";

        return stockQuantity switch
        {
            0 => statusNode.Element("outOfStock")?.Value ?? "Out of Stock",
            <= 10 => statusNode.Element("lowStock")?.Value ?? "Low Stock",
            _ => statusNode.Element("inStock")?.Value ?? "In Stock"
        };
    }

    public string GetLabel(string labelKey, string cultureCode)
    {
        var doc = GetResourceDocument(cultureCode);
        var labelNode = doc?.Root?.Element("ui")?.Element("labels")?.Element(labelKey);
        return labelNode?.Value ?? labelKey;
    }

    public Dictionary<string, string> GetAllCategoryNames(string cultureCode)
    {
        var doc = GetResourceDocument(cultureCode);
        var result = new Dictionary<string, string>();

        var categories = doc?.Root?.Element("categories")?.Elements("category");
        if (categories != null)
        {
            foreach (var category in categories)
            {
                var id = category.Attribute("id")?.Value;
                var name = category.Element("name")?.Value;
                if (id != null && name != null)
                {
                    result[id] = name;
                }
            }
        }

        return result;
    }

    private XDocument? GetResourceDocument(string cultureCode)
    {
        var normalizedCulture = NormalizeCulture(cultureCode);
        
        // Try exact match first
        if (_cachedResources.TryGetValue(normalizedCulture, out var doc))
        {
            return doc;
        }

        // Try language-only match (e.g., "es" for "es-MX")
        var languageCode = normalizedCulture.Split('-')[0];
        if (_cachedResources.TryGetValue(languageCode, out doc))
        {
            return doc;
        }

        // Fallback to default culture
        _logger.LogDebug("Culture {Culture} not found, falling back to {DefaultCulture}", 
            cultureCode, DefaultCulture);
        return _cachedResources.GetValueOrDefault(DefaultCulture);
    }

    private static string NormalizeCulture(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            return DefaultCulture;

        // Handle common variations
        return cultureCode.ToLower() switch
        {
            "es-es" or "es-mx" => "es",
            "fr-fr" or "fr-ca" => "fr",
            "de-de" or "de-at" => "de",
            "ja-jp" => "ja",
            _ => cultureCode
        };
    }
}
