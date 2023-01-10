using System.Globalization;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace sc2dsstats.maui.Services;

public class OcrService
{
    public async Task<List<string>> GetImagePlayerNames(MemoryStream memoryStream)
    {
        var tempFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tmpImage.png");

        memoryStream.Position = 0;

        using (var stream = File.Open(tempFile, FileMode.Create))
        {
            await memoryStream.CopyToAsync(stream);
        }

        var ocrResult = await GetTextFromOcr(tempFile);
        if (ocrResult != null)
        {
            return GetPlayerNames(ocrResult);
        }

        File.Delete(tempFile);

        return new();
    }

    public async Task<OcrResult?> GetTextFromOcr(string imageFile)
    {
        var language = new Language("en");

        // string imageFile = @"C:\Users\pax77\Bilder\ds\leaverRating\ocr\Screenshot 2023-01-07 133456.png";
        StorageFile storageFile = await StorageFile.GetFileFromPathAsync(imageFile);

        var ocrResult = TryGetOcrResult(language, storageFile);

        if (ocrResult != null)
        {
            return ocrResult;
        }
        else
        {
            return null;
        }

        //CancellationTokenSource cts = new();
        //using (var stream = await storageFile.OpenAsync(FileAccessMode.Read))
        //{
        //    var decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.PngDecoderId, stream);
        //    var ocrResult = await GetOcrResult(language, decoder, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, cts.Token);
        //    return ocrResult?.Text ?? "";
        //}
    }

    private List<string> GetPlayerNames(OcrResult ocrResult)
    {
        var playerNames = new List<string>();

        for (int i = 0; i < ocrResult.Lines.Count; i++)
        {
            OcrLine line = ocrResult.Lines[i];
            string text = line.Text;

            var clanIndex = text.IndexOf('>');
            while (clanIndex > 0 && text.Length > clanIndex + 1)
            {
                text = text[(clanIndex + 1)..];
                clanIndex = text.IndexOf('>');
            }

            if (text.Any(a => char.IsDigit(a)))
            {
                continue;
            }

            var name = text.Trim();
            if (name.ToUpper() == "DIRECT STRIKE")
            {
                continue;
            }

            playerNames.Add(name);
            if (playerNames.Count == 6)
            {
                break;
            }
        }
        return playerNames;
    }

    private OcrResult? TryGetOcrResult(Language language, StorageFile storageFile)
    {
        List<BitmapPixelFormat> pixelFormats = Enum.GetValues(typeof(BitmapPixelFormat)).Cast<BitmapPixelFormat>().ToList();
        List<BitmapAlphaMode> alphaModes = Enum.GetValues(typeof(BitmapAlphaMode)).Cast<BitmapAlphaMode>().ToList();

        List<Task> tasks = new List<Task>();
        CancellationTokenSource cts = new();
        _ = Cancel(cts);
        OcrResult? result = null;

        for (int i = 0; i < pixelFormats.Count; i++)
        {
            for (int j = 0; j < alphaModes.Count; j++)
            {
                BitmapPixelFormat bitmapPixelFormat = pixelFormats[i];
                BitmapAlphaMode bitmapAlphaMode = alphaModes[j];

                var taks = Task.Run(async () =>
                {
                    using (var stream = await storageFile.OpenAsync(FileAccessMode.Read))
                    {
                        var decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.PngDecoderId, stream);

                        var ocrResult = await GetOcrResult(language, decoder, bitmapPixelFormat, bitmapAlphaMode, cts.Token);
                        if (ocrResult != null)
                        {
                            result = ocrResult;
                        }
                    }
                }, cts.Token);
                tasks.Add(taks);
            }
        }
        Task.WaitAll(tasks.ToArray());
        return result;
    }

    private async Task<OcrResult?> GetOcrResult(Language language,
                                                BitmapDecoder decoder,
                                                BitmapPixelFormat pixelFormat,
                                                BitmapAlphaMode alphaMode,
                                                CancellationToken token)
    {
        try
        {
            var bitmap = await decoder.GetSoftwareBitmapAsync(pixelFormat, alphaMode);

            var engine = OcrEngine.TryCreateFromLanguage(language);
            var ocrResult = await engine.RecognizeAsync(bitmap).AsTask(token);

            return ocrResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{pixelFormat}, {alphaMode} => {ex.Message}");
        }
        return null;
    }

    public async Task DoIt()
    {
        List<Task> tasks = new List<Task>();
        CancellationTokenSource cts = new();
        _ = Cancel(cts);

        try
        {
            string imageFile = @"C:\Users\pax77\Bilder\Pictures\carrytychus.png";
            // string imageFile = @"C:\Users\pax77\Bilder\Pictures\sc2_battlenet_p1.png";
            StorageFile storageFile = await StorageFile.GetFileFromPathAsync(imageFile);

            //using var stream = await storageFile.OpenAsync(FileAccessMode.Read);


            List<BitmapPixelFormat> pixelFormats = Enum.GetValues(typeof(BitmapPixelFormat)).Cast<BitmapPixelFormat>().ToList();
            List<BitmapAlphaMode> alphaModes = Enum.GetValues(typeof(BitmapAlphaMode)).Cast<BitmapAlphaMode>().ToList();


            for (int i = 0; i < pixelFormats.Count; i++)
            {
                for (int j = 0; j < alphaModes.Count; j++)
                {
                    Console.WriteLine($"{pixelFormats[i]}, {alphaModes[j]}");
                    var pixelFormat = pixelFormats.ElementAt(i);
                    var alphaMode = alphaModes.ElementAt(j);
                    var task = Task.Run(async () =>
                    {
                        using (var stream = await storageFile.OpenAsync(FileAccessMode.Read))
                        {
                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.PngDecoderId, stream);



                            SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync(pixelFormat, alphaMode);

                            OcrEngine? ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();

                            if (ocrEngine == null)
                            {
                                return;
                            }

                            var ocrResult = await ocrEngine.RecognizeAsync(bitmap);

                            var text = ocrResult.Text;

                            Console.WriteLine($"{text}");
                        }
                    }, cts.Token);
                    tasks.Add(task);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"failed creating test: {ex.Message}");
        }
        Task.WaitAll(tasks.ToArray());
    }

    private async Task Cancel(CancellationTokenSource cts)
    {
        await Task.Delay(10000);
        cts.Cancel();
    }
}
