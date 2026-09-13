namespace IntelliDocs.Core.Documents;

public sealed class InvalidDocumentStateTransitionException : InvalidOperationException
{
    public InvalidDocumentStateTransitionException(
        DocumentStatus currentStatus,
        DocumentStatus requestedStatus)
        : base(
            $"Transition from '{currentStatus}' to '{requestedStatus}' is not allowed.")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public DocumentStatus CurrentStatus { get; }

    public DocumentStatus RequestedStatus { get; }
}
