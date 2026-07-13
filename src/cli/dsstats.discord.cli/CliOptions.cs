namespace dsstats.discord.cli;

internal enum CliCommand
{
    None,
    Init,
    Add
}

internal sealed record CliOptions(
    CliCommand Command,
    string DiscordConfigPath,
    string DatabaseConfigPath,
    string SqlPath,
    string ManualPath,
    bool Production,
    bool ShowHelp)
{
    private static readonly string DataDirectory = OperatingSystem.IsWindows()
        ? @"C:\data\ds\discord\patchnotes"
        : "/data/ds/discord/patchnotes";

    private static readonly string DefaultConfigPath = OperatingSystem.IsWindows()
        ? @"C:\data\localserverconfig.json"
        : "/data/localserverconfig.json";

    private static readonly string DatabaseConfigRelativePath = Path.Combine(
        "src", "server", "dsstats.api", "appsettings.Development.json");

    public static CliOptions Parse(string[] args)
    {
        var command = CliCommand.None;
        var configPath = DefaultConfigPath;
        var databaseConfigPath = FindDefaultDatabaseConfigPath();
        var sqlPath = Path.Combine(DataDirectory, "dsstats_DsUpdates.sql");
        var manualPath = Path.Combine(DataDirectory, "manual.txt");
        var production = false;
        var databaseConfigExplicit = false;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "init" when command == CliCommand.None: command = CliCommand.Init; break;
                case "add" when command == CliCommand.None: command = CliCommand.Add; break;
                case "--config": configPath = ReadValue(args, ref i, "--config"); break;
                case "--db-config":
                    databaseConfigPath = ReadValue(args, ref i, "--db-config");
                    databaseConfigExplicit = true;
                    break;
                case "--sql": sqlPath = ReadValue(args, ref i, "--sql"); break;
                case "--manual": manualPath = ReadValue(args, ref i, "--manual"); break;
                case "--prod" when !production: production = true; break;
                case "--help" or "-h": showHelp = true; break;
                default: throw new ArgumentException($"Unknown or duplicate argument: {args[i]}");
            }
        }

        if (!showHelp && command == CliCommand.None)
        {
            throw new ArgumentException("Missing command. Use 'init' or 'add'.");
        }

        if (production && databaseConfigExplicit)
        {
            throw new ArgumentException("--prod cannot be combined with --db-config. Production uses dsstats:ConnectionString from --config.");
        }

        return new(command, configPath, databaseConfigPath, sqlPath, manualPath, production, showHelp);
    }

    public static void PrintUsage() => Console.WriteLine("""
        Builds and updates the Direct Strike patch-note archive.

        Usage:
          dsstats.discord.cli init [options]
          dsstats.discord.cli add [options]

        Commands:
          init  Synchronize SQL backup and manual additions into MySQL
          add   Add followed Discord posts newer than the database cursor

        Options:
          --config <path>  Discord configuration (default: C:\data\localserverconfig.json)
          --db-config <path> Development DB configuration
                            (default: src/server/dsstats.api/appsettings.Development.json)
          --sql <path>     SQL backup used by init
          --manual <path>  Manual additions used by init
          --prod           Use dsstats:ConnectionString from the production config
          --help, -h       Show help

        Development database connections must target localhost. --prod is always explicit.
        The CLI never applies migrations. Discord configuration must contain ServerID/ChannelID;
        add also requires discord:Token or DSSTATS_DISCORD_TOKEN.
        """);

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        return args[index];
    }

    private static string FindDefaultDatabaseConfigPath()
    {
        var fromCurrentDirectory = FindInParents(Environment.CurrentDirectory);
        if (fromCurrentDirectory is not null)
        {
            return fromCurrentDirectory;
        }

        var fromExecutable = FindInParents(AppContext.BaseDirectory);
        return fromExecutable ?? DatabaseConfigRelativePath;
    }

    private static string? FindInParents(string startPath)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(startPath));
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, DatabaseConfigRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
