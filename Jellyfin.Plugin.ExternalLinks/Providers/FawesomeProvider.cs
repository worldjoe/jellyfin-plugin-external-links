using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.ExternalLinks.Providers;

/// <summary>Fawesome (fawesome.tv) links.</summary>
public sealed partial class FawesomeProvider : IExternalLinkProvider
{
    /// <inheritdoc />
    public string Name => "Fawesome";

    /// <inheritdoc />
    public string? Resolve(string itemName, string? path, string? imdbId, IReadOnlyDictionary<string, string> manual)
    {
        // 1. Filename tag: "Title (2019) [fawesome-10647548].strm"
        if (!string.IsNullOrEmpty(path))
        {
            var m = TagRegex().Match(path);
            if (m.Success)
            {
                return Build(m.Groups[1].Value, itemName);
            }
        }

        // 2. Manual mapping by IMDb id.
        if (!string.IsNullOrEmpty(imdbId) && manual.TryGetValue(imdbId.ToLowerInvariant(), out var target))
        {
            return Build(target, itemName);
        }

        return null;
    }

    private static string? Build(string idOrUrl, string itemName)
    {
        idOrUrl = idOrUrl.Trim();
        if (Uri.TryCreate(idOrUrl, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && (uri.Host == "fawesome.tv" || uri.Host.EndsWith(".fawesome.tv", StringComparison.Ordinal)))
        {
            return uri.AbsoluteUri;
        }

        if (!DigitsRegex().IsMatch(idOrUrl))
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var c in itemName.ToLower(CultureInfo.InvariantCulture))
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        var slug = sb.ToString().Trim('-');
        return "https://fawesome.tv/movies/" + idOrUrl + (slug.Length > 0 ? "/" + slug : string.Empty);
    }

    [GeneratedRegex(@"\[fawesome-(\d+)\]", RegexOptions.IgnoreCase)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsRegex();
}
