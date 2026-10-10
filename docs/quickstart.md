# SnapMark portable quick start — 0.6.12

1. Extract the whole ZIP to a writable folder on Windows x64. Keep
   `portable.flag` beside `SnapMark.exe`; run the executable directly. The app
   includes its .NET runtime. Installation and administrator elevation are not
   required by SnapMark, although company application policies still apply.
2. Press PrintScreen, drag a region, and mark up the captured image. If the
   hotkey is occupied, use Capture region on the home screen.
3. Select (V) an object to edit its properties. Drag from blank canvas to select
   several objects; Delete removes them, Ctrl+Z restores them. Drag tool buttons
   to reorder them and drag the sidebar dividers to resize the tool list.
4. Ctrl+C copies the flattened result. Ctrl+S saves an image; Save editable
   project preserves the image and objects in `.snapmark` for later editing.
5. History thumbnails are below the canvas. Drag their horizontal divider to
   resize the strip; right-click Rename to create a searchable name. Their
   delete button sends the selected capture to the Windows Recycle Bin.
6. Settings → Storage offers Browse for image, video, history, recovery and
   project folders. Changing a path does not move existing files. Recovery
   folder changes apply after restarting.
7. For MP4/Trim/GIF, Settings → Video encoder → Browse selects a trusted
   `ffmpeg.exe`. Auto searches beside the app, its `tools` folder, then PATH.
   Select a recording region, adjust its corners, then start. The boundary
   remains visible; Ctrl+Shift+F10 stops/finalizes recording.
8. Team folders saves copies into an existing Google Drive/OneDrive synced
   local folder and opens the configured provider share page. Configure sharing
   permissions with the provider; SnapMark does not log in or grant access.

Closing the home window leaves SnapMark in the system tray. Use tray → Exit
to exit. Before sharing sensitive captures, read [Security](../SECURITY.md):
history and editable projects retain original pixels before redaction.

See [portable updates](portable-release.md), [known issues](known-issues.md)
and [exporter plugins](plugins.md). Use in companies and paid design work is
permitted; resale of SnapMark is restricted by [LICENSE](../LICENSE).
