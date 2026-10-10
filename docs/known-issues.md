# Known issues and verification limits — 0.6.12

- The large 120-topic blueprint is a product/system design catalog, not a list
  of 120 completed features. See the current [roadmap](design-roadmap.md).
- The user confirmed JLAB microphone recording works. Final simultaneous
  system audio + JLAB listening checks and long-session drift are still open.
  Audio privacy settings and device/security policy can prevent capture.
- Mixed-DPI monitor transitions, disconnects, HDR and long sessions need a
  wider hardware matrix. A clean Windows machine release check is pending.
- Capture reads visible desktop pixels. Covered, minimized, protected or secure
  content cannot be assumed to capture correctly; this is not background capture.
- OCR depends on installed Windows languages; Thai accuracy needs real fixtures.
- Automatic scrolling exists, but page overlap/content and browser extension
  setup need broader real-page validation. It is not a universal scroll engine.
- Plugin v1 supports only flattened-image exporters. DLLs execute with user
  permissions inside SnapMark and can block or crash it. Restart to unload.
- Measurements use one calibrated image scale, not BIM geometry or perspective
  correction. Verify source drawing scale and units before professional reliance.
- FFmpeg is external and not bundled; recording/trim/GIF require a suitable build.
- Team folders uses existing local sync plus provider web pages. OAuth, automatic
  access control and direct Teams/ACC uploads are not implemented.
- Portable avoids an installation requirement; it does not bypass company policy.
  The executable is unsigned. No automatic updater is provided.

Report a reproducible problem with version, steps, expected/actual behavior,
monitor/DPI information and a synthetic sample. Avoid uploading private drawings,
original history or microphone recordings; see [Security](../SECURITY.md).
