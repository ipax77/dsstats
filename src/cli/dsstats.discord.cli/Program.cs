namespace dsstats.discord.cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine("Run with --help for usage.");
            return 1;
        }

        if (options.ShowHelp)
        {
            CliOptions.PrintUsage();
            return 0;
        }

        using var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        try
        {
            var requiresToken = options.Command == CliCommand.Add;
            var settings = await DiscordSettings.LoadAsync(
                options.DiscordConfigPath,
                options.DatabaseConfigPath,
                options.Production,
                requiresToken,
                cancellationSource.Token);
            if (options.Production)
            {
                Console.WriteLine("Database mode: PRODUCTION (--prod). Migrations are disabled in this CLI.");
            }
            var result = options.Command switch
            {
                CliCommand.Init => await PatchNoteDatabase.InitializeAsync(
                    options.SqlPath,
                    options.ManualPath,
                    settings,
                    cancellationSource.Token),
                CliCommand.Add => await AddDiscordMessagesAsync(settings, cancellationSource.Token),
                _ => throw new InvalidOperationException("No command selected.")
            };

            if (options.Command == CliCommand.Init)
            {
                Console.WriteLine(
                    $"Legacy synchronization complete: {result.Added:N0} added, {result.Updated:N0} updated, {result.Removed:N0} removed.");
            }
            else
            {
                Console.WriteLine(result.Added == 0
                    ? "No new Direct Strike patch-note changes."
                    : $"Stored {result.Added:N0} new Direct Strike patch-note changes.");
                Console.WriteLine($"Discord cursor: {result.DiscordCursor ?? "<none>"}");
            }

            return 0;
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
            Console.Error.WriteLine("Cancelled.");
            return 2;
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or System.Text.Json.JsonException
            or DiscordApiException
            or InvalidOperationException
            or FormatException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<DatabaseImportResult> AddDiscordMessagesAsync(
        DiscordSettings settings,
        CancellationToken cancellationToken)
    {
        using var httpClient = DiscordApiClient.CreateHttpClient(settings.Token);
        return await PatchNoteDatabase.AddDiscordAsync(
            new DiscordApiClient(httpClient, settings),
            settings,
            cancellationToken);
    }
}
