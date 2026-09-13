namespace IntelliDocs.Core.Documents;

public enum DocumentStatus
{
    Submitted = 0,
    Stored = 1,
    Queued = 2,
    Processing = 3,
    Extracted = 4,
    Validating = 5,
    Approved = 6,
    NeedsReview = 7,
    InReview = 8,
    Rejected = 9,
    Publishing = 10,
    Completed = 11,
    Failed = 12,
    DeadLettered = 13
}
