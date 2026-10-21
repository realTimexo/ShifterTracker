using System.Security.Principal;

namespace ShiftTracker;

internal static class Program
{
    private static readonly string DiagnosticDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShifterTracker");
    private static readonly string DiagnosticLog = Path.Combine(DiagnosticDirectory, "startup.log");

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, eventArgs) => ReportFailure("Windows Forms", eventArgs.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
                ReportFailure("Unhandled process exception", eventArgs.ExceptionObject as Exception
                    ?? new Exception(eventArgs.ExceptionObject?.ToString() ?? "Unknown fatal exception."));

            WriteDiagnostic("Starting ShifterTracker 1.1.0.");
            ApplicationConfiguration.Initialize();
            bool startVirtualGamepad = args.Any(a =>
                string.Equals(a, "--enable-virtual-gamepad", StringComparison.OrdinalIgnoreCase));
            Application.Run(new MainForm(startVirtualGamepad));
            WriteDiagnostic("ShifterTracker exited normally.");
        }
        catch (Exception ex)
        {
            ReportFailure("Startup", ex);
        }
    }

    private static void ReportFailure(string context, Exception exception)
    {
        WriteDiagnostic($"{context}:\n{exception}");
        try
        {
            MessageBox.Show(
                "ShifterTracker could not continue. Details were saved to:\n" + DiagnosticLog +
                "\n\nError: " + exception.Message +
                "\n\nShifterTracker konnte nicht fortgesetzt werden. Details stehen in startup.log.",
                "ShifterTracker 1.1.0",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // The log is still useful if Windows cannot display a dialog.
        }
    }

    private static void WriteDiagnostic(string message)
    {
        try
        {
            Directory.CreateDirectory(DiagnosticDirectory);
            File.AppendAllText(DiagnosticLog,
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Do not let a logging failure prevent the application from starting.
        }
    }

    internal static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
