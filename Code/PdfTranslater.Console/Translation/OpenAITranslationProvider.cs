using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PdfTranslater.ConsoleApp.Translation;

internal sealed class OpenAITranslationProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _model;

    public OpenAITranslationProvider(EnvironmentConfig config)
    {
        if (!config.HasOpenAiKey)
        {
            throw new InvalidOperationException("OpenAI API key is missing. Cannot initialize OpenAITranslationProvider.");
        }

        _endpoint = config.OpenAiResponsesUrl;
        _model = config.OpenAiModel;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.OpenAiApiKey!);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = _model,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = BuildTranslationPrompt(text, sourceLanguage, targetLanguage)
                        }
                    }
                }
            },
            temperature = 0.2,
            max_output_tokens = 4000
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI Translation error {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        return ExtractTranslation(document);
    }

    private static string BuildTranslationPrompt(string text, string sourceLanguage, string targetLanguage)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Translate the following text from {sourceLanguage} to {targetLanguage}, preserving paragraph structure and sentence boundaries.");
        builder.AppendLine("Return only a JSON array of objects, each with \"source\" (the original sentence) and \"translation\" (the Chinese sentence). Do not add any extra explanation or markdown.");
        builder.AppendLine();
        builder.AppendLine(text);
        return builder.ToString();
    }

    private static string ExtractTranslation(JsonDocument document)
    {
        var builder = new StringBuilder();

        if (document.RootElement.TryGetProperty("output", out var outputs))
        {
            foreach (var output in outputs.EnumerateArray())
            {
                if (!output.TryGetProperty("content", out var contents))
                {
                    continue;
                }

                foreach (var content in contents.EnumerateArray())
                {
                    if (!content.TryGetProperty("type", out var typeElement) ||
                        typeElement.GetString() != "output_text")
                    {
                        continue;
                    }

                    if (content.TryGetProperty("text", out var textElement) &&
                        textElement.GetString() is { } textValue)
                    {
                        builder.AppendLine(textValue);
                    }
                }
            }
        }

        if (builder.Length == 0 && document.RootElement.TryGetProperty("choices", out var choices))
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (!choice.TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("content", out var content))
                {
                    continue;
                }

                switch (content.ValueKind)
                {
                    case JsonValueKind.String when content.GetString() is { } single:
                        builder.AppendLine(single);
                        break;
                    case JsonValueKind.Array:
                        foreach (var entry in content.EnumerateArray())
                        {
                            if (entry.TryGetProperty("text", out var entryText) &&
                                entryText.GetString() is { } entryValue)
                            {
                                builder.AppendLine(entryValue);
                            }
                        }
                        break;
                }
            }
        }

        return builder.ToString().Trim();
    }
}
