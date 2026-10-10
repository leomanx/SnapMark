# Exporter plugins v1 — actual supported API

The home Plugins button loads a selected local .NET DLL after a trust prompt.
The editor OUTPUT → Plugin export command selects a loaded exporter and an
output path. SnapMark renders the current image plus annotations, encodes PNG
bytes, and calls `Export(FlattenedImage image, string path)`.

The contract in `src/SnapMark.Core/PluginApi.cs` is:

```csharp
public sealed record FlattenedImage(byte[] Png, int Width, int Height);
public interface ISnapMarkExporter
{
    string Name { get; }
    string Extension { get; }
    void Export(FlattenedImage flattenedImage, string path);
}
```

Check the source declaration when building; the snippet describes the shape of
the API. Provide a public concrete class with a public parameterless constructor.
Name must be nonempty and at most 80 characters. Extension must be a dot followed
by 1–12 alphanumeric characters. At most 50 exporters can be loaded per session.
Loading the same DLL path twice is ignored. Restart unloads plugins; loading is
manual per session and there is no marketplace or automatic discovery.

## Build and try the included example

From the source checkout with Windows/.NET 8 SDK:

```powershell
dotnet build examples/SnapMark.PngExporter -c Release
dotnet run --project tests/SnapMark.Tests -c Release -- --plugin examples/SnapMark.PngExporter/bin/Release/net8.0-windows10.0.19041.0/SnapMark.PngExporter.dll
```

In SnapMark → Plugins, choose that `SnapMark.PngExporter.dll`, accept the
trust prompt, open an image, add markup, then Plugin export → Example · Flattened
PNG. This deliberately simple exporter demonstrates a real loaded DLL and output.
Its compiled DLL, source and project file are included in the portable example folder; building
requires the matching source checkout's Core project and Directory.Build.props.

Build against the matching SnapMark.Core version. Copy the exporter DLL rather
than a second mismatched Core assembly. Plugin dependency resolution/package
versioning is limited; the tiny example has no additional dependencies. A portable
consumer can load a built DLL without installing the SDK.

## What can be extended

Current: alternate image export/encoding, report output from a flattened image,
or custom output naming inside an exporter. Saving respects the path passed by
the app only if the plugin implementation does so.

Not exposed: editable objects, adding drawing tools/Ribbon buttons, capture or
recording providers, editor selection, undo commands, history or share-adapter APIs.
Those require a future SDK; none is implied by the current Plugins button.
Export runs synchronously on the UI thread. Exceptions are shown, but a hanging
plugin can freeze the UI and a process-level failure can terminate SnapMark.

The API passes flattened pixels, not original history, but this is NOT a security
boundary: DLL code can access files/network under the user's permissions. See
[Security](../SECURITY.md). Separate process execution, permission controls,
dependency isolation and signed/versioned packages are future work.
