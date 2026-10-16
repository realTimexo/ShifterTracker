using System.Runtime.InteropServices;

namespace ShiftTracker;

/// <summary>Optional DirectInput output. Requires the separately installed vJoy driver.</summary>
internal sealed class VJoyOutput : IDisposable
{
    private const uint DeviceId = 1;
    private const int Own = 0, Free = 1;
    private IntPtr library;
    private bool acquired;
    private byte? activeButton;
    private readonly GetBool noArgEnabled;
    private readonly GetStatus getStatus;
    private readonly GetButtonCount getButtonCount;
    private readonly GetBoolUInt acquire;
    private readonly ReleaseUInt relinquish;
    private readonly SetButton setButton;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool GetBool();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetStatus(uint deviceId);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetButtonCount(uint deviceId);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool GetBoolUInt(uint deviceId);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ReleaseUInt(uint deviceId);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool SetButton([MarshalAs(UnmanagedType.Bool)] bool pressed, uint deviceId, byte button);

    public VJoyOutput(int minimumButtons)
    {
        library = LoadLibrary();
        noArgEnabled = Get<GetBool>("vJoyEnabled");
        getStatus = Get<GetStatus>("GetVJDStatus");
        getButtonCount = Get<GetButtonCount>("GetVJDButtonNumber");
        acquire = Get<GetBoolUInt>("AcquireVJD");
        relinquish = Get<ReleaseUInt>("RelinquishVJD");
        setButton = Get<SetButton>("SetBtn");

        if (!noArgEnabled()) { Dispose(); throw new InvalidOperationException("The vJoy driver is not enabled. Install or enable vJoy, then restart Windows if needed."); }
        int status = getStatus(DeviceId);
        if (status == Free)
        {
            if (!acquire(DeviceId)) { Dispose(); throw new InvalidOperationException("Could not acquire vJoy Device 1. Check whether another app is using it."); }
            acquired = true;
        }
        else if (status == Own) acquired = true;
        else if (status == 2) { Dispose(); throw new InvalidOperationException("vJoy Device 1 is already owned by another application."); }
        else { Dispose(); throw new InvalidOperationException("vJoy Device 1 is missing or its driver is not running. Enable Device 1 in vJoyConf."); }

        int buttons = getButtonCount(DeviceId);
        if (buttons < minimumButtons)
        {
            Dispose();
            throw new InvalidOperationException($"vJoy Device 1 has only {buttons} buttons. Enable at least {minimumButtons} in vJoyConf.");
        }
    }

    public void SetStateButton(byte button)
    {
        if (activeButton == button) return;
        if (activeButton is byte previous && !setButton(false, DeviceId, previous))
            throw new InvalidOperationException($"Could not release vJoy button {previous}.");
        activeButton = null;
        if (!setButton(true, DeviceId, button)) throw new InvalidOperationException($"Could not press vJoy button {button}.");
        activeButton = button;
    }

    public void ReleaseActiveButton()
    {
        if (activeButton is byte previous)
        {
            setButton(false, DeviceId, previous);
            activeButton = null;
        }
    }

    private T Get<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    private static IntPtr LoadLibrary()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "vJoyInterface.dll"),
            Path.Combine(programFiles, "vJoy", "x64", "vJoyInterface.dll"),
            Path.Combine(programFilesX86, "vJoy", "x64", "vJoyInterface.dll"),
            Path.Combine(programFiles, "vJoy", "vJoyInterface.dll"),
            Path.Combine(Environment.SystemDirectory, "vJoyInterface.dll")
        };
        foreach (var candidate in candidates)
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle)) return handle;
        if (NativeLibrary.TryLoad("vJoyInterface.dll", out var loaded)) return loaded;
        throw new DllNotFoundException("vJoyInterface.dll was not found. Install vJoy to use DirectInput output.");
    }

    public void Dispose()
    {
        ReleaseActiveButton();
        if (acquired) { relinquish(DeviceId); acquired = false; }
        if (library != IntPtr.Zero) { NativeLibrary.Free(library); library = IntPtr.Zero; }
    }
}
