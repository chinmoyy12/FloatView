using System;
using System.Windows.Threading;
using FloatView.App.Platform;

namespace FloatView.App.Media;

public class AudioVisualizerService
{
    private readonly DispatcherTimer _timer;
    private readonly Random _rand = new();
    private readonly double[] _bars = new double[6];
    private readonly WasapiAudioMeterService _wasapiMeter = new();
    private bool _isPlaying = false;
    private double _phase = 0;

    public event Action<double[]>? VisualizerFrame;

    public bool IsHardwareMeterAvailable => _wasapiMeter.IsAvailable;

    public AudioVisualizerService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render);
        _timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 fps
        _timer.Tick += OnTick;
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            _isPlaying = value;
            if (_isPlaying && !_timer.IsEnabled)
            {
                _timer.Start();
            }
        }
    }

    public void SetLowPowerMode(bool lowPower)
    {
        _timer.Interval = TimeSpan.FromMilliseconds(lowPower ? 70 : 33);
    }

    public void Stop()
    {
        _timer.Stop();
        Array.Fill(_bars, 0.0);
        VisualizerFrame?.Invoke(_bars);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _phase += 0.2;
        bool hasNonZero = false;

        // Query real-time Windows hardware audio meter
        var (leftPeak, rightPeak) = _wasapiMeter.GetStereoPeakValues();
        float masterPeak = Math.Max(leftPeak, rightPeak);
        bool hasRealAudio = masterPeak > 0.005f;

        for (int i = 0; i < _bars.Length; i++)
        {
            if (hasRealAudio)
            {
                // Dynamic mapping of real hardware audio levels across frequency bands
                double channelPeak = i < 3 ? leftPeak : rightPeak;
                double multiplier = i switch
                {
                    0 => 1.15, // Sub-bass
                    1 => 1.35, // Bass
                    2 => 1.45, // Low-mids
                    3 => 1.45, // High-mids
                    4 => 1.30, // Presence
                    5 => 1.10, // Brilliance
                    _ => 1.0
                };

                // Add slight organic harmonics based on position and phase
                double harmonic = Math.Sin(_phase + (i * 1.1)) * 0.08;
                double target = Math.Clamp((channelPeak * multiplier) + harmonic, 0.05, 1.0);

                // Fast attack, smooth decay
                if (target > _bars[i])
                {
                    _bars[i] = (_bars[i] * 0.3) + (target * 0.7); // Rapid rise
                }
                else
                {
                    _bars[i] = (_bars[i] * 0.8) + (target * 0.2); // Smooth fall
                }
                hasNonZero = true;
            }
            else if (_isPlaying)
            {
                // Fallback: If hardware audio is silent or unmetered, use playback activity wave
                double wave = Math.Sin(_phase + (i * 0.9)) * 0.3 + 0.35;
                double jitter = (_rand.NextDouble() - 0.5) * 0.15;
                double target = Math.Clamp(wave + jitter, 0.1, 0.75);
                _bars[i] = (_bars[i] * 0.7) + (target * 0.3);
                hasNonZero = true;
            }
            else
            {
                // Decay to baseline when paused
                _bars[i] *= 0.72;
                if (_bars[i] < 0.01) _bars[i] = 0;
                else hasNonZero = true;
            }
        }

        VisualizerFrame?.Invoke(_bars);

        if (!_isPlaying && !hasRealAudio && !hasNonZero)
        {
            _timer.Stop();
        }
    }
}
