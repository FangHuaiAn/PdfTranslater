namespace PdfTranslater.ConsoleApp.Translation;

internal sealed class MockTranslationProvider : ITranslationProvider
{
    public Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        // this provider allows the CLI to stay runnable when Azure keys are unset
        Console.WriteLine("[WARN] Azure credentials missing. Returning original text as mock translation.");
        return Task.FromResult(text);
    }
}
