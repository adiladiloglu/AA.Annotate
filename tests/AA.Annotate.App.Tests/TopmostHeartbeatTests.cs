using AA.Annotate.App.Services;

namespace AA.Annotate.App.Tests;

public sealed class TopmostHeartbeatTests
{
    [Fact]
    public void EnablingPulsesImmediatelyAndStartsTimerOnce()
    {
        var timer = new FakeTopmostPulseTimer();
        var pulseCount = 0;
        using var heartbeat = new TopmostHeartbeat(timer, () => pulseCount++);

        heartbeat.SetEnabled(true);
        heartbeat.SetEnabled(true);

        Assert.Equal(1, pulseCount);
        Assert.Equal(1, timer.StartCount);
        Assert.Equal(0, timer.StopCount);
    }

    [Fact]
    public void TimerPulseReassertsTopmostOnlyWhileEnabled()
    {
        var timer = new FakeTopmostPulseTimer();
        var pulseCount = 0;
        using var heartbeat = new TopmostHeartbeat(timer, () => pulseCount++);

        heartbeat.SetEnabled(true);
        timer.Pulse();
        heartbeat.SetEnabled(false);
        timer.Pulse();

        Assert.Equal(2, pulseCount);
        Assert.Equal(1, timer.StopCount);
    }

    [Fact]
    public void ReenablingAfterDisablePulsesAndRestartsTimer()
    {
        var timer = new FakeTopmostPulseTimer();
        var pulseCount = 0;
        using var heartbeat = new TopmostHeartbeat(timer, () => pulseCount++);

        heartbeat.SetEnabled(true);
        heartbeat.SetEnabled(false);
        heartbeat.SetEnabled(true);

        Assert.Equal(2, pulseCount);
        Assert.Equal(2, timer.StartCount);
        Assert.Equal(1, timer.StopCount);
    }

    [Fact]
    public void DisposeStopsAndUnsubscribes()
    {
        var timer = new FakeTopmostPulseTimer();
        var pulseCount = 0;
        var heartbeat = new TopmostHeartbeat(timer, () => pulseCount++);
        heartbeat.SetEnabled(true);

        heartbeat.Dispose();
        timer.Pulse();
        heartbeat.SetEnabled(true);

        Assert.Equal(1, pulseCount);
        Assert.Equal(1, timer.StartCount);
        Assert.Equal(1, timer.StopCount);
    }

    private sealed class FakeTopmostPulseTimer : ITopmostPulseTimer
    {
        public event EventHandler? Tick;

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public void Start() => StartCount++;

        public void Stop() => StopCount++;

        public void Pulse() => Tick?.Invoke(this, EventArgs.Empty);
    }
}
