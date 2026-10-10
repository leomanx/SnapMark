# SnapMark 0.5.0 · Eight roadmap work groups

This release follows stability → precise capture → design → project workflow, then history,
OCR/scrolling, recording and packaging. Cloud remains deferred. The table records implemented
workflows and remaining scope; it does not mark the entire 120-topic blueprint complete.

| Group | Delivered in 0.5.0 | Remaining validation / scope |
|---|---|---|
| Stability | Allocation limits, corrupt-history isolation, atomic export failure coverage, async video finalization before exit, single-instance file opening | Mixed DPI/hotplug/HDR/protected content, disk-full hardware scenario, long recording benchmarks |
| Precise capture | Magnifier, physical pixel coordinates/HEX, keyboard frame, visible window picker, Ctrl edge snapping, transparent polygon capture | Selected-window metadata refinement; moving/occluded window capture |
| Design | Flattened-image eyedropper, export-free grid/rulers, installed fonts, bold, multiline alignment, saved typography presets, Ctrl+K command search | Text background/advanced typography; more guide controls |
| Project workflow | Manual project profiles or app-name matches, markup color/personal stamp preset, export folder, `{app}`/`{counter}`, dated subfolders | More application-specific presets; counters advance when Save dialog opens, including cancellation |
| History | App/title/date/name filtering, metadata sidecars, owned-file retention by count and age, resilient old entries | OCR indexing of history |
| OCR / scrolling | Explicit ROI, result text search, Windows preferred-language OCR, bounded auto-scroll; local browser extension source | Thai OCR needs installed language support; broad real-page auto-scroll and extension runtime QA |
| Recording | WASAPI system/mic/both, separate endpoint selectors, native pixel MP4 or aspect-preserving limits, quality, window region, cursor highlight/ripple, configurable GIF | Long-session A/V drift and device disconnect tests; window region stays fixed |
| Packaging | Local package verification/install GUI, startup option, Dark/Light/System, opt-in Explorer action, exporter plugin contract/sample | Plugin contract currently covers export only; signed/network updates remain deferred |

## Video and audio

Record video → choose Region / monitor / all screens / visible window. Choose fps, MP4 resolution,
quality and audio mode: None, System, Microphone or Both. System and microphone have independent
device selectors and initially use Windows default devices. Both mixes the two tracks into stereo
AAC while copying the already encoded video stream. FFmpeg remains an external dependency.

Audio runs in an isolated local helper process. A driver that hangs during initialization is stopped
after 12 seconds with an actionable error; stop/finalization is bounded as well. This keeps native
driver calls from hanging the main app. Invalid/disconnected devices report an error and require
another endpoint or No audio. No Windows audio services/settings are changed by the app.

System audio captures playback from the selected output endpoint; choose the output actually used
by Teams/browser/player. Microphone captures the selected input. Temporary WAV/video files remain
available when finalization fails. Successful finalization removes those temporary files.

The confirmation shows captured **physical pixels** and resulting MP4 dimensions, independent of
Windows display scaling. Native uses the original size; H.264 yuv420p adds one padding pixel to an
odd width/height. For example 65×63 becomes 66×64. Maximum-height choices 720/1080/1440/2160 keep
aspect ratio and never enlarge a smaller capture; 3840×2160 at 1080 becomes 1920×1080. FPS choices
are 10/15/24/30, CRF quality 18/23/28. Capture guards reject more than 64 MP or a side over 16000px.

Visible-window video records a fixed desktop rectangle. Moving, covering or minimizing the target
changes what is visible in that rectangle. This release does not track an occluded window surface.
Cursor effects are visible desktop overlays while recording.

Trim / GIF has configurable GIF fps, maximum width and forever/once/three-repeat playback.
GIF has no audio; MP4 trim retains available audio. Export writes a temporary file before replacing
the requested destination; the source stays intact.

## Capture and design shortcuts

In capture: W selects visible windows; Ctrl while dragging snaps near their edges. Space creates
a keyboard frame; arrows move 1px, Shift+arrows resize, Ctrl makes the step 10px, Enter finishes.
F enables polygon capture: click vertices, Enter finishes; outside the polygon is transparent.
Video/scroll regions disable freeform. Esc cancels all overlays.

In editor: Ctrl+K searches tools and actions. Eyedropper samples the visible flattened composition,
including redaction. Grid/rulers help editing and never appear in copied/exported images.
Text options and personal presets include font family, bold and multiline alignment.

## Profiles, history, OCR

Project/app profiles can map comma-separated process names such as `Revit` to naming, folder,
color and a saved personal style/stamp. An app match wins over the manually selected profile.
Hotkey capture obtains foreground app metadata before hiding SnapMark. Main-button captures may
identify SnapMark, and picking another window does not yet update that metadata to the picked app.
Settings supports date folders, age retention, theme and startup; theme/recovery changes need restart.

History stores small JSON sidecars beside owned capture PNGs. Search matches filename, date, app
and window title. Age/count cleanup touches generated SnapMark files and matching sidecars only.
Existing captures without metadata remain available; OCR text is not indexed yet.

OCR ROI is relative to the flattened/cropped image. Auto chooses from Windows user language
preferences, rather than detecting the language from pixels. English is validated on this machine;
Thai language support was unavailable and no Windows language/privacy settings were changed.
Auto scroll stops on unchanged frames, overlap failure, cancellation or the bounded frame limit;
manual scrolling remains available. General application/page compatibility still needs QA.

## Browser full-page capture (optional, local)

Source: `extensions/browser-capture`. Load this folder as an unpacked extension through Chrome/Edge's
extension management page when desired. The extension is not installed automatically. On an HTTP(S)
page click its icon then capture; save the PNG locally and drag it into SnapMark or Open image.
It uses activeTab/debugger/downloads, detaches before the Save dialog, bounds actual/predicted image
size and has no cloud destination. JavaScript syntax was checked; browser runtime QA remains open.

## Installation and plugins

Main → Install / update local package → choose an extracted release folder with its manifest.
SHA-256 detects missing/changed payload; it is not a publisher signature. The installer creates
a new per-user version folder and Start Menu shortcut, preserving old version folders. Portable
data and installed `%APPDATA%\SnapMark` data are separate; installation does not migrate captures.
Startup and Explorer registration are opt-in controls. No registry integration was activated in QA.

`SnapMark.Core.ISnapMarkExporter` accepts flattened PNG bytes and dimensions. Compile a trusted
.NET 8 Windows assembly against the matching Core project; see repository
`examples/SnapMark.PngExporter`. The portable package includes the example source for reference.
Load plugin from the main window, then Plugin export in editor. Plugins run local code with user
permissions and require explicit trust; assemblies are not auto-loaded. Export plugins receive the
flattened result, including redaction. Tool/capture/share plugin APIs are future work.

## Evidence and limits

Final Release build: zero warnings/errors. Final automated run: **180 assertions passed**, including real desktop video/image capture, decode, synthetic audio trim/GIF and the helper lifecycle. This count excludes the unavailable real audio hardware recheck.

Release build and automated assertion counts are recorded in `artifacts/roadmap-validation-final.log`.
Tests include actual desktop capture, MP4 decode, font
project round-trip/alignment, freeform alpha, installer hash/traversal/idempotency, locked export,
corrupt history, helper-process readiness/stop/unavailable-device handling and the compiled exporter
example. Earlier in this work session, the in-process audio implementation passed three real hardware
checks: simultaneous system/mic WAV writes, a played loopback tone after silence, and Both-mode AAC
MP4 with exact padded dimensions. The final hardware recheck encountered hanging/invalidated devices;
Windows subsequently reported zero active output/input endpoints. Those failures prompted the helper
process isolation. **Final helper-based simultaneous real audio recording remains unverified until
usable endpoints return.** No service restart or device-setting change was performed. Probe details
are retained in `artifacts/audio-*-validation.log` and `artifacts/audio-worker-validation.log`.
Intelligible voice playback and long-duration drift still need listening tests.
Light theme, keyboard overlay and video setup received direct UI inspection. Multi-device hardware
coverage, Thai OCR, browser extension runtime and long sessions are explicitly unverified.
