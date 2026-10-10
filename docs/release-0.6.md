# SnapMark 0.6.0 · Direct tools, recording boundary and team folders

- Seven main tool categories are direct buttons in a darker row above the canvas.
  Subtools remain in the left panel. Switching categories remembers the last tool;
  shortcuts can select tools without the category resetting them.
- Text background: enable, color wheel/HEX, opacity percentage and padding.
  Apply to existing selected text; undo restores the prior style. Editable projects
  and personal presets retain the background. Text/typography controls are contextual.
- Recording has a translucent cyan boundary above ordinary windows. Four click-through,
  non-activating strips sit outside the selected physical pixels, so they are not
  part of the MP4. At a desktop edge that strip is clipped; full-desktop recording
  has no exterior space for a frame. The region stays fixed when windows move.
- History → Index image text reads images locally with installed Windows OCR languages.
  Search then matches recognized text as well as app/title/date/name. Language-specific
  sidecars persist results; a replaced/modified image invalidates its cache. Indexing
  can be cancelled between recognition operations. Missing images are skipped.
  Thai OCR requires installed Thai language support and remains unverified here.
- Stability: history decodes small thumbnails rather than every full-resolution image,
  tolerates images disappearing after enumeration and corrupt OCR sidecars. Text background
  values are validated when loading projects/presets. Indexing writes its cache atomically.
  Existing dimension/recovery/export safeguards remain covered. Mixed-DPI/hotplug/HDR,
  disk-full and long-session hardware checks still remain in the broader roadmap.

## Google Drive / OneDrive team workflow

Main or Editor → **Team folders**. Browse to an existing synced team folder for each
provider and optionally paste its shared-folder web link. Save connection persists both.
No OAuth registration is needed in SnapMark for this workflow: the desktop sync client
must already be signed in and configured by the user.

Editor can save a flattened PNG or a separate editable `.snapmark` copy. Main/Editor
can also copy an existing image, MP4, PDF or project into that folder. Unique filenames
avoid overwriting another teammate's output. Editable copies include the original image.
These team copies do not mark the editor's local project/recovery as saved.

**Open sharing page** opens the configured shared folder, or the provider home page
when no link is configured. **Copy folder link** copies only the configured folder URL.
Manage access and obtain individual file links on the provider website or in Explorer.
SnapMark does not automatically change sharing permissions or produce a file sharing URL.
Saving a local file does not prove upload completion; check Drive/OneDrive sync status.
No team account, folder or public permission was configured during testing.

References: [Google Drive for desktop](https://support.google.com/drive/answer/10838124),
[OneDrive sync](https://support.microsoft.com/en-us/onedrive/sync-your-computer-s-files-and-folders-with-onedrive),
[OneDrive sharing through Explorer](https://support.microsoft.com/en-gb/windows/experience/connectivity-networking/share-files-in-windows).

## Validation

224 assertions passed: real desktop capture/video/decode, synthetic media, actual English
OCR indexing, invalid/stale cache handling, background rendering/project/preset/undo,
frame geometry at negative/edge coordinates, provider URL validation and UI model checks
for every category and remembered subtools, actual boundary HWND placement/cleanup, and
async team-file transfer/cancellation. Text background and team-folder layouts received
direct UI inspection; sample text with a yellow background was applied successfully.
No sound hardware recheck in this release.
Logs: `artifacts/0.6-final-validation.log` and `artifacts/0.6-build.log`.
JLAB audio was user-confirmed working after 0.5.1; further audio work is deferred as requested.
