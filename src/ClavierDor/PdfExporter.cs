using System;
using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace ClavierDor;

public static class PdfExporter
{
    public static string ExportScores(IEnumerable<(string Name, int Score, DateTime StartedAt)> scores)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
        var font = new XFont("Arial", 12, XFontStyle.Regular);

        double y = 40;
        gfx.DrawString("Classement - Clavier d'Or", titleFont, XBrushes.Black, 40, y);
        y += 24;
        gfx.DrawString($"Exporté le {DateTime.Now:dd/MM/yyyy HH:mm}", font, XBrushes.Black, 40, y);
        y += 24;

        gfx.DrawString("Joueur", font, XBrushes.Black, 40, y);
        gfx.DrawString("Score", font, XBrushes.Black, 220, y);
        gfx.DrawString("Date", font, XBrushes.Black, 300, y);
        y += 18;

        foreach (var (name, score, startedAt) in scores)
        {
            gfx.DrawString(name, font, XBrushes.Black, 40, y);
            gfx.DrawString(score.ToString(), font, XBrushes.Black, 220, y);
            gfx.DrawString(startedAt.ToString("dd/MM/yyyy"), font, XBrushes.Black, 300, y);
            y += 18;
        }

        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".clavierdor");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "classement.pdf");
        document.Save(outputPath);
        return outputPath;
    }
}
