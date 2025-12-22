namespace PdfTranslater.ConsoleApp.Services;

internal sealed class TranslationResultWriter
{
    public async Task WriteAsync(string outputPath, string content, bool forceOverwrite, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory); // ensure target folder exists before writing
        }

        if (File.Exists(outputPath) && !forceOverwrite)
        {
            throw new IOException($"Output file already exists. Use --force to overwrite: {outputPath}"); // prevent unintended overwrites
        }

        await File.WriteAllTextAsync(outputPath, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
    }
}
