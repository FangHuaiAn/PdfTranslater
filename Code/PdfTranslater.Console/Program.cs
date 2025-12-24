using PdfTranslater.ConsoleApp;
using PdfTranslater.Core;
using PdfTranslater.Core.Services;
using PdfTranslater.Core.Translation;

return await ProgramEntrypoint.RunAsync(args);

internal static class ProgramEntrypoint
{
    public static async Task<int> RunAsync(string[] args)
    {
        using var cancellationSource = new CancellationTokenSource(); // enables Ctrl+C to stop gracefully
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            Console.WriteLine("[WARN] Cancellation requested. Attempting graceful shutdown...");
            cancellationSource.Cancel();
            eventArgs.Cancel = true;
        };

        try
        {
            var options = CliOptionsParser.Parse(args);
            var environment = EnvironmentConfig.Load(); // load env vars to decide translation provider

            ITranslationProvider translationProvider;
            if (environment.HasOpenAiKey)
            {
                translationProvider = new OpenAITranslationProvider(environment);
            }
            else if (environment.HasAzureCredentials)
            {
                translationProvider = new AzureTranslatorClient(environment);
            }
            else
            {
                translationProvider = new MockTranslationProvider(); // fallback to mock when credentials are absent so CLI still runs
            }

            var extractor = new PdfTextExtractor();
            var translator = new BatchTranslator(translationProvider);
            var writer = new TranslationResultWriter();
            var app = new PdfTranslationApp(extractor, translator, writer);

            await app.RunAsync(options, cancellationSource.Token);
            Console.WriteLine("[INFO] Translation completed successfully.");
            return 0;
        }
        catch (CliUsageException usageError)
        {
            Console.Error.WriteLine($"[ERROR] {usageError.Message}");
            Console.Error.WriteLine(CliOptionsParser.Usage);
            return 2;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("[ERROR] Operation canceled by user.");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Unexpected failure: {ex.Message}");
            return 1;
        }
    }
}
