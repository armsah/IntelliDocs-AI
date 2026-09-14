using System.Text.Json;
using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Infrastructure.DocumentIntelligence;
using Microsoft.Extensions.Options;

if (args.Length is < 3 or > 4)
{
    Console.Error.WriteLine(
        "Usage: <invoice|layout|query> <input-file> <output-json> [comma-separated-query-fields]");
    return 1;
}

var mode = args[0].ToLowerInvariant();

var model = mode switch
{
    "invoice" => DocumentAnalysisModel.Invoice,
    "layout" => DocumentAnalysisModel.Layout,
    "query" => DocumentAnalysisModel.Layout,
    _ => throw new ArgumentException(
        "Model must be 'invoice', 'layout', or 'query'.")
};

IReadOnlyList<string>? queryFields = null;

if (mode == "query")
{
    if (args.Length != 4 ||
        string.IsNullOrWhiteSpace(args[3]))
    {
        Console.Error.WriteLine(
            "Query mode requires comma-separated query fields.");
        return 1;
    }

    queryFields =
        args[3]
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    if (queryFields.Count == 0)
    {
        Console.Error.WriteLine(
            "At least one query field is required.");
        return 1;
    }
}
else if (args.Length != 3)
{
    Console.Error.WriteLine(
        "Invoice and layout modes do not accept query fields.");
    return 1;
}

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
        model,
        queryFields);

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

if (queryFields is not null)
{
    Console.WriteLine(
        $"Query fields: {string.Join(", ", queryFields)}");
}

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
