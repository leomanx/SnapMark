# SnapMark 0.5.1 · Dimension/calibration and audio troubleshooting

## Dimension and calibration fix

The editor previously opened a modal text/calibration dialog while the canvas still held mouse
capture and the draft gesture. Focus changes could clear the draft through LostMouseCapture;
another mouse-up during the nested modal loop could also reuse the pending gesture. Apply then
accessed missing state or repeated the prompt. This is unrelated to metric unit coverage.

The editor now snapshots the finished gesture, ends it and releases mouse capture **before**
showing a dialog. Apply uses that immutable snapshot. Cancel leaves the scale/document intact.
Double-click text editing also retains its edit target across focus/capture changes.

Calibrate scale has separate positive length and editable unit fields: mm/cm/m/px/in/ft.
Invalid, zero and non-finite values keep the dialog open with an error. Apply calibration is a
distinct button. The scale is numeric units per pixel; no unit is limited to integer values.
Dimension label remains a manually entered label. Distance/length/area use the image calibration.

Direct UI QA: dragged a dimension and applied `3500 mm`; then calibrated a 244.32 px reference
to 200 mm. The dialog closed, status showed approximately 0.8186 mm/px and the editor stayed usable.
Regression tests cover modal capture loss/reentrant completion, single undo, cancellation,
calibration undo, metric/imperial units and invalid numeric input.

## Audio diagnostics

Video setup now has Refresh audio devices and Test selected audio (2 seconds), operating on the
selected System/Microphone/Both devices. The test deletes only its own temporary test WAVs and
does not alter Windows device settings. It verifies opening/finalization, rather than intelligible
voice or A/V synchronization. Record and listen to a short MP4 to check those.

Errors identify the failing device and Windows HRESULT. Internal stack traces go to the helper's
diagnostic error stream. The helper starts capture off the WPF synchronization context, and stop
waits for capture completion before disposing writers. Existing process timeouts remain in place.

User-selected microphone: **JLAB TALK PRO**. Windows reports four output endpoints and two inputs.
The user confirms Sound Recorder can record from the microphone. Automated/native SnapMark probes
have encountered `REGDB_E_CLASSNOTREG (0x80040154)` at microphone GetMixFormat and a duplicate
endpoint error during system initialization. MediaCapture, AudioGraph, WinMM and DirectShow probes
did not produce a validated alternative; experimental backends are not shipped.
Subsequent user check (2026-10-05): video recording with JLAB microphone audio works. The user
suspects Kaspersky permission was involved; that cause is not established. The failed probes above
remain historical evidence, rather than the current microphone status. Simultaneous system + JLAB
audio, intelligible playback and long-session synchronization still need confirmation.
No service restart, driver
installation or microphone/privacy setting change was performed.

Final validation: 192 assertions passed, including gesture regressions, desktop capture, video,
synthetic media and exporter plugin checks.
Validation logs: `artifacts/hotfix-0.5.1-final-validation.log`, `artifacts/hotfix-0.5.1-build.log`,
`artifacts/audio-recheck-detail.log` and the other local audio probe logs. The assertion run excludes
the failing hardware audio recheck; successful synthetic mux/media tests do not substitute for it.
