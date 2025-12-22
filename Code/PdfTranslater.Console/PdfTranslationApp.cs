using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        var finalOutputPath = ResolveFinalOutputPath(options);
        await _writer.WriteAsync(finalOutputPath, output, options.ForceOverwrite, cancellationToken);
        CopySourcePdf(options.InputPath, finalOutputPath);
    }

    private static string BuildOutput(IEnumerable<(PdfPageText page, string translation)> pages, string extension)
    {
        var builder = new StringBuilder();
        var isMarkdown = string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase);

        foreach (var (page, translation) in pages)
        {
            AppendPageHeader(builder, page.PageNumber, isMarkdown);
            AppendBilingualBlock(builder, page.Text, translation);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendPageHeader(StringBuilder builder, int pageNumber, bool isMarkdown)
    {
        if (isMarkdown)
        {
            builder.AppendLine($"# Page {pageNumber}");
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine($"Page {pageNumber}");
            builder.AppendLine(new string('-', 24));
        }
    }

    private static void AppendBilingualBlock(StringBuilder builder, string original, string translation)
    {
        builder.AppendLine("Sentence-level bilingual block (English / Chinese)");
        builder.AppendLine();
        foreach (var pair in GetSentencePairs(original, translation))
        {
            if (!string.IsNullOrWhiteSpace(pair.Source))
            {
                builder.AppendLine(pair.Source.Trim());
            }

            if (!string.IsNullOrWhiteSpace(pair.Translation))
            {
                builder.AppendLine(pair.Translation.Trim());
            }

            builder.AppendLine();
        }
    }

    private static IReadOnlyList<SentencePair> GetSentencePairs(string original, string translation)
    {
        if (TryParseSentencePairs(translation, out var pairs))
        {
            return pairs;
        }

        return new List<SentencePair>
        {
            new(original.Trim(), translation.Trim())
        };
    }

    private static bool TryParseSentencePairs(string translation, out List<SentencePair> pairs)
    {
        pairs = new List<SentencePair>();
        var trimmed = translation.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        try
        {
            pairs = JsonSerializer.Deserialize<List<SentencePair>>(trimmed, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SentencePair>();
            return pairs.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ResolveFinalOutputPath(AppOptions options)
    {
        var workspaceRoot = LocateSolutionRoot();
        var outputDirectory = Path.Combine(workspaceRoot, "OutputDicuments");
        var sourceFolder = Path.GetFileNameWithoutExtension(options.InputPath) switch
        {
            { Length: > 0 } value => value,
            _ => "translation"
        };

        var targetDirectory = Path.Combine(outputDirectory, sourceFolder);
        Directory.CreateDirectory(targetDirectory);
        var outputFileName = Path.GetFileName(options.OutputPath);
        return Path.Combine(targetDirectory, string.IsNullOrWhiteSpace(outputFileName) ? "translated.md" : outputFileName);
    }

    private static void CopySourcePdf(string sourcePath, string finalOutputPath)
    {
        var targetDirectory = Path.GetDirectoryName(finalOutputPath);
        if (targetDirectory is null)
        {
            return;
        }

        var destination = Path.Combine(targetDirectory, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destination, overwrite: true);
        Console.WriteLine($"[INFO] Copied source PDF to {destination}");
    }

    private static string LocateSolutionRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PdfTranslater.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private sealed record SentencePair([property: JsonPropertyName("source")] string Source, [property: JsonPropertyName("translation")] string Translation);
}
