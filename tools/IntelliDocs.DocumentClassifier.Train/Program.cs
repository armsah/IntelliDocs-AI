using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;

const string defaultClassifierId = "intellidocs-p6-classifier-v1";
const string defaultEvidencePath =
    "docs/evidence/p6/classifier-build.json";

var classifierId =
    args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0])
        ? args[0]
        : defaultClassifierId;

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

var trainingContainerUri =
    Environment.GetEnvironmentVariable(
        "ClassifierTraining__ContainerSasUrl");

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

if (string.IsNullOrWhiteSpace(trainingContainerUri))
{
    throw new InvalidOperationException(
        "ClassifierTraining__ContainerSasUrl is required.");
}

var containerUri = new Uri(trainingContainerUri);

var documentTypes =
    new Dictionary<string, ClassifierDocumentTypeDetails>(
        StringComparer.Ordinal)
    {
        ["invoice"] =
            CreateDocumentType(
                containerUri,
                "invoice/"),

        ["purchase_order"] =
            CreateDocumentType(
                containerUri,
                "purchase_order/"),

        ["delivery_note"] =
            CreateDocumentType(
                containerUri,
                "delivery_note/"),

        ["contract"] =
            CreateDocumentType(
                containerUri,
                "contract/"),

        ["form"] =
            CreateDocumentType(
                containerUri,
                "form/")
    };

var client =
    new DocumentIntelligenceAdministrationClient(
        new Uri(endpoint),
        new AzureKeyCredential(apiKey));

var options =
    new BuildClassifierOptions(
        classifierId,
        documentTypes)
    {
        Description =
            "IntelliDocs AI P6 synthetic five-document-type classifier"
    };

Console.WriteLine(
    $"Building classifier '{classifierId}'...");

Console.WriteLine(
    $"Document types: {string.Join(", ", documentTypes.Keys)}");

var operation =
    await client.BuildClassifierAsync(
        WaitUntil.Completed,
        options);

var classifier = operation.Value;

Console.WriteLine(
    $"Classifier ID: {classifier.ClassifierId}");

Console.WriteLine(
    $"Created on: {classifier.CreatedOn:O}");

Console.WriteLine("Document types:");

foreach (var documentType in classifier.DocumentTypes.Keys)
{
    Console.WriteLine($"  {documentType}");
}

var evidence = new
{
    classifierId = classifier.ClassifierId,
    createdOn = classifier.CreatedOn,
    documentTypes =
        classifier.DocumentTypes.Keys
            .OrderBy(value => value)
            .ToArray(),
    trainingDataset = new
    {
        kind = "synthetic",
        documentsPerClass = 5,
        totalDocuments = 25,
        holdoutDocumentsUsedForTraining = 0
    },
    note =
        "Synthetic P6 integration evidence; not representative production-quality model evaluation."
};

var evidenceDirectory =
    Path.GetDirectoryName(evidencePath);

if (!string.IsNullOrWhiteSpace(evidenceDirectory))
{
    Directory.CreateDirectory(evidenceDirectory);
}

var json =
    JsonSerializer.Serialize(
        evidence,
        new JsonSerializerOptions
        {
            WriteIndented = true
        });

await File.WriteAllTextAsync(
    evidencePath,
    json);

Console.WriteLine(
    $"Evidence written: {evidencePath}");

static ClassifierDocumentTypeDetails CreateDocumentType(
    Uri containerUri,
    string prefix)
{
    var source =
        new BlobContentSource(containerUri)
        {
            Prefix = prefix
        };

    return new ClassifierDocumentTypeDetails(source);
}