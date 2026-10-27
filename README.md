# Jellyfin External Links Plugin

Keep a movie/episode in your Jellyfin library (with full metadata and artwork), but open an external site when you press **Play**. First supported source: [Fawesome](https://fawesome.tv).

Target: Jellyfin 12.4 (`net10.0`).

## How it works

1. **Library stub.** Create a `.strm` file per title. Its contents are a dummy URL. Jellyfin scans it as a normal movie and fetches metadata/artwork (e.g. from the IMDb id in the filename).
2. **Link mapping.** The plugin maps each item to a Fawesome URL, either from a filename tag or from manual mappings on the settings page.
3. **Client redirect.** A script served by the plugin intercepts Play/Resume clicks in jellyfin-web. For linked items it opens the external URL in a new tab; other items play normally.

## Install

1. Build: `dotnet build -c Release Jellyfin.Plugin.ExternalLinks`
2. Copy `Jellyfin.Plugin.ExternalLinks\bin\Release\net10.0\Jellyfin.Plugin.ExternalLinks.dll` to `<jellyfin data>\plugins\ExternalLinks_1.0.0.0\`.
3. Restart Jellyfin.
   Each plugin needs its own folder (one `meta.json`/DLL set per folder). Don't drop the DLL into another plugin's folder, or it won't be listed separately.
4. Install the **JavaScript Injector** plugin and paste this into its code box (leave Script URL empty). Do not paste an HTML `<script>` tag:

   ```js
   const s = document.createElement('script'); s.src = '/ExternalLinks/script.js'; s.defer = true; document.head.appendChild(s);
   ```

   If Jellyfin runs under a base path (e.g. `/jellyfin`), include it in `s.src`: `/jellyfin/ExternalLinks/script.js`. The script itself respects the base path for its API calls.

   The injector's "Script URL" field did not load the script in testing; use the snippet above.

   After a hard refresh, the browser console should show `[ExternalLinks] script loaded`.

## Usage

### Option A: filename tag

Create a stub named:

```
After School Lunch Special (2019) [imdbid-tt9073068] [fawesome-10647548].strm
```

`10647548` is the id from `https://fawesome.tv/movies/10647548/after-school-lunch-special`. The slug is generated from the item title.

### Option B: manual mapping

Dashboard > Plugins > External Links. One mapping per line, `imdbId = fawesomeId` or a full `https://fawesome.tv/...` URL. Lines starting with `#` are ignored.

```
tt9073068 = 10647548
tt1234567 = https://fawesome.tv/movies/123/some-title
```

Filename tags take priority over manual mappings. Only https `fawesome.tv` URLs are accepted.

Then rescan the library.

## API

| Endpoint | Auth | Description |
|---|---|---|
| `GET /ExternalLinks/script.js` | anonymous | Client redirect script |
| `GET /ExternalLinks/Links` | logged in | Map of item id (N format) to external URL, for all linked movies/episodes |

## Adding a source

Implement `IExternalLinkProvider` (`Providers/IExternalLinkProvider.cs`) and add it to the `Providers` array in `Api/LinksController.cs`.

## Limitations

- **Web client only.** Native apps (Android TV, iOS, Roku, etc.) can't run injected JS, so Play there tries the dummy stub and fails.
- Fawesome ids can't be derived from IMDb ids; you supply them.
- The link map is cached client-side for 60 seconds, so new items take up to a minute to redirect.
- Play-button selectors (`.btnPlay`, `.btnResume`, `[data-action=play|resume]`, `.cardOverlayFab-primary`) may need adjusting for your jellyfin-web version; see `Web/externalLinks.js`.
- Not yet tested end to end on a live server.
