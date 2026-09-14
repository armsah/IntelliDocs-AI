namespace IntelliDocs.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public Guid? FailureInjectionDocumentId { get; set; }
}