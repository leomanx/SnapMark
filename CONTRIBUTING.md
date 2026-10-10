# Contributing to SnapMark

Contributions to this source-available project remain under [LICENSE](LICENSE),
including the no-software-resale restriction. Commercial use of the app for
professional work is permitted. Third-party dependencies retain their own terms.
Only contribute code/assets you have permission to redistribute under these terms.

Use Windows x64 and .NET 8 SDK. Build `SnapMark.sln` in Release, then run
`dotnet run --project tests/SnapMark.Tests -c Release -- --ui-model`.
Desktop/video checks additionally use `--capture --recording` and require FFmpeg.
Do not run microphone checks without the operator's agreement.

For changes, describe the problem, final behavior, validation and remaining limits.
Keep preview and export on the shared renderer. Include meaningful regression checks
for geometry, persistence, redaction and lifecycle changes. Keep all user input paths
clearly labeled. Test cancellation and undo where applicable. Do not distribute
private captures, `data`, OAuth credentials, proprietary symbols or Pantone assets.

New plugin capabilities require explicit API design; v1 currently accepts only
flattened-image exporters. Read [plugins](docs/plugins.md) before proposing tools,
capture providers or share adapters. Dependencies need license notices and a reason.

Repository: https://github.com/leomanx/SnapMark. Use GitHub Issues for reproducible
bugs and suggestions, and pull requests for proposed changes. Security-sensitive
details should follow SECURITY.md instead of being posted publicly.
