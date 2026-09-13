using System.Text.Json;
using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Infrastructure.DocumentIntelligence;
using Microsoft.Extensions.Options;

if (args.Length != 3)
{
    Console.Error.WriteLine(
        "Usage: <invoice|layout> <input-file> <output-json>");
    return 1;
}

var model = args[0].ToLowerInvariant() switch
{
    "invoice" => DocumentAnalysisModel.Invoice,
    "layout" => DocumentAnalysisModel.Layout,
    _ => throw new ArgumentException(
        "Model must be 'invoice' or 'layout'.")
};

var inputPath = Path.GetFullPath(args[1]);
var outputPath = Path.GetFullPath(args[2]);

var endpoint =
    Environment.GetEnvironmentVariable(
        "DocumentIntelligence__Endpoint");

var apiKey =
    Environment.GetEnvironmentVariable(
        "DocumentIntelligence__ApiKey");

if (string.IsNullOrWhiteSpace(endpoint) ||
    string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine(
        "DocumentIntelligence__Endpoint and " +
        "DocumentIntelligence__ApiKey must be configured.");
    return 2;
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine(
        $"Input file not found: {inputPath}");
    return 3;
}

var options =
    Options.Create(
        new DocumentIntelligenceOptions
        {
            Endpoint = endpoint,
            ApiKey = apiKey
        });

IDocumentIntelligenceProvider provider =
    new AzureDocumentIntelligenceProvider(options);

var content = await File.ReadAllBytesAsync(inputPath);

var request =
    new DocumentAnalysisRequest(
        Path.GetFileName(inputPath),
        GetContentType(inputPath),
        content,
        model);

var result =
    await provider.AnalyzeAsync(request);

Directory.CreateDirectory(
    Path.GetDirectoryName(outputPath)
    ?? Directory.GetCurrentDirectory());

await File.WriteAllTextAsync(
    outputPath,
    JsonSerializer.Serialize(
        result,
        new JsonSerializerOptions
        {
            WriteIndented = true
        }));

Console.WriteLine(
    $"Model: {result.ModelId}");
Console.WriteLine(
    $"Pages: {result.Pages.Count}");
Console.WriteLine(
    $"Fields: {result.Fields.Count}");
Console.WriteLine(
    $"Tables: {result.Tables.Count}");
Console.WriteLine(
    $"Evidence: {outputPath}");

return 0;

static string GetContentType(string path)
{
    return Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".tif" or ".tiff" => "image/tiff",
        _ => "application/octet-stream"
    };
}
