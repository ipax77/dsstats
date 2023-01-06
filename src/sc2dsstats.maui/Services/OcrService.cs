using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using static System.Net.Mime.MediaTypeNames;

namespace sc2dsstats.maui.Services;

public class OcrService
{
    public async Task GetTextFromOcr()
    {

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
