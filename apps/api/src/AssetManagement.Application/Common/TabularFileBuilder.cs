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
    private const double LandscapeUsableWidth = 782; // A4 landscape (842pt wide) minus the same margins.

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
    /// <param name="landscape">A4 landscape instead of the default portrait — more usable width for
    /// reports with many columns.</param>
    /// <param name="wrapText">Wraps a cell's full text across multiple lines (growing the row's height
    /// as needed) instead of truncating it with an ellipsis — for reports where every field must show
    /// in full, never cut off.</param>
    public static byte[] BuildPdf(
        string title, IReadOnlyList<string> headers, IEnumerable<string[]> rows, double[]? columnWidths = null,
        bool landscape = false, bool wrapText = false)
    {
        EmbeddedFontResolver.EnsureRegistered();

        var pageUsableWidth = landscape ? LandscapeUsableWidth : PageUsableWidth;
        var widths = columnWidths ?? Enumerable.Repeat(pageUsableWidth / headers.Count, headers.Count).ToArray();

        var headerFont = new XFont(EmbeddedFontResolver.FamilyName, 9, XFontStyleEx.Bold);
        var rowFont = new XFont(EmbeddedFontResolver.FamilyName, 8, XFontStyleEx.Regular);
        var titleFont = new XFont(EmbeddedFontResolver.FamilyName, 14, XFontStyleEx.Bold);

        var document = new PdfDocument();
        PdfPage? page = null;
        XGraphics? gfx = null;
        double y = 0;

        void StartPage()
        {
            page = document.AddPage();
            if (landscape)
            {
                page.Orientation = PdfSharp.PageOrientation.Landscape;
            }

            gfx = XGraphics.FromPdfPage(page);
            y = MarginTop;
            gfx.DrawString(title, titleFont, XBrushes.Black, new XPoint(MarginLeft, y));
            y += RowHeight * 1.5;
            y = wrapText
                ? DrawWrappedRow(gfx!, WrapRow(gfx!, headers, headerFont, widths), headerFont, y, widths)
                : DrawTruncatedRow(gfx!, headers, headerFont, y, widths);
        }

        StartPage();

        foreach (var row in rows)
        {
            if (wrapText)
            {
                var wrapped = WrapRow(gfx!, row, rowFont, widths);
                var neededHeight = RowHeight * Math.Max(1, wrapped.Count == 0 ? 1 : wrapped.Max(c => c.Count));
                if (y + neededHeight > page!.Height.Point - MarginTop)
                {
                    StartPage();
                    wrapped = WrapRow(gfx!, row, rowFont, widths);
                }

                y = DrawWrappedRow(gfx!, wrapped, rowFont, y, widths);
            }
            else
            {
                if (y + RowHeight > page!.Height.Point - MarginTop)
                {
                    StartPage();
                }

                y = DrawTruncatedRow(gfx!, row, rowFont, y, widths);
            }
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static double DrawTruncatedRow(XGraphics gfx, IReadOnlyList<string> values, XFont font, double y, double[] columnWidths)
    {
        var x = MarginLeft;
        for (var col = 0; col < values.Count && col < columnWidths.Length; col++)
        {
            gfx.DrawString(Truncate(values[col], columnWidths[col]), font, XBrushes.Black, new XPoint(x, y));
            x += columnWidths[col];
        }

        return y + RowHeight;
    }

    private static string Truncate(string value, double columnWidth)
    {
        var maxChars = (int)(columnWidth / 4.5);
        return value.Length > maxChars ? string.Concat(value.AsSpan(0, Math.Max(0, maxChars - 1)), "…") : value;
    }

    private static List<List<string>> WrapRow(XGraphics gfx, IReadOnlyList<string> values, XFont font, double[] columnWidths)
    {
        var result = new List<List<string>>();
        for (var col = 0; col < values.Count && col < columnWidths.Length; col++)
        {
            result.Add(WrapText(gfx, values[col], font, columnWidths[col]));
        }

        return result;
    }

    private static double DrawWrappedRow(
        XGraphics gfx, List<List<string>> wrappedColumns, XFont font, double y, double[] columnWidths)
    {
        var maxLines = wrappedColumns.Count == 0 ? 1 : wrappedColumns.Max(c => c.Count);
        var x = MarginLeft;
        for (var col = 0; col < wrappedColumns.Count; col++)
        {
            var lineY = y;
            foreach (var line in wrappedColumns[col])
            {
                gfx.DrawString(line, font, XBrushes.Black, new XPoint(x, lineY));
                lineY += RowHeight;
            }

            x += columnWidths[col];
        }

        return y + RowHeight * maxLines;
    }

    /// <summary>Word-wraps <paramref name="text"/> to fit <paramref name="maxWidth"/>, measuring with the
    /// real font metrics rather than a character-count heuristic. A single "word" wider than the column
    /// (e.g. a long code with no spaces) is hard-split by character instead of overflowing the column.</summary>
    private static List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [""];
        }

        var lines = new List<string>();
        var current = "";

        foreach (var rawWord in text.Split(' '))
        {
            var word = rawWord;
            while (gfx.MeasureString(word, font).Width > maxWidth)
            {
                var splitAt = word.Length;
                while (splitAt > 1 && gfx.MeasureString(word[..splitAt], font).Width > maxWidth)
                {
                    splitAt--;
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                    current = "";
                }

                lines.Add(word[..splitAt]);
                word = word[splitAt..];
            }

            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (current.Length == 0 || gfx.MeasureString(candidate, font).Width <= maxWidth)
            {
                current = candidate;
            }
            else
            {
                lines.Add(current);
                current = word;
            }
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        return lines.Count == 0 ? [""] : lines;
    }
}
