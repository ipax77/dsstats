namespace dsstats.shared.Interfaces;

public interface IFaqService
{
    Task<int> GetCount(FaqRequest request, CancellationToken token = default);
    Task<List<FaqDto>> GetList(FaqRequest request, CancellationToken token = default);
    Task<bool> Upvote(int faqId, string clientIp);
    Task<int> CreateFaq(FaqDto faqDto, string? name);
    Task<bool> UpdateFaq(FaqDto faqDto, string? name);
    Task<bool> DeleteFaq(int faqId);
}