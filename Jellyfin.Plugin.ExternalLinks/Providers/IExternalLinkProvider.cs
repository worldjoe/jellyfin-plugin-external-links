using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalLinks.Providers;

/// <summary>Resolves an external playback URL for a library item.</summary>
public interface IExternalLinkProvider
{
    /// <summary>Gets the provider name.</summary>
    string Name { get; }

    /// <summary>Resolves a URL.</summary>
    /// <param name="itemName">Item display name.</param>
    /// <param name="path">Item file path, if any.</param>
    /// <param name="imdbId">Item IMDb id, if any.</param>
    /// <param name="manual">Manual mappings keyed by lowercase IMDb id.</param>
    /// <returns>URL or null.</returns>
    string? Resolve(string itemName, string? path, string? imdbId, IReadOnlyDictionary<string, string> manual);
}
