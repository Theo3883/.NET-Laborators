using Lab3.Model;
using Lab3.Models;

namespace Lab3.Services;

public interface IOrderLocalizationService
{
    /// <summary>
    /// Get localized category name for a specific culture
    /// </summary>
    string GetLocalizedCategoryName(OrderCategory category, string culture);
    
    /// <summary>
    /// Get localized category description for a specific culture
    /// </summary>
    string GetLocalizedCategoryDescription(OrderCategory category, string culture);
    
    /// <summary>
    /// Get localized common term
    /// </summary>
    string GetLocalizedTerm(string key, string culture);
    
    /// <summary>
    /// Get all supported cultures
    /// </summary>
    IEnumerable<string> GetSupportedCultures();
    
    /// <summary>
    /// Check if a culture is supported
    /// </summary>
    bool IsCultureSupported(string culture);
    
    /// <summary>
    /// Get all localized category information for a culture
    /// </summary>
    IEnumerable<LocalizedCategoryInfo> GetAllLocalizedCategories(string culture);
}
