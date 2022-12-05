namespace sc2dsstats.maui.Services
{
    public interface IFromServerSwitchService
    {
        bool GetFromServer();
        void SetFromServer(bool fromServer);
    }
}