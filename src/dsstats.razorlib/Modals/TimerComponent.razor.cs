using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace dsstats.razorlib.Modals;

public partial class TimerComponent : ComponentBase, IDisposable
{
    [Inject]
    public ILogger<TimerComponent> Logger { get; set; } = null!;

    private System.Timers.Timer _timer = null!;
    private int _secondsToRun = 0;

    protected string Time { get; set; } = "00:00";
    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public EventCallback TimerOut { get; set; }

    public void Start(int secondsToRun)
    {
        _secondsToRun = secondsToRun;
        Logger.LogWarning("Starting timer for {Seconds} seconds", _secondsToRun);
        if (_secondsToRun > 0)
        {
            Time = TimeSpan.FromSeconds(_secondsToRun).ToString(@"mm\:ss");
            InvokeAsync(StateHasChanged);
            _timer.Start();
        }
    }

    public void Stop()
    {
        _timer.Stop();
    }

    protected override void OnInitialized()
    {
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
    }

    private async void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        _secondsToRun--;

        await InvokeAsync(() =>
        {
            Time = TimeSpan.FromSeconds(_secondsToRun).ToString(@"mm\:ss");
            Logger.LogWarning("Timer ticked, {Seconds} seconds remaining", _secondsToRun);

            if (_secondsToRun <= 0)
            {
                Logger.LogWarning("Timer finished after {Seconds} seconds", _secondsToRun);
                _timer.Stop();
                TimerOut.InvokeAsync();
                _secondsToRun = 0;
            }
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}