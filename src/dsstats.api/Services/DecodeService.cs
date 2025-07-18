using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.api.Services;

public class DecodeService(ILogger<DecodeService> logger,
                           IHttpClientFactory httpClientFactory,
                           IServiceScopeFactory scopeFactory)
{
    public EventHandler<DecodeEventArgs>? DecodeFinished;
    public EventHandler<DecodeRawEventArgs>? DecodeRawFinished;

    private void OnDecodeFinished(DecodeEventArgs e)
    {
        DecodeFinished?.Invoke(this, e);
    }

    private void OnRawDecodeRawFinished(DecodeRawEventArgs e)
    {
        DecodeRawFinished?.Invoke(this, e);
    }

    public async Task SaveReplays(Guid guid, List<IFormFile> files)
    {
        var httpClient = httpClientFactory.CreateClient("decode");
        try
        {
            var formData = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                formData.Add(fileContent, "files", file.FileName);
            }

            var result = await httpClient.PostAsync($"/api/v1/decode/upload/{guid}", formData);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("failed saving replays: {error}", ex.Message);
        }
    }

    public async Task ConsumeDecodeResult(Guid guid, List<IhReplay> replays)
    {
        try
        {
            if (replays.Count > 0)
            {
                using var scope = scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
                replays.ForEach(f => f.Replay.FileName = string.Empty);
                await importService.Import(replays.Select(s => s.Replay).ToList());
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed importing decode result: {error}", ex.Message);
        }
        finally
        {
            OnDecodeFinished(new()
            {
                Guid = guid,
                IhReplays = replays,
            });
        }
    }

    public async Task SaveRawReplays(Guid guid, List<IFormFile> files)
    {
        var httpClient = httpClientFactory.CreateClient("decode");
        try
        {
            var formData = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                formData.Add(fileContent, "files", file.FileName);
            }

            var result = await httpClient.PostAsync($"/api/v1/decode/upload/raw/{guid}", formData);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("failed saving raw replays: {error}", ex.Message);
        }
    }

    public async Task ConsumeRawDecodeResult(Guid guid, List<ChallengeResponse> challengeResponses)
    {
        try
        {

        }
        catch (Exception ex)
        {
            logger.LogError("failed importing decode result: {error}", ex.Message);
        }
        finally
        {
            OnRawDecodeRawFinished(new()
            {
                Guid = guid,
                ChallengeResponses = challengeResponses,
            });
        }
    }
}

public class DecodeEventArgs : EventArgs
{
    public Guid Guid { get; set; }
    public List<IhReplay> IhReplays { get; set; } = [];
    public string? Error { get; set; }
}

public class DecodeRawEventArgs : EventArgs
{
    public Guid Guid { get; set; }
    public List<ChallengeResponse> ChallengeResponses { get; set; } = [];
    public string? Error { get; set; }
}