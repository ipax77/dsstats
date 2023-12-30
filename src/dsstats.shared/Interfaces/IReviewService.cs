namespace dsstats.shared.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse> GetReview(ReviewRequest request, CancellationToken token = default);
    Task<ReviewResponse> GetReviewRatingTypeInfo(ReviewRequest request, CancellationToken token = default);
}