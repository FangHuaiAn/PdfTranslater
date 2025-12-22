using UglyToad.PdfPig;

namespace PdfTranslater.ConsoleApp.Services;

internal sealed class PdfTextExtractor
{
    public Task<IReadOnlyList<PdfPageText>> ExtractAsync(string pdfPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException($"Input file not found: {pdfPath}"); // guard to surface missing PDFs before opening
        }

        var pages = new List<PdfPageText>();
        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages()) // read each page so translation can preserve numbering
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = page.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($"[WARN] Page {page.Number} contains no text content.");
            }

            pages.Add(new PdfPageText(page.Number, text));
        }

        return Task.FromResult<IReadOnlyList<PdfPageText>>(pages);
    }
}
