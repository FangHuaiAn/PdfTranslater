namespace PdfTranslater.ConsoleApp;

internal sealed record EnvironmentConfig(
    string? TranslatorKey,
    string? TranslatorRegion,
    string? TranslatorEndpoint,
    string? OpenAiApiKey,
    string OpenAiBaseUrl,
    string OpenAiModel)
{
    public bool HasAzureCredentials =>
        !string.IsNullOrWhiteSpace(TranslatorKey) &&
        !string.IsNullOrWhiteSpace(TranslatorRegion) &&
        !string.IsNullOrWhiteSpace(TranslatorEndpoint);

    public bool HasOpenAiKey => !string.IsNullOrWhiteSpace(OpenAiApiKey);
    public string OpenAiResponsesUrl =>
        $"{OpenAiBaseUrl.TrimEnd('/')}/v1/responses";

    public static EnvironmentConfig Load()
    {
        var openAiBase = Environment.GetEnvironmentVariable("OPENAI_API_BASE");
        var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        return new EnvironmentConfig(
            TranslatorKey: Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY"),
            TranslatorRegion: Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION"),
            TranslatorEndpoint: Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_ENDPOINT"),
            OpenAiApiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            OpenAiBaseUrl: string.IsNullOrWhiteSpace(openAiBase)
                ? "https://api.openai.com"
                : openAiBase!.Trim(),
            OpenAiModel: string.IsNullOrWhiteSpace(openAiModel)
                ? "gpt-4o-mini"
                : openAiModel!.Trim());
    }
}
