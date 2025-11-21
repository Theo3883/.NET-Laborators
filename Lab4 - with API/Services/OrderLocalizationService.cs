using System.Xml.Linq;
using Lab3.Model;
using Lab3.Models;

namespace Lab3.Services;

public class OrderLocalizationService : IOrderLocalizationService
{
    private readonly Dictionary<string, XDocument> _resourceFiles = new();
    private readonly ILogger<OrderLocalizationService> _logger;
    private const string DefaultCulture = "en-US";
    private readonly string _resourcePath;

    public OrderLocalizationService(ILogger<OrderLocalizationService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _resourcePath = Path.Combine(environment.ContentRootPath, "Resources");
        LoadResourceFiles();
    }

    private void LoadResourceFiles()
    {
        try
        {
            if (!Directory.Exists(_resourcePath))
            {
                _logger.LogWarning("Resources directory not found at {ResourcePath}", _resourcePath);
                return;
            }

            var xmlFiles = Directory.GetFiles(_resourcePath, "OrderMetadata.*.xml");
            
            foreach (var file in xmlFiles)
            {
                var fileName = Path.GetFileName(file);
                var culture = fileName.Replace("OrderMetadata.", "").Replace(".xml", "");
                
                try
                {
                    var doc = XDocument.Load(file);
                    _resourceFiles[culture] = doc;
                    _logger.LogInformation("Loaded resource file for culture: {Culture}", culture);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading resource file: {File}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} resource files", _resourceFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading resource files from {ResourcePath}", _resourcePath);
        }
    }

    public string GetLocalizedCategoryName(OrderCategory category, string culture)
    {
        var requestedCulture = culture ?? DefaultCulture;
        
        // Try requested culture first
        var categoryName = GetCategoryNameFromResource(category, requestedCulture);
        if (categoryName != null)
            return categoryName;

        // Fallback to default culture
        if (requestedCulture != DefaultCulture)
        {
            _logger.LogWarning(
                "Category translation not found for {Category} in culture {Culture}, falling back to {DefaultCulture}",
                category, requestedCulture, DefaultCulture);
            
            categoryName = GetCategoryNameFromResource(category, DefaultCulture);
            if (categoryName != null)
                return categoryName;
        }

        // Final fallback to enum value with formatting
        _logger.LogWarning(
            "Category translation not found for {Category} in any culture, using default formatting",
            category);
        
        return FormatCategoryName(category);
    }

    public string GetLocalizedCategoryDescription(OrderCategory category, string culture)
    {
        var requestedCulture = culture ?? DefaultCulture;
        
        // Try requested culture first
        var description = GetCategoryDescriptionFromResource(category, requestedCulture);
        if (description != null)
            return description;

        // Fallback to default culture
        if (requestedCulture != DefaultCulture)
        {
            description = GetCategoryDescriptionFromResource(category, DefaultCulture);
            if (description != null)
                return description;
        }

        // Final fallback
        return $"Description for {category} category";
    }

    public string GetLocalizedTerm(string key, string culture)
    {
        var requestedCulture = culture ?? DefaultCulture;
        
        // Try requested culture first
        var term = GetTermFromResource(key, requestedCulture);
        if (term != null)
            return term;

        // Fallback to default culture
        if (requestedCulture != DefaultCulture)
        {
            term = GetTermFromResource(key, DefaultCulture);
            if (term != null)
                return term;
        }

        // Final fallback to key itself
        return key;
    }

    public IEnumerable<string> GetSupportedCultures()
    {
        return _resourceFiles.Keys.OrderBy(c => c);
    }

    public bool IsCultureSupported(string culture)
    {
        return _resourceFiles.ContainsKey(culture);
    }

    public IEnumerable<LocalizedCategoryInfo> GetAllLocalizedCategories(string culture)
    {
        var requestedCulture = culture ?? DefaultCulture;
        var result = new List<LocalizedCategoryInfo>();

        foreach (OrderCategory category in Enum.GetValues(typeof(OrderCategory)))
        {
            result.Add(new LocalizedCategoryInfo
            {
                CategoryKey = category.ToString(),
                LocalizedCategoryName = GetLocalizedCategoryName(category, requestedCulture),
                CategoryDescription = GetLocalizedCategoryDescription(category, requestedCulture),
                Culture = requestedCulture
            });
        }

        return result;
    }

    private string? GetCategoryNameFromResource(OrderCategory category, string culture)
    {
        if (!_resourceFiles.TryGetValue(culture, out var doc))
            return null;

        var categoryKey = category.ToString();
        var categoryElement = doc.Root?
            .Element("CategoryTranslations")?
            .Elements("Category")
            .FirstOrDefault(e => e.Attribute("Key")?.Value == categoryKey);

        return categoryElement?.Value;
    }

    private string? GetCategoryDescriptionFromResource(OrderCategory category, string culture)
    {
        if (!_resourceFiles.TryGetValue(culture, out var doc))
            return null;

        var categoryKey = category.ToString();
        var descriptionElement = doc.Root?
            .Element("Descriptions")?
            .Elements("CategoryDescription")
            .FirstOrDefault(e => e.Attribute("Category")?.Value == categoryKey);

        return descriptionElement?.Value;
    }

    private string? GetTermFromResource(string key, string culture)
    {
        if (!_resourceFiles.TryGetValue(culture, out var doc))
            return null;

        var termElement = doc.Root?
            .Element("CommonTerms")?
            .Elements("Term")
            .FirstOrDefault(e => e.Attribute("Key")?.Value == key);

        return termElement?.Value;
    }

    private static string FormatCategoryName(OrderCategory category)
    {
        return category switch
        {
            OrderCategory.Fiction => "Fiction & Literature",
            OrderCategory.NonFiction => "Non-Fiction",
            OrderCategory.Technical => "Technical & Professional",
            OrderCategory.Children => "Children's Books",
            _ => category.ToString()
        };
    }
}
