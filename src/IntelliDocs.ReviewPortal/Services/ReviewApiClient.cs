using System.Net.Http.Json;
using IntelliDocs.ReviewPortal.Models;

namespace IntelliDocs.ReviewPortal.Services;

public sealed class ReviewApiClient
{
    private readonly HttpClient _httpClient;

    public ReviewApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<DocumentReviewQueueItem>>
        GetQueueAsync(
            CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<
                   List<DocumentReviewQueueItem>>(
                   "api/v1/reviews",
                   cancellationToken)
               ?? [];
    }

    public async Task<DocumentReview?> GetReviewAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<DocumentReview>(
            $"api/v1/reviews/{documentId}",
            cancellationToken);
    }

    public async Task<DocumentReview> StartReviewAsync(
        Guid documentId,
        string reviewer,
        CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.PostAsJsonAsync(
                $"api/v1/reviews/{documentId}/start",
                new StartReviewRequest(reviewer),
                cancellationToken);

        return await ReadReviewAsync(
            response,
            cancellationToken);
    }

    public async Task<DocumentReview> AddCorrectionAsync(
        Guid documentId,
        AddCorrectionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.PostAsJsonAsync(
                $"api/v1/reviews/{documentId}/corrections",
                request,
                cancellationToken);

        return await ReadReviewAsync(
            response,
            cancellationToken);
    }

    public async Task<DocumentReview> CompleteReviewAsync(
        Guid documentId,
        CompleteReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.PostAsJsonAsync(
                $"api/v1/reviews/{documentId}/decision",
                request,
                cancellationToken);

        return await ReadReviewAsync(
            response,
            cancellationToken);
    }

    private static async Task<DocumentReview> ReadReviewAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var detail =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            throw new HttpRequestException(
                $"Review API returned " +
                $"{(int)response.StatusCode} " +
                $"{response.ReasonPhrase}. {detail}");
        }

        return await response.Content
                   .ReadFromJsonAsync<DocumentReview>(
                       cancellationToken)
               ?? throw new InvalidOperationException(
                   "Review API returned an empty response.");
    }
}
