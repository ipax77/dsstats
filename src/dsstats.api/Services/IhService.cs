
namespace dsstats.api.Services;

public class IhService
{
    public IhService(DecodeService decodeService)
    {
        decodeService.DecodeFinished += DecodeFinished;
    }

    private void DecodeFinished(object? sender, DecodeEventArgs e)
    {
        
    }


}
