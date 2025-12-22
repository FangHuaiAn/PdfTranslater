namespace PdfTranslater.ConsoleApp;

internal sealed record EnvironmentConfig(
    string? TranslatorKey,
    string? TranslatorRegion,
    string? TranslatorEndpoint)
{
    public bool HasAzureCredentials =>
        !string.IsNullOrWhiteSpace(TranslatorKey) &&
        !string.IsNullOrWhiteSpace(TranslatorRegion) &&
        !string.IsNullOrWhiteSpace(TranslatorEndpoint);

    public static EnvironmentConfig Load()
    {
        return new EnvironmentConfig(
            TranslatorKey: Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY"),
            TranslatorRegion: Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION"),
            TranslatorEndpoint: Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_ENDPOINT"));
    }
}
