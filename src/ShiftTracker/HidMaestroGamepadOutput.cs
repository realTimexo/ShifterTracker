using HIDMaestro;

namespace ShiftTracker;

/// <summary>Optional Xbox 360-compatible virtual gamepad output.</summary>
internal sealed class HidMaestroGamepadOutput : IDisposable
{
    private readonly HMContext context;
    private readonly HMController controller;

    public static bool IsDriverInstalled()
    {
        using var probe = new HMContext();
        return probe.IsDriverInstalled;
    }

    /// <remarks>
    /// Call only after the UI has explained the driver and received an explicit
    /// yes from the user. The caller is expected to run elevated: both the
    /// one-time driver install and virtual-device creation require admin rights.
    /// </remarks>
    public HidMaestroGamepadOutput()
    {
        context = new HMContext();
        try
        {
            context.LoadDefaultProfiles();
            // Reuse an existing driver. InstallDriver() is intentionally not
            // called when one is present because the upstream installer sweeps
            // its virtual devices before a full driver deployment.
            if (!context.IsDriverInstalled)
                context.InstallDriver();

            HMProfile profile = context.GetProfile("xbox-360-wired")
                ?? throw new InvalidOperationException("The bundled Xbox 360 profile is missing.");
            controller = context.CreateController(profile, "shiftertracker-main");
            SetButtons(HMButton.None);
        }
        catch
        {
            context.Dispose();
            throw;
        }
    }

    public void SetButtons(HMButton buttons, HMHat hat = HMHat.None)
    {
        var state = new HMGamepadState { Buttons = buttons, Hat = hat };
        controller.SubmitState(in state);
    }

    public void Dispose()
    {
        try { SetButtons(HMButton.None); } catch { }
        context.Dispose();
    }
}
