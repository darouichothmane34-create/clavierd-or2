using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace ClavierDor;

public static class PdfExporter
{
    public static string ExportScores(string path, List<(string Name, int Score, DateTime StartedAt)> scores)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var headerFont = new XFont("Helvetica", 16, XFontStyle.Bold);
        var font = new XFont("Helvetica", 12, XFontStyle.Regular);

        var y = 40;
        gfx.DrawString("Classement - Clavier d'Or", headerFont, XBrushes.Black, 40, y);
        y += 24;
        gfx.DrawString($"Exporté le {DateTime.Now:dd/MM/yyyy HH:mm}", font, XBrushes.Black, 40, y);
        y += 30;

        gfx.DrawString("Joueur", font, XBrushes.Black, 40, y);
        gfx.DrawString("Score", font, XBrushes.Black, 260, y);
        gfx.DrawString("Date", font, XBrushes.Black, 340, y);
        y += 16;

        foreach (var (name, score, startedAt) in scores)
        {
            gfx.DrawString(name, font, XBrushes.Black, 40, y);
            gfx.DrawString(score.ToString(), font, XBrushes.Black, 260, y);
            gfx.DrawString(startedAt.ToString("dd/MM/yyyy"), font, XBrushes.Black, 340, y);
            y += 16;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        document.Save(path);
        return path;
    }
}
