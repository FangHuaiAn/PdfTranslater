using System.Text;
using PdfTranslater.ConsoleApp.Services;

namespace PdfTranslater.ConsoleApp;

internal sealed class PdfTranslationApp
{
    private readonly PdfTextExtractor _extractor;
    private readonly BatchTranslator _translator;
    private readonly TranslationResultWriter _writer;

    public PdfTranslationApp(
        PdfTextExtractor extractor,
        BatchTranslator translator,
        TranslationResultWriter writer)
    {
        _extractor = extractor;
        _translator = translator;
        _writer = writer;
    }

    public async Task RunAsync(AppOptions options, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[INFO] Loading PDF from {options.InputPath}...");
        var pages = await _extractor.ExtractAsync(options.InputPath, cancellationToken); // extract per-page text before translation

        if (pages.Count == 0)
        {
            Console.WriteLine("[WARN] PDF contains no extractable text.");
        }

        var translatedPages = new List<(PdfPageText page, string translation)>();
        foreach (var page in pages) // translate pages sequentially to preserve ordering
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"[INFO] Translating page {page.PageNumber}...");
            var translated = await _translator.TranslateAsync(page.Text, options.SourceLanguage, options.TargetLanguage, cancellationToken);
            translatedPages.Add((page, translated));
        }

        Console.WriteLine("[INFO] Writing output file...");
        var output = BuildOutput(translatedPages, Path.GetExtension(options.OutputPath)); // format result per page header
        await _writer.WriteAsync(options.OutputPath, output, options.ForceOverwrite, cancellationToken);
    }

    private static string BuildOutput(IEnumerable<(PdfPageText page, string translation)> pages, string extension)
    {
        var builder = new StringBuilder();
        var isMarkdown = string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase); // markdown gets headings for each page

        foreach (var (page, translation) in pages)
        {
            if (isMarkdown)
            {
                builder.AppendLine($"# Page {page.PageNumber}");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine($"Page {page.PageNumber}");
                builder.AppendLine(new string('-', 24));
            }

            builder.AppendLine(string.IsNullOrWhiteSpace(translation)
                ? "(No translated content)"
                : translation.Trim());
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
