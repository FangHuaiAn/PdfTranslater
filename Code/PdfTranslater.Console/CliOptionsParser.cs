using System.Text;

namespace PdfTranslater.ConsoleApp;

internal static class CliOptionsParser
{
    public const string Usage = "Usage: dotnet run -- --input <path> --output <path> --from <sourceLocale> --to <targetLocale> [--force]";

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
            throw new CliUsageException("Missing required option --input.");
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
}

internal sealed class CliUsageException : Exception
{
    public CliUsageException(string message) : base(message)
    {
    }
}
