namespace GC2Connector;

/// <summary>
/// Two-phase shot handling. GC2 may send an early reading (no spin) then a final
/// reading (with spin) for the same shot. Waits up to 1500ms for the final.
/// </summary>
public sealed class ShotAggregator
{
    private static readonly TimeSpan FinalTimeout = TimeSpan.FromMilliseconds(1500);

    private Gc2ShotData? _pending;
    private CancellationTokenSource? _timerCts;
    private double _lastSpeed;

    public event Action<Gc2ShotData>? ShotReady;

    public void Feed(Gc2ShotData shot)
    {
        if (shot.IsMisread) return;

        // Duplicate filter
        if (Math.Abs(shot.SpeedMph - _lastSpeed) < 0.1 && _pending == null)
            return;

        if (!shot.HasSpin)
        {
            // Early reading — stash and wait for final
            _pending = shot;
            _timerCts?.Cancel();
            _timerCts = new CancellationTokenSource();
            var ct = _timerCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(FinalTimeout, ct);
                    if (_pending != null)
                    {
                        Emit(_pending);
                        _pending = null;
                    }
                }
                catch (OperationCanceledException) { }
            });
        }
        else
        {
            // Final reading with spin — use it
            _timerCts?.Cancel();
            _pending = null;
            Emit(shot);
        }
    }

    private void Emit(Gc2ShotData shot)
    {
        _lastSpeed = shot.SpeedMph;
        ShotReady?.Invoke(shot);
    }
}
