using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;

const string classifierId = "intellidocs-p6-classifier-v1";
const string defaultInputRoot =
    "samples/synthetic/p6/classifier/holdout";
const string defaultEvidencePath =
    "docs/evidence/p6/classifier-holdout-results.json";

var inputRoot =
    args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0])
        ? args[0]
        : defaultInputRoot;

var evidencePath =
    args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1])
        ? args[1]
        : defaultEvidencePath;

var endpoint =
    Environment.GetEnvironmentVariable(
        "DocumentIntelligence__Endpoint");

var apiKey =
    Environment.GetEnvironmentVariable(
        "DocumentIntelligence__ApiKey");

if (string.IsNullOrWhiteSpace(endpoint))
{
    throw new InvalidOperationException(
        "DocumentIntelligence__Endpoint is required.");
}

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "DocumentIntelligence__ApiKey is required.");
}

if (!Directory.Exists(inputRoot))
{
    throw new DirectoryNotFoundException(inputRoot);
}

var client =
    new DocumentIntelligenceClient(
        new Uri(endpoint),
        new AzureKeyCredential(apiKey));

var files =
    Directory.GetFiles(
        inputRoot,
        "*.png",
        SearchOption.AllDirectories);

Array.Sort(files, StringComparer.Ordinal);

if (files.Length != 10)
{
    throw new InvalidOperationException(
        $"Expected 10 holdout PNGs but found {files.Length}.");
}

var results = new List<object>();

var correct = 0;

foreach (var file in files)
{
    var relativePath =
        Path.GetRelativePath(inputRoot, file);

    var expectedType =
        relativePath
            .Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            .First();

    var bytes =
        await File.ReadAllBytesAsync(file);

    var options =
        new ClassifyDocumentOptions(
            classifierId,
            BinaryData.FromBytes(bytes));

    var operation =
        await client.ClassifyDocumentAsync(
            WaitUntil.Completed,
            options);

    var result = operation.Value;

    var best =
        result.Documents
            .OrderByDescending(document => document.Confidence)
            .FirstOrDefault();

    var predictedType =
        best?.DocumentType;

    var confidence =
        best?.Confidence ?? 0;

    var isCorrect =
        string.Equals(
            expectedType,
            predictedType,
            StringComparison.Ordinal);

    if (isCorrect)
    {
        correct++;
    }

    Console.WriteLine(
        $"{relativePath}: expected={expectedType}, " +
        $"predicted={predictedType}, " +
        $"confidence={confidence:F4}, " +
        $"correct={isCorrect}");

    results.Add(
        new
        {
            file = relativePath.Replace('\\', '/'),
            expectedType,
            predictedType,
            confidence,
            correct = isCorrect
        });
}

var accuracy =
    files.Length == 0
        ? 0
        : (double)correct / files.Length;

var evidence = new
{
    classifierId,
    dataset = new
    {
        kind = "synthetic-holdout",
        documents = files.Length,
        documentsPerClass = 2,
        uploadedToTrainingContainer = false
    },
    summary = new
    {
        correct,
        total = files.Length,
        accuracy
    },
    results,
    note =
        "Synthetic held-out integration evidence; not representative production-quality accuracy."
};

var evidenceDirectory =
    Path.GetDirectoryName(evidencePath);

if (!string.IsNullOrWhiteSpace(evidenceDirectory))
{
    Directory.CreateDirectory(evidenceDirectory);
}

await File.WriteAllTextAsync(
    evidencePath,
    JsonSerializer.Serialize(
        evidence,
        new JsonSerializerOptions
        {
            WriteIndented = true
        }));

Console.WriteLine();
Console.WriteLine(
    $"Accuracy: {correct}/{files.Length} ({accuracy:P2})");

Console.WriteLine(
    $"Evidence written: {evidencePath}");