using System.Text;
using PdfTranslater.ConsoleApp.Translation;

namespace PdfTranslater.ConsoleApp.Services;

internal sealed class BatchTranslator
{
    private const int MaxCharactersPerRequest = 4500; // keep Azure requests under quota per call
    private readonly ITranslationProvider _provider;

    public BatchTranslator(ITranslationProvider provider)
    {
        _provider = provider;
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var output = new StringBuilder();
        foreach (var chunk in ChunkText(text)) // chunk text so each API call stays within limits
        {
            cancellationToken.ThrowIfCancellationRequested();
            var translated = await _provider.TranslateAsync(chunk, sourceLanguage, targetLanguage, cancellationToken);
            output.AppendLine(translated.Trim());
        }

        return output.ToString();
    }

    private static IEnumerable<string> ChunkText(string text) // preserve paragraphs while capping size
    {
        // Split on double new lines to keep paragraphs together whenever possible.
        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            var candidate = paragraph.Trim();
            if (candidate.Length == 0)
            {
                continue;
            }

            if (current.Length + candidate.Length + Environment.NewLine.Length > MaxCharactersPerRequest)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.AppendLine();
            }

            current.Append(candidate);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }
}
