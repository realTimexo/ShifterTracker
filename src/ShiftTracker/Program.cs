using System.Security.Principal;

namespace ShiftTracker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        bool startVirtualGamepad = args.Any(a =>
            string.Equals(a, "--enable-virtual-gamepad", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(startVirtualGamepad));
    }

    internal static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
