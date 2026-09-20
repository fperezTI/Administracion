using AssetManagement.Application.ImportExport;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AssetManagement.Application.Common;

/// <summary>
/// Builds a simple tabular <c>.xlsx</c>/<c>.pdf</c> file from headers + rows — extracted from F9's
/// <c>ExportAssetsQueryHandler</c> (the first place this app generated a spreadsheet/PDF) so F10's report
/// exports reuse it instead of re-implementing the same drawing/pagination logic (see the F10 plan,
/// decision 3). No behavior change versus the original F9 code.
/// </summary>
public static class TabularFileBuilder
{
    private const double MarginLeft = 30;
    private const double MarginTop = 40;
    private const double RowHeight = 18;
    private const double PageUsableWidth = 535; // A4 portrait minus margins, matches F9's original layout.

    public static byte[] BuildXlsx(string sheetName, IReadOnlyList<string> headers, IEnumerable<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var col = 0; col < headers.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Length; col++)
            {
                worksheet.Cell(rowIndex, col + 1).Value = row[col];
            }

            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <param name="columnWidths">In PDF points; must total no more than the usable page width. Defaults
    /// to equal-width columns across the page when omitted.</param>
    public static byte[] BuildPdf(string title, IReadOnlyList<string> headers, IEnumerable<string[]> rows, double[]? columnWidths = null)
    {
        EmbeddedFontResolver.EnsureRegistered();

        var widths = columnWidths ?? Enumerable.Repeat(PageUsableWidth / headers.Count, headers.Count).ToArray();

        var headerFont = new XFont(EmbeddedFontResolver.FamilyName, 9, XFontStyleEx.Bold);
        var rowFont = new XFont(EmbeddedFontResolver.FamilyName, 8, XFontStyleEx.Regular);
        var titleFont = new XFont(EmbeddedFontResolver.FamilyName, 14, XFontStyleEx.Bold);

        var document = new PdfDocument();
        PdfPage? page = null;
        XGraphics? gfx = null;
        double y = 0;

        void StartPage()
        {
            page = document.AddPage(); // Defaults to A4 portrait.
            gfx = XGraphics.FromPdfPage(page);
            y = MarginTop;
            gfx.DrawString(title, titleFont, XBrushes.Black, new XPoint(MarginLeft, y));
            y += RowHeight * 1.5;
            DrawRow(gfx!, headers, headerFont, y, widths);
            y += RowHeight;
        }

        StartPage();

        foreach (var row in rows)
        {
            if (y + RowHeight > page!.Height.Point - MarginTop)
            {
                StartPage();
            }

            DrawRow(gfx!, row, rowFont, y, widths);
            y += RowHeight;
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static void DrawRow(XGraphics gfx, IReadOnlyList<string> values, XFont font, double y, double[] columnWidths)
    {
        var x = MarginLeft;
        for (var col = 0; col < values.Count && col < columnWidths.Length; col++)
        {
            gfx.DrawString(Truncate(values[col], columnWidths[col]), font, XBrushes.Black, new XPoint(x, y));
            x += columnWidths[col];
        }
    }

    private static string Truncate(string value, double columnWidth)
    {
        var maxChars = (int)(columnWidth / 4.5);
        return value.Length > maxChars ? string.Concat(value.AsSpan(0, Math.Max(0, maxChars - 1)), "…") : value;
    }
}
