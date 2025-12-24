namespace PdfTranslater.Core;

public sealed record AppOptions(
    string InputPath,
    string OutputPath,
    string SourceLanguage,
    string TargetLanguage,
    bool ForceOverwrite);
