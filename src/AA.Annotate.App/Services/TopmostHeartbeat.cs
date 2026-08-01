using Avalonia.Threading;

namespace AA.Annotate.App.Services;

internal sealed class TopmostHeartbeat : IDisposable
{
    private readonly ITopmostPulseTimer _timer;
    private readonly Action _pulse;
    private bool _isEnabled;
    private bool _isDisposed;

    public TopmostHeartbeat(TimeSpan interval, Action pulse)
        : this(new DispatcherTopmostPulseTimer(interval), pulse)
    {
    }

    internal TopmostHeartbeat(ITopmostPulseTimer timer, Action pulse)
    {
        _timer = timer;
        _pulse = pulse;
        _timer.Tick += OnTick;
    }

    public void SetEnabled(bool enabled)
    {
        if (_isDisposed || _isEnabled == enabled)
        {
            return;
        }

        _isEnabled = enabled;
        if (enabled)
        {
            _pulse();
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _isEnabled = false;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_isEnabled)
        {
            _pulse();
        }
    }
}

internal interface ITopmostPulseTimer
{
    event EventHandler? Tick;

    void Start();

    void Stop();
}

internal sealed class DispatcherTopmostPulseTimer : ITopmostPulseTimer
{
    private readonly DispatcherTimer _timer;

    public DispatcherTopmostPulseTimer(TimeSpan interval)
    {
        _timer = new DispatcherTimer { Interval = interval };
    }

    public event EventHandler? Tick
    {
        add => _timer.Tick += value;
        remove => _timer.Tick -= value;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();
}
