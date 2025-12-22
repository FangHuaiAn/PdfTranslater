namespace PdfTranslater.ConsoleApp;

internal sealed record AppOptions(
    string InputPath,
    string OutputPath,
    string SourceLanguage,
    string TargetLanguage,
    bool ForceOverwrite);
