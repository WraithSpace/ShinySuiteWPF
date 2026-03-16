using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShinySuite.Services;

public partial class DetectionService
{
    private readonly string _tessPath;

    // Single temp file reused each OCR call (detection loop is single-threaded)
    private static readonly string TempPng =
        Path.Combine(Path.GetTempPath(), "shinysuite_ocr.png");

    public DetectionService()
    {
        _tessPath = FindTesseract();
    }

    // ── Screen capture ─────────────────────────────────────────────────────────

    public static Bitmap CaptureRegion(int x, int y, int width, int height)
    {
        var bmp = new Bitmap(Math.Max(1, width), Math.Max(1, height), PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }

    // ── OCR via Tesseract CLI ──────────────────────────────────────────────────

    public async Task<string> OcrAsync(Bitmap bmp)
    {
        try
        {
            using var processed = Preprocess(bmp);
            processed.Save(TempPng, ImageFormat.Png);

            var psi = new ProcessStartInfo(_tessPath,
                $"\"{TempPng}\" stdout --psm 6 --oem 3")
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
            };
            using var proc = Process.Start(psi)!;
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return CleanOcrText(output);
        }
        catch (Exception ex) { return $"[{ex.GetType().Name}]"; }
    }

    // 3× nearest-neighbor upscale then grayscale — same as the Python version
    private static Bitmap Preprocess(Bitmap src)
    {
        const int Scale = 3;

        // Step 1: upscale with INTER_NEAREST to keep crisp pixel-art edges
        var scaled = new Bitmap(src.Width * Scale, src.Height * Scale);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode   = PixelOffsetMode.Half;
            g.DrawImage(src, 0, 0, scaled.Width, scaled.Height);
        }

        // Step 2: convert to grayscale via ColorMatrix
        var gray = new Bitmap(scaled.Width, scaled.Height);
        using (var g = Graphics.FromImage(gray))
        {
            var cm = new ColorMatrix(new[]
            {
                new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                new float[] { 0,      0,      0,      1, 0 },
                new float[] { 0,      0,      0,      0, 1 },
            });
            using var ia = new ImageAttributes();
            ia.SetColorMatrix(cm);
            g.DrawImage(scaled,
                new Rectangle(0, 0, scaled.Width, scaled.Height),
                0, 0, scaled.Width, scaled.Height,
                GraphicsUnit.Pixel, ia);
        }
        scaled.Dispose();
        return gray;
    }

    private static string CleanOcrText(string raw)
        => Regex.Replace(raw.ToUpperInvariant(), @"[^A-Z0-9\s'\-]", " ").Trim();

    // ── Pokémon matching ───────────────────────────────────────────────────────

    /// <summary>Returns the showdown ID whose display name best matches ocrText, or null.</summary>
    public static string? MatchPokemon(string ocrText,
        IEnumerable<(string sid, string displayName)> candidates)
    {
        if (string.IsNullOrWhiteSpace(ocrText)) return null;
        var upper = ocrText.ToUpperInvariant();
        string? bestSid = null;
        double bestScore = 0.55;

        foreach (var (sid, display) in candidates)
        {
            var name = display.ToUpperInvariant();
            double score = upper.Contains(name) ? 1.0 : FuzzyRatio(upper, name);
            if (score > bestScore) { bestScore = score; bestSid = sid; }
        }
        return bestSid;
    }

    private static double FuzzyRatio(string a, string b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        if (maxLen == 0) return 1.0;
        return 1.0 - (double)Levenshtein(a, b) / maxLen;
    }

    private static int Levenshtein(string a, string b)
    {
        int m = a.Length, n = b.Length;
        var dp = new int[m + 1, n + 1];
        for (int i = 0; i <= m; i++) dp[i, 0] = i;
        for (int j = 0; j <= n; j++) dp[0, j] = j;
        for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
        return dp[m, n];
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string FindTesseract()
    {
        var pf   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string[] candidates =
        [
            Path.Combine(pf,   "Tesseract-OCR", "tesseract.exe"),
            Path.Combine(pf86, "Tesseract-OCR", "tesseract.exe"),
        ];
        return Array.Find(candidates, File.Exists) ?? "tesseract";
    }
}
