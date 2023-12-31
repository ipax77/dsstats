namespace dsstats.shared.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse> GetReview(ReviewRequest request, CancellationToken token = default);
    Task<ReviewResponse> GetReviewRatingTypeInfo(ReviewRequest request, CancellationToken token = default);
    Task<ReviewYearResponse> GetYearReview(RatingType ratingType, int year, CancellationToken token = default);
    Task<ReviewYearResponse> GetYearRatingTypeReview(RatingType ratingType, int year, CancellationToken token = default);
}