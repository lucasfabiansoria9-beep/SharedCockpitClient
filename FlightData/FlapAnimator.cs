using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Anima flaps en pasos discretos de stepValue (p.ej., 0.5) cada stepMs.
    /// Cancela animaciones previas si llega un nuevo target.
    /// </summary>
    public sealed class FlapAnimator : IDisposable
    {
        private readonly int stepMs;
        private readonly double stepValue;
        private readonly object gate = new();
        private CancellationTokenSource? cts;

        public double Current { get; private set; }

        public FlapAnimator(int stepMs = 50, double stepValue = 0.5)
        {
            this.stepMs = stepMs;
            this.stepValue = stepValue;
        }

        public void AnimateTo(double target)
        {
            target = Math.Round(target * 2.0, MidpointRounding.AwayFromZero) / 2.0; // clamp al grid 0.5
            lock (gate)
            {
                cts?.Cancel();
                cts = new CancellationTokenSource();
                _ = RunAnimationAsync(Current, target, cts.Token);
            }
        }

        private async Task RunAnimationAsync(double start, double target, CancellationToken token)
        {
            Console.WriteLine($"[AnimStart] Flaps a {target} (step= {stepValue})");
            var dir = target > start ? 1.0 : -1.0;
            var value = start;

            while (!token.IsCancellationRequested && Math.Abs(value - target) > 1e-9)
            {
                var next = value + dir * stepValue;
                // último paso exacto al target para evitar overshoot por discretización
                if ((dir > 0 && next > target) || (dir < 0 && next < target)) next = target;

                value = next;
                Current = value;
                Console.WriteLine($"[Broadcast] Flaps step -> {value:0.0}");
                try
                {
                    await Task.Delay(stepMs, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            if (!token.IsCancellationRequested)
                Console.WriteLine("[AnimEnd] Flaps completado");
        }

        public void Dispose()
        {
            lock (gate)
            {
                cts?.Cancel();
                cts?.Dispose();
                cts = null;
            }
        }
    }
}
