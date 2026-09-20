using System.Reflection;
using PdfSharp.Fonts;

namespace AssetManagement.Application.ImportExport;

/// <summary>
/// PdfSharp 6 has no OS font resolver inside the Linux container this app deploys to (Docker Compose) —
/// it throws unless <see cref="GlobalFontSettings.FontResolver"/> is set explicitly (see ADR 0011).
/// Bundles Liberation Sans (SIL Open Font License 1.1, metric-compatible with Arial — see
/// <c>ImportExport/Fonts/LICENSE-LiberationFonts.txt</c>) as an embedded resource so PDF export never
/// depends on fonts being installed on the host.
/// </summary>
public sealed class EmbeddedFontResolver : IFontResolver
{
    public const string FamilyName = "Liberation Sans";

    private const string RegularFaceName = "LiberationSans#Regular";
    private const string BoldFaceName = "LiberationSans#Bold";

    public static void EnsureRegistered()
    {
        GlobalFontSettings.FontResolver ??= new EmbeddedFontResolver();
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var faceName = isBold ? BoldFaceName : RegularFaceName;
        return new FontResolverInfo(faceName);
    }

    public byte[] GetFont(string faceName) => faceName switch
    {
        BoldFaceName => ReadEmbeddedFont("LiberationSans-Bold.ttf"),
        _ => ReadEmbeddedFont("LiberationSans-Regular.ttf"),
    };

    private static byte[] ReadEmbeddedFont(string fileName)
    {
        var assembly = typeof(EmbeddedFontResolver).Assembly;
        var resourceName = $"AssetManagement.Application.ImportExport.Fonts.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso de fuente incrustado no encontrado: {resourceName}");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
