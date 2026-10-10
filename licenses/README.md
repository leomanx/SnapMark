# Third-party components in the portable build

SnapMark's original code and artwork use the custom source-available license in
[../LICENSE](../LICENSE), which permits business use but restricts software resale.
The files here describe third-party components; those components retain their
own terms without SnapMark's resale restriction.

- Self-contained .NET and Windows Desktop runtime 8.0.25: license/notice files copied from the
  exact NuGet runtime packs used by this build. On a runtime upgrade, refresh these files from
  the matching packs before publishing.
- C#/WinRT projections and support: [Microsoft CsWinRT license](https://github.com/microsoft/CsWinRT/blob/master/LICENSE).
- Windows SDK targeting pack: license terms downloaded from its NuGet metadata link
  [Microsoft Windows SDK license](https://aka.ms/WinSDKLicenseURL). The app uses installed Windows
  PDF/OCR engines and distributes the managed projections in the self-contained executable.
- FFmpeg is external and is not included in this release. Its license depends on the user's
  chosen build. No Pantone color library or proprietary Snagit assets are included.
- NAudio WASAPI 2.2.1 and its Core dependency: MIT license in `NAudio-2.2.1-LICENSE.txt`.

These notices accompany the portable and locally installed packages.
