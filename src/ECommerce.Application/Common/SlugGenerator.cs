using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ECommerce.Application.Common;

/// <summary>Genera slugs amigables para URL a partir de un texto.</summary>
public static class SlugGenerator
{
    /// <summary>Convierte "Café Colombiano 500g" en "cafe-colombiano-500g".</summary>
    public static string Generate(string input, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC);
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');

        return slug.Length > maxLength ? slug[..maxLength].Trim('-') : slug;
    }
}
