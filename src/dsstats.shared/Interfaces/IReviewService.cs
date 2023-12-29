namespace dsstats.shared.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse> GetReview(ReviewRequest request, CancellationToken token = default);
}