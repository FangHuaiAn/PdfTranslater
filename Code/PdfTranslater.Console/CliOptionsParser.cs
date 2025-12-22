using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PdfTranslater.ConsoleApp;

internal static class CliOptionsParser
{
    public const string Usage = "Usage: dotnet run -- --output <path> --from <sourceLocale> --to <targetLocale> [--input <path>] [--force]";

    public static AppOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new CliUsageException("No arguments provided.");
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var force = false;

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                throw new CliUsageException($"Unexpected token '{current}'.");
            }

            var key = current[2..];
            if (string.Equals(key, "force", StringComparison.OrdinalIgnoreCase))
            {
                force = true;
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new CliUsageException($"Missing value for option '{key}'.");
            }

            var value = args[++index];
            normalized[key] = value;
        }

        if (!normalized.TryGetValue("input", out var inputPath))
        {
            inputPath = ResolveDefaultTestPdf();
            if (inputPath is null)
            {
                throw new CliUsageException("Missing required option --input and no default test PDF was discovered.");
            }
        }

        if (!normalized.TryGetValue("output", out var outputPath))
        {
            throw new CliUsageException("Missing required option --output.");
        }

        if (!normalized.TryGetValue("from", out var sourceLanguage))
        {
            throw new CliUsageException("Missing required option --from.");
        }

        if (!normalized.TryGetValue("to", out var targetLanguage))
        {
            throw new CliUsageException("Missing required option --to.");
        }

        return new AppOptions(
            Path.GetFullPath(inputPath),
            Path.GetFullPath(outputPath),
            sourceLanguage,
            targetLanguage,
            force);
    }

    private static string? ResolveDefaultTestPdf()
    {
        var configPath = LocateConfigFile("config", "test-resources.json");
        if (configPath is null)
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(configPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("testPdfDirectory", out var element))
            {
                return null;
            }

            var relativeFolder = element.GetString();
            if (string.IsNullOrWhiteSpace(relativeFolder))
            {
                return null;
            }

            var baseDir = Path.GetDirectoryName(configPath)!;
            var pdfFolder = Path.GetFullPath(Path.Combine(baseDir, relativeFolder));
            if (!Directory.Exists(pdfFolder))
            {
                var rootCandidate = Path.GetFullPath(Path.Combine(baseDir, "..", relativeFolder));
                if (!Directory.Exists(rootCandidate))
                {
                    return null;
                }

                pdfFolder = rootCandidate;
            }

            var candidate = Directory.EnumerateFiles(pdfFolder, "*.pdf")
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return candidate;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? LocateConfigFile(params string[] segments)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, Path.Combine(segments));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

internal sealed class CliUsageException : Exception
{
    public CliUsageException(string message) : base(message)
    {
    }
}
