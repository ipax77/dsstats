
namespace sc2arcade.crawler
{
    public interface ICrawlerService
    {
        Task GetLobbyHistory(DateTime tillTime, CancellationToken token);
    }
}