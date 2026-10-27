using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ExternalLinks.Configuration;

/// <summary>Plugin configuration.</summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets manual mappings, one per line: <c>imdbId = fawesomeIdOrUrl</c>.</summary>
    public string Mappings { get; set; } = string.Empty;
}
