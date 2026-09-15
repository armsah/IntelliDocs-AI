using IntelliDocs.Core.DocumentClassification;
using IntelliDocs.Core.DocumentIntelligence;

namespace IntelliDocs.Core.Validation;

public static class DocumentConfidencePolicy
{
    public const double AutoApproveThreshold = 0.90;
    public const double ReviewThreshold = 0.70;

    public static DocumentValidationResult Evaluate(
        DocumentClassificationResult classification,
        DocumentAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(
            classification);

        ArgumentNullException.ThrowIfNull(
            analysis);

        var normalizedFields =
            DocumentFieldNormalizer.Normalize(
                analysis.Fields);

        var issues =
            DocumentBusinessValidator
                .Validate(
                    classification.DocumentType,
                    normalizedFields)
                .ToList();

        var requiredFields =
            DocumentBusinessValidator
                .GetRequiredFields(
                    classification.DocumentType);

        var policyConfidence =
            CalculatePolicyConfidence(
                classification.Confidence,
                normalizedFields,
                requiredFields);

        if (policyConfidence <
            ReviewThreshold)
        {
            issues.Add(
                new ValidationIssue(
                    "LOW_CONFIDENCE",
                    $"Policy confidence {policyConfidence:F4} is below the review threshold {ReviewThreshold:F2}.",
                    ValidationSeverity.Warning));
        }
        else if (policyConfidence <
                 AutoApproveThreshold)
        {
            issues.Add(
                new ValidationIssue(
                    "REVIEW_CONFIDENCE",
                    $"Policy confidence {policyConfidence:F4} is below the auto-approval threshold {AutoApproveThreshold:F2}.",
                    ValidationSeverity.Info));
        }

        var structuredAutoApprovalSupported =
            requiredFields.Count > 0;

        var hasBlockingIssue =
            issues.Any(
                issue =>
                    issue.Severity is
                        ValidationSeverity.Error or
                        ValidationSeverity.Critical);

        var decision =
            structuredAutoApprovalSupported &&
            policyConfidence >=
                AutoApproveThreshold &&
            !hasBlockingIssue
                ? DocumentRoutingDecision.Approved
                : DocumentRoutingDecision.NeedsReview;

        return new DocumentValidationResult(
            classification.DocumentType,
            policyConfidence,
            decision,
            normalizedFields,
            issues);
    }

    private static double CalculatePolicyConfidence(
        double classificationConfidence,
        IReadOnlyList<NormalizedDocumentField> fields,
        IReadOnlyList<string> requiredFields)
    {
        var confidence =
            classificationConfidence;

        foreach (var requiredField in requiredFields)
        {
            var field =
                fields.FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Name,
                            requiredField,
                            StringComparison.OrdinalIgnoreCase));

            if (field is null ||
                string.IsNullOrWhiteSpace(
                    field.NormalizedValue))
            {
                continue;
            }

            if (field.Confidence.HasValue)
            {
                confidence =
                    Math.Min(
                        confidence,
                        field.Confidence.Value);
            }
        }

        return confidence;
    }
}
