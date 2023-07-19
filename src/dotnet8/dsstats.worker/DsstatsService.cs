
using System.Management;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using pax.dsstats.dbng;

using pax.dsstats.shared.Arcade;
using s2protocol.NET;

namespace dsstats.worker;

public partial class DsstatsService
{
    private string appFolder;
    private string connectionString;
    private string configFile;
    private string libPath;
    private List<string> sc2Dirs;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMapper mapper;
    private readonly ILogger<DsstatsService> logger;

    private ReplayDecoderOptions decoderOptions;
    private ReplayDecoder? decoder;
    private readonly SemaphoreSlim ssDecode = new(1, 1);
    private readonly SemaphoreSlim ssSave = new(1, 1);
    private readonly SemaphoreSlim ssUpload = new(1, 1);

    public DsstatsService(IServiceScopeFactory scopeFactory,
                          IHttpClientFactory httpClientFactory,
                          IMapper mapper,
                          ILogger<DsstatsService> logger)
    {
        appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker");

        configFile = Path.Combine(appFolder, "workerconfig.json");
        connectionString = $"Data Source={Path.Combine(appFolder, "dsstats.db")}";
        var sc2Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Starcraft II");
        sc2Dirs = new() { sc2Dir };
        libPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ipax77", "Dsstats Service");

        decoderOptions = new()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };

        this.scopeFactory = scopeFactory;
        this.httpClientFactory = httpClientFactory;
        this.mapper = mapper;
        this.logger = logger;
    }

    private HashSet<Unit> Units = new();
    private HashSet<Upgrade> Upgrades = new();

    private HashSet<PlayerId> PlayerIds = new();

    public async Task StartJob(CancellationToken token = default)
    {
        EnsurePrerequisites();
        UpdateConfig();
        var newReplays = await GetNewReplays();
        if (newReplays.Count > 0)
        {
            try
            {
                int decoded = await Decode(newReplays, token);
                await Upload(token);
                logger.LogWarning("replays decoded: {decoded}", decoded);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
            }
        }
    }

    private void EnsurePrerequisites()
    {
        sc2Dirs = GetMyDocumentsPathAllUsers().Select(s => Path.Combine(s, "Starcraft II")).ToList();

        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.Migrate();
    }

   private static List<string> GetMyDocumentsPathAllUsers()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new();
        }

        const string parcialSubkey = @"\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders";
        const string keyName = "Personal";

        //get sids
        List<string> sids = GetMachineSids();
        List<string> myDocumentsPaths = new();

        if (sids != null)
        {
            foreach (var sid in sids)
            {
                //get paths                  
                var subkey = sid + parcialSubkey;

                using var key = Registry.Users.OpenSubKey(subkey);
                if (key != null)
                {
                    var o = key.GetValue(keyName);
                    if (o != null)
                    {
                        var myDocumentPath = o.ToString();
                        if (myDocumentPath != null)
                        {
                            myDocumentsPaths.Add(myDocumentPath);
                        }
                    }
                }
            }
        }

        return myDocumentsPaths;
    }    

    private static List<string> GetMachineSids()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new();
        }

        ManagementObjectSearcher searcher = new("SELECT * FROM Win32_UserProfile");
        var regs = searcher.Get();
        List<string> sids = new();

        foreach (ManagementObject os in regs.Cast<ManagementObject>())
        {
            if (os["SID"] != null)
            {
                var sid = os["SID"].ToString();
                if (sid != null)
                {
                    sids.Add(sid);
                }
            }
        }
        searcher.Dispose();
        return sids;
    }
}

