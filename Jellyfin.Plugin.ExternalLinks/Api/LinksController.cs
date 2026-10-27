using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.ExternalLinks.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ExternalLinks.Api;

/// <summary>External link endpoints.</summary>
[ApiController]
[Route("ExternalLinks")]
public class LinksController : ControllerBase
{
    private static readonly IExternalLinkProvider[] Providers = { new FawesomeProvider() };

    private readonly ILibraryManager _library;

    /// <summary>Initializes a new instance of the <see cref="LinksController"/> class.</summary>
    /// <param name="library">Library manager.</param>
    public LinksController(ILibraryManager library)
    {
        _library = library;
    }

    /// <summary>Serves the client script that redirects Play to external URLs.</summary>
    /// <returns>JavaScript.</returns>
    [HttpGet("script.js")]
    [AllowAnonymous]
    public ActionResult Script()
    {
        var name = typeof(Plugin).Namespace + ".Web.externalLinks.js";
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-cache";
        return File(stream, "application/javascript");
    }

    /// <summary>Gets a map of item id (N format) to external URL for all linked items.</summary>
    /// <returns>Map.</returns>
    [HttpGet("Links")]
    [Authorize]
    public ActionResult<Dictionary<string, string>> GetLinks()
    {
        var manual = ParseMappings(Plugin.Instance.Configuration.Mappings);
        var items = _library.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Episode },
            Recursive = true
        });

        var result = new Dictionary<string, string>();
        foreach (var item in items)
        {
            var imdb = item.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Imdb);
            foreach (var p in Providers)
            {
                var url = p.Resolve(item.Name, item.Path, imdb, manual);
                if (url is not null)
                {
                    result[item.Id.ToString("N")] = url;
                    break;
                }
            }
        }

        return result;
    }

    private static Dictionary<string, string> ParseMappings(string text)
    {
        var map = new Dictionary<string, string>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            var i = line.IndexOf('=', StringComparison.Ordinal);
            if (line.StartsWith('#') || i <= 0)
            {
                continue;
            }

            map[line[..i].Trim().ToLowerInvariant()] = line[(i + 1)..].Trim();
        }

        return map;
    }
}
