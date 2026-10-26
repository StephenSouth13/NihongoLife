# Publish the WebGL build

Unity is not built in GitHub Actions. There is no Unity license on the hosted runners. Build on a machine with **Unity 6000.3.12f1**, then upload the output folder.

## 1. Build

From the project folder `NihongoLife/`:

```
Unity -batchmode -nographics -quit -projectPath . -executeMethod NihongoLife.Editor.ReleaseBuild.BuildWebGL
```

The output is `Build/Release/WebGL/` next to the Unity project (or `NIHONGO_BUILD_ROOT` when that variable is set). The player build writes:

- `index.html` and `TemplateData/` from `Assets/WebGLTemplates/NihongoLife`
- `Build/` (loader, framework, wasm, data)
- `zones/index.json` and the zone bundles
- `_headers` (Netlify and Cloudflare Pages)
- `.htaccess` (Apache)
- `vercel.json` (Vercel)

Open that folder. `tools/publish-webgl.sh` checks that those pieces are present.

## 2. Brotli

Compression is Brotli **with decompression fallback**. The `.unityweb` files are compressed, and Unity's loader inflates them. Do **not** send `Content-Encoding: br`. If the host also inflates the body, the loader fails.

Do not pre-compress the files a second time. Do not add a service-worker that rewrites `.unityweb`.

MIME types that matter:

- `.wasm` → `application/wasm`
- `.js` → `application/javascript`

Cache:

- `Build/*` and `zones/*` except `zones/index.json` can be cached for a year (the names are hashed).
- `index.html` and `zones/index.json` must be `no-cache`, or a new deploy will mix an old page with a new build.

## 3. Hosts

Publish the **whole** output folder, including `zones/`. The city is not inside the first download.

**Netlify** or **Cloudflare Pages**: drag the folder, or `netlify deploy --dir=Build/Release/WebGL --prod`. `_headers` is picked up automatically.

**Vercel**: set the project root to the output folder so `vercel.json` is at the root of the deployment. `vercel --prod` from that folder.

**GitHub Pages**: Pages cannot set `Content-Encoding` or a custom wasm MIME on every plan. Fallback decompression covers the first. wasm is usually served as `application/wasm` already. Upload the folder as the Pages site (a `gh-pages` branch or a Pages artifact). Do not force-push over a branch you have not checked.

**Apache**: the generated `.htaccess` sets the MIME types and cache headers. It does not set `Content-Encoding`.

## 4. Check the live site

1. The loading screen shows 日本語ライフ, a gold progress bar, and a percent. Reduced-motion in the OS stops the falling petals.
2. The canvas fills the window, including a phone with a notch (the page pads `safe-area-inset`).
3. Fullscreen appears after the instance starts.
4. A phone in portrait shows a dismissible hint. The game still runs in portrait. Turning the phone does not lock orientation.
5. DevTools shows one `.wasm` response with a wasm content type, and no `Content-Encoding: br` on `.unityweb`.
6. Entering the city downloads a file under `zones/`. Reloading `index.html` does not keep serving a stale `zones/index.json`.
