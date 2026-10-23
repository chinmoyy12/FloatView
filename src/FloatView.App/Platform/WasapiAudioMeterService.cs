using System;
using System.Runtime.InteropServices;
using FloatView.App.Persistence;

namespace FloatView.App.Platform;

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumeratorComObject { }

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
    [PreserveSig]
    int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
    [PreserveSig]
    int RegisterEndpointNotificationCallback(IntPtr pClient);
    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IntPtr pClient);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
    [PreserveSig]
    int OpenPropertyStore(int stgmAccess, out IntPtr ppProperties);
    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
    [PreserveSig]
    int GetState(out int pdwState);
}

[ComImport]
[Guid("C02216F6-0380-4D5B-9C00-DAC82BE3EF83"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioMeterInformation
{
    [PreserveSig]
    int GetPeakValue(out float pfPeak);
    [PreserveSig]
    int GetMeteringChannelCount(out int pnChannelCount);
    [PreserveSig]
    int GetChannelsPeakValues(int u32ChannelCount, [In, Out] float[] afPeakValues);
    [PreserveSig]
    int QueryHardwareSupport(out int pdwHardwareSupportMask);
}

public class WasapiAudioMeterService
{
    private static readonly Guid IidAudioMeter = new("C02216F6-0380-4D5B-9C00-DAC82BE3EF83");
    private const int CLSCTX_INPROC_SERVER = 1;
    private const int eRender = 0;
    private const int eMultimedia = 1;

    private IMMDeviceEnumerator? _enumerator;
    private IMMDevice? _device;
    private IAudioMeterInformation? _meter;
    private DateTime _lastAttempt = DateTime.MinValue;
    private bool _isInitialized = false;

    public bool IsAvailable => _meter != null;

    private void EnsureDevice()
    {
        if (_meter != null) return;
        if ((DateTime.UtcNow - _lastAttempt).TotalSeconds < 2) return; // Throttled retry
        _lastAttempt = DateTime.UtcNow;

        try
        {
            _enumerator ??= (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            int hr = _enumerator.GetDefaultAudioEndpoint(eRender, eMultimedia, out _device);
            if (hr == 0 && _device != null)
            {
                var iid = IidAudioMeter;
                hr = _device.Activate(ref iid, CLSCTX_INPROC_SERVER, IntPtr.Zero, out IntPtr pMeter);
                if (hr == 0 && pMeter != IntPtr.Zero)
                {
                    try
                    {
                        var obj = Marshal.GetObjectForIUnknown(pMeter);
                        if (obj is IAudioMeterInformation meter)
                        {
                            _meter = meter;
                            _isInitialized = true;
                        }
                    }
                    finally
                    {
                        Marshal.Release(pMeter);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _meter = null;
            _device = null;
            if (!_isInitialized)
            {
                SettingsService.Log($"WASAPI Audio Meter initialization info: {ex}");
            }
        }
    }

    public float GetPeakValue()
    {
        EnsureDevice();
        if (_meter == null) return 0f;

        try
        {
            int hr = _meter.GetPeakValue(out float peak);
            if (hr == 0)
            {
                return Math.Clamp(peak, 0f, 1f);
            }
            _meter = null;
            _device = null;
        }
        catch
        {
            _meter = null;
            _device = null;
        }

        return 0f;
    }

    public (float Left, float Right) GetStereoPeakValues()
    {
        EnsureDevice();
        if (_meter == null) return (0f, 0f);

        try
        {
            float[] channels = new float[2];
            int hr = _meter.GetChannelsPeakValues(2, channels);
            if (hr == 0)
            {
                return (Math.Clamp(channels[0], 0f, 1f), Math.Clamp(channels[1], 0f, 1f));
            }
        }
        catch
        {
            // Fall back to master peak
        }

        float master = GetPeakValue();
        return (master, master);
    }
}
