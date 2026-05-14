namespace dsstats.shared;

public class PwaConfig
{
    public const int MinCpuCores = 1;
    public const int MaxCpuCores = 4;
    public const int DefaultCpuCores = 2;

    public string ConfigVersion { get; init; } = "77.0.0";
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    public int CPUCores { get; set; } = DefaultCpuCores;
    public bool UploadCredential { get; set; } = true;
    public List<string> IgnoreReplays { get; set; } = [];
    public string ReplayStartName { get; set; } = "Direct Strike";
    public string Culture { get; set; } = "iv";

    public static int NormalizeCpuCores(int cpuCores)
        => Math.Clamp(cpuCores, MinCpuCores, MaxCpuCores);

    public static PwaConfig Normalize(PwaConfig config)
    {
        config.CPUCores = NormalizeCpuCores(config.CPUCores);
        return config;
    }
}
