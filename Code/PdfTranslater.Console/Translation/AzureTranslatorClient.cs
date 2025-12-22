using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PdfTranslater.ConsoleApp.Translation;

internal sealed class AzureTranslatorClient : ITranslationProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _key;
    private readonly string _region;

    public AzureTranslatorClient(EnvironmentConfig config)
    {
        if (!config.HasAzureCredentials)
        {
            throw new InvalidOperationException("Azure credentials are missing. Cannot initialize AzureTranslatorClient.");
        }

        _endpoint = config.TranslatorEndpoint!.TrimEnd('/');
        _key = config.TranslatorKey!;
        _region = config.TranslatorRegion!;
        _httpClient = new HttpClient();
    }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        var requestUri = $"{_endpoint}/translate?api-version=3.0&from={Uri.EscapeDataString(sourceLanguage)}&to={Uri.EscapeDataString(targetLanguage)}";
        var payload = JsonSerializer.Serialize(new[] { new { Text = text } });
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Ocp-Apim-Subscription-Key", _key);
        request.Headers.Add("Ocp-Apim-Subscription-Region", _region);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // request JSON response

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure Translator error {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var builder = new StringBuilder(); // collect translated fragments from Azure response
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (!element.TryGetProperty("translations", out var translations))
            {
                continue;
            }

            foreach (var translation in translations.EnumerateArray())
            {
                if (translation.TryGetProperty("text", out var textElement))
                {
                    builder.AppendLine(textElement.GetString());
                }
            }
        }

        return builder.ToString();
    }
}
