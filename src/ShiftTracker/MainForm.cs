using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;
using HIDMaestro;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ShiftTracker;

public sealed class MainForm : Form
{
    private static readonly string[] V1States = ["1", "2", "3", "4", "5", "R", "N"];
    private static readonly string[] V2States = ["1", "2", "3", "4", "5", "R", "NL", "N", "NR"];
    private static readonly string[] AllStates = ["1", "2", "3", "4", "5", "R", "NL", "N", "NR"];
    private static readonly string ConfigFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ShifterTracker", "settings.json");

    // A charcoal / cool-teal instrument palette, intentionally distinct from
    // generic green-and-purple dashboard themes.
    private static readonly Color Canvas = Color.FromArgb(19, 22, 26);
    private static readonly Color Surface = Color.FromArgb(27, 31, 36);
    private static readonly Color Card = Color.FromArgb(34, 39, 45);
    private static readonly Color Raised = Color.FromArgb(43, 49, 56);
    private static readonly Color Border = Color.FromArgb(57, 65, 73);
    private static readonly Color Ink = Color.FromArgb(235, 239, 242);
    private static readonly Color Muted = Color.FromArgb(150, 160, 169);
    private static readonly Color Teal = Color.FromArgb(83, 211, 198);
    private static readonly Color Amber = Color.FromArgb(232, 174, 103);
    private static readonly Color Red = Color.FromArgb(228, 106, 112);

    private static readonly Dictionary<string, (string En, string De)> Copy = new()
    {
        ["app.title"] = ("SHIFTTRACKER", "SHIFTTRACKER"),
        ["app.subtitle"] = ("CAMERA-DRIVEN H-PATTERN INPUT", "KAMERA-GESTEUERTE H-SCHALTUNG"),
        ["nav.live"] = ("Live view", "Live-Ansicht"),
        ["nav.setup"] = ("Setup & tracking", "Setup & Tracking"),
        ["nav.inputs"] = ("Inputs & output", "Eingaben & Ausgabe"),
        ["nav.dev"] = ("Diagnostics", "Diagnose"),
        ["camera.heading"] = ("Camera source", "Kameraquelle"),
        ["camera.desc"] = ("Choose the camera aimed at the shifter.", "Wähle die Kamera, die auf den Schalthebel gerichtet ist."),
        ["camera.scan"] = ("Scan cameras", "Kameras suchen"),
        ["camera.connect"] = ("Connect", "Verbinden"),
        ["camera.none"] = ("No camera", "Keine Kamera"),
        ["camera.ready"] = ("Camera connected", "Kamera verbunden"),
        ["camera.error"] = ("Camera error", "Kamerafehler"),
        ["monitor.live"] = ("LIVE TRACKING", "LIVE-TRACKING"),
        ["monitor.current"] = ("CURRENT POSITION", "AKTUELLE POSITION"),
        ["monitor.pattern"] = ("5-SPEED H-PATTERN", "5-GANG-H-SCHALTBILD"),
        ["monitor.arm"] = ("Arm detection", "Erkennung aktivieren"),
        ["monitor.pause"] = ("Pause output", "Ausgabe pausieren"),
        ["monitor.openSetup"] = ("Open setup", "Setup öffnen"),
        ["monitor.notDetected"] = ("No position locked", "Keine Position erkannt"),
        ["monitor.marker"] = ("Color marker", "Farbmarker"),
        ["monitor.image"] = ("Image matching", "Bildvergleich"),
        ["monitor.alignment"] = ("CAMERA ALIGNMENT", "KAMERAAUSRICHTUNG"),
        ["monitor.output"] = ("OUTPUT", "AUSGABE"),
        ["setup.title"] = ("Camera, orientation and calibration", "Kamera, Ausrichtung und Einlernen"),
        ["setup.orientation"] = ("View & image orientation", "Ansicht & Bildausrichtung"),
        ["setup.view"] = ("Camera perspective", "Kameraperspektive"),
        ["setup.top"] = ("Top-down", "Von oben"),
        ["setup.bottom"] = ("Bottom-up (reference photos)", "Von unten (Referenzbilder)"),
        ["setup.rotation"] = ("Rotate image", "Bild drehen"),
        ["setup.rotate0"] = ("0°", "0°"),
        ["setup.rotate90"] = ("90° clockwise", "90° im Uhrzeigersinn"),
        ["setup.rotate180"] = ("180°", "180°"),
        ["setup.rotate270"] = ("90° counter-clockwise", "90° gegen Uhrzeigersinn"),
        ["setup.mirrorH"] = ("Mirror horizontally", "Horizontal spiegeln"),
        ["setup.mirrorV"] = ("Mirror vertically", "Vertikal spiegeln"),
        ["setup.tracking"] = ("Recognition method", "Erkennungsmethode"),
        ["setup.markerMode"] = ("Color marker", "Farbmarker"),
        ["setup.imageMode"] = ("Image matching (no sticker)", "Bildvergleich (ohne Aufkleber)"),
        ["setup.engine"] = ("Camera movement compensation", "Kamerabewegung ausgleichen"),
        ["setup.legacy"] = ("Legacy · fixed camera", "Alt · feste Kamera"),
        ["setup.adaptive"] = ("Adaptive V2 · re-align each frame", "Adaptiv V2 · jedes Bild neu ausrichten"),
        ["setup.version"] = ("Shift protocol", "Schaltprotokoll"),
        ["setup.v1"] = ("V1 · direct gear output", "V1 · direkte Gang-Ausgabe"),
        ["setup.v2"] = ("V2 · ordered neutral gates", "V2 · Neutralgassen-Sequenz"),
        ["setup.calibration"] = ("Teach positions", "Positionen einlernen"),
        ["setup.calibrationDesc"] = ("Move the lever to each position, then capture it. V2 also learns N-L and N-R.", "Stelle den Hebel in jede Position und lerne sie ein. V2 erfasst auch N-L und N-R."),
        ["setup.learn"] = ("Capture", "Einlernen"),
        ["setup.clear"] = ("Clear calibration", "Kalibrierung löschen"),
        ["setup.progress"] = ("positions learned", "Positionen gelernt"),
        ["setup.markerHint"] = ("For marker mode, click the marker in the live image first.", "Im Farbmarker-Modus zuerst im Livebild auf den Marker klicken."),
        ["inputs.title"] = ("Choose what the game receives", "Festlegen, was das Spiel empfängt"),
        ["inputs.output"] = ("Output device", "Ausgabegerät"),
        ["inputs.keyboard"] = ("Keyboard keys", "Tastaturtasten"),
        ["inputs.vjoy"] = ("vJoy · DirectInput", "vJoy · DirectInput"),
        ["inputs.virtual"] = ("Built-in virtual Xbox 360 pad", "Integriertes virtuelles Xbox-360-Gamepad"),
        ["inputs.start"] = ("Start output", "Ausgabe starten"),
        ["inputs.stop"] = ("Stop output", "Ausgabe stoppen"),
        ["inputs.keyMap"] = ("KEY BINDINGS", "TASTENBELEGUNG"),
        ["inputs.capture"] = ("Click a field, then press the key to bind it.", "Feld anklicken, dann die gewünschte Taste drücken."),
        ["inputs.padMap"] = ("VIRTUAL GAMEPAD BUTTONS", "VIRTUELLE GAMEPAD-TASTEN"),
        ["inputs.padHelp"] = ("Each position needs its own button. The game sees one held button for the active position.", "Jede Position braucht eine eigene Taste. Das Spiel sieht für die aktuelle Position genau einen gehaltenen Button."),
        ["inputs.double"] = ("V1 keyboard: send a second tap for gear-to-gear shifts", "V1-Tastatur: Gang-zu-Gang-Wechsel doppelt drücken"),
        ["inputs.gap"] = ("Tap interval", "Tastendruck-Abstand"),
        ["inputs.vjoyHelp"] = ("vJoy must already be installed and Device 1 configured with at least 9 buttons.", "vJoy muss installiert sein; Gerät 1 braucht mindestens 9 Buttons."),
        ["inputs.driverTitle"] = ("One-time driver install; elevation each session", "Einmalige Treiberinstallation; Admin-Abfrage je Sitzung"),
        ["inputs.driverHelp"] = ("The virtual pad is built in. Windows still requires administrator approval to create a virtual device. On first use, the bundled UMDF driver and its local signing certificate are installed; the tracker itself is not installed as a separate program.", "Das virtuelle Gamepad ist integriert. Windows verlangt trotzdem Administratorrechte, um ein virtuelles Gerät anzulegen. Beim ersten Verwenden werden der gebündelte UMDF-Treiber und sein lokales Signaturzertifikat eingerichtet; der Tracker wird nicht als separates Programm installiert."),
        ["inputs.driverConfirm"] = ("Install the bundled HIDMaestro UMDF virtual-device driver? Windows will ask for administrator approval. This adds a locally trusted signing certificate. The driver is installed once; creating the virtual pad requires an elevated tracker session each time.", "Den gebündelten HIDMaestro-UMDF-Treiber installieren? Windows fragt nach Administratorrechten. Dabei wird ein lokal vertrautes Signaturzertifikat hinzugefügt. Der Treiber wird einmal installiert; zum Erstellen des virtuellen Gamepads benötigt jede Sitzung erhöhte Rechte."),
        ["inputs.elevateConfirm"] = ("The virtual gamepad requires administrator rights. The tracker will restart elevated for this session; the main app does not install separately. Continue?", "Das virtuelle Gamepad benötigt Administratorrechte. Der Tracker startet für diese Sitzung mit erhöhten Rechten neu; die Haupt-App wird nicht separat installiert. Fortfahren?"),
        ["inputs.notAdmin"] = ("Virtual gamepad needs administrator approval.", "Das virtuelle Gamepad benötigt Administratorrechte."),
        ["inputs.ready"] = ("Output ready", "Ausgabe bereit"),
        ["inputs.paused"] = ("Output paused", "Ausgabe pausiert"),
        ["inputs.driverMissing"] = ("Driver not installed", "Treiber nicht installiert"),
        ["inputs.vjoyMissing"] = ("vJoy is not available", "vJoy ist nicht verfügbar"),
        ["inputs.error"] = ("Output could not start", "Ausgabe konnte nicht gestartet werden"),
        ["diag.title"] = ("Developer diagnostics", "Entwicklerdiagnose"),
        ["diag.desc"] = ("Frame-by-frame recognition, alignment confidence and output events.", "Erkennung, Ausrichtungsvertrauen und Ausgabeereignisse pro Bild."),
        ["diag.alignment"] = ("ALIGNMENT", "AUSRICHTUNG"),
        ["diag.features"] = ("FEATURES", "MERKMALE"),
        ["diag.candidate"] = ("CANDIDATE", "KANDIDAT"),
        ["diag.active"] = ("ACTIVE STATE", "AKTIVER ZUSTAND"),
        ["diag.log"] = ("EVENT LOG", "EREIGNISLOG"),
        ["diag.clear"] = ("Clear log", "Log leeren"),
        ["diag.copy"] = ("Copy log", "Log kopieren"),
        ["diag.noAnchor"] = ("Adaptive reference not set · start calibration to create one", "Adaptive Referenz fehlt · beim Einlernen wird sie erstellt"),
        ["diag.legacy"] = ("Legacy tracking · no camera-motion correction", "Altes Tracking · kein Kameraausgleich"),
        ["status.ready"] = ("READY", "BEREIT"),
        ["status.cameraMissing"] = ("NO CAMERA", "KEINE KAMERA"),
        ["status.cameraConnected"] = ("CAMERA CONNECTED", "KAMERA VERBUNDEN"),
        ["status.needCalibration"] = ("CALIBRATION NEEDED", "EINLERNEN ERFORDERLICH"),
        ["status.armed"] = ("DETECTION ARMED", "ERKENNUNG AKTIV"),
        ["status.paused"] = ("OUTPUT PAUSED", "AUSGABE PAUSIERT"),
        ["status.trackingLost"] = ("ALIGNMENT LOST · OUTPUT HELD", "AUSRICHTUNG VERLOREN · AUSGABE GEHALTEN"),
        ["status.driverReady"] = ("VIRTUAL GAMEPAD READY", "VIRTUELLES GAMEPAD BEREIT"),
        ["status.vjoyReady"] = ("VJOY DEVICE 1 READY", "VJOY-GERÄT 1 BEREIT"),
        ["log.started"] = ("ShifterTracker 1.0.1 started. Settings stay on this PC.", "ShifterTracker 1.0.1 gestartet. Einstellungen bleiben auf diesem PC."),
        ["log.scan"] = ("Camera scan found {0} device(s).", "Kamerasuche: {0} Gerät(e) gefunden."),
        ["log.noCamera"] = ("No camera was found.", "Keine Kamera gefunden."),
        ["log.cameraConnected"] = ("Camera {0} connected · 640×480 / fast profile.", "Kamera {0} verbunden · schnelles Profil 640×480."),
        ["log.cameraError"] = ("Could not open camera {0}.", "Kamera {0} konnte nicht geöffnet werden."),
        ["log.calibrationReset"] = ("{0} · calibration cleared; teach the positions again.", "{0} · Kalibrierung gelöscht; Positionen neu einlernen."),
        ["log.anchor"] = ("Adaptive reference captured · {0} fixed background features.", "Adaptive Referenz erfasst · {0} feste Hintergrundmerkmale."),
        ["log.learnedImage"] = ("Image sample learned for {0}.", "Bildvorlage für {0} eingelernt."),
        ["log.learnedPosition"] = ("Position {0} learned at x={1:F3}, y={2:F3}.", "Position {0} bei x={1:F3}, y={2:F3} eingelernt."),
        ["log.shift"] = ("Shift {0} → {1} · {2}", "Schaltwechsel {0} → {1} · {2}"),
        ["log.outputKey"] = ("Key {0} sent {1}×.", "Taste {0} {1}× gesendet."),
        ["log.outputPad"] = ("Virtual pad state: {0} · {1}.", "Virtueller Gamepad-Zustand: {0} · {1}."),
        ["log.alignLost"] = ("Adaptive alignment rejected this frame · output held for safety.", "Adaptive Ausrichtung dieses Bild verworfen · Ausgabe sicher gehalten."),
        ["log.outputStarted"] = ("Output started: {0}.", "Ausgabe gestartet: {0}."),
        ["log.outputStopped"] = ("Output stopped; active buttons released.", "Ausgabe beendet; aktive Buttons freigegeben."),
        ["log.error"] = ("Error: {0}", "Fehler: {0}")
    };

    private readonly Dictionary<string, string> keyAssignments = new()
    {
        ["1"] = "F13", ["2"] = "F14", ["3"] = "F15", ["4"] = "F16", ["5"] = "F17",
        ["R"] = "F18", ["N"] = "F19", ["NL"] = "F20", ["NR"] = "F21"
    };
    private readonly Dictionary<string, string> padAssignments = new()
    {
        ["1"] = "DpadUp", ["2"] = "DpadDown", ["3"] = "DpadLeft", ["4"] = "DpadRight", ["5"] = "A",
        ["R"] = "B", ["NL"] = "X", ["N"] = "Y", ["NR"] = "LeftBumper"
    };

    private readonly Dictionary<Control, string> localizedControls = new();
    private readonly Dictionary<string, TextBox> keyBoxes = new();
    private readonly Dictionary<string, Label> keyCaptions = new();
    private readonly Dictionary<string, ComboBox> padBoxes = new();
    private readonly Queue<string> v2OutputQueue = new();
    private readonly HashSet<string> v2NeutralSent = new();
    private readonly Dictionary<string, (double X, double Y)> positions = new();
    private readonly Dictionary<string, Mat> imageTemplates = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly MotionCompensator motionCompensator = new();
    private readonly System.Windows.Forms.Timer frameTimer = new() { Interval = 16 };

    private readonly ComboBox languageSelect = new();
    private readonly ComboBox cameraSelect = new();
    private readonly ComboBox versionSelect = new();
    private readonly ComboBox outputSelect = new();
    private readonly ComboBox viewSelect = new();
    private readonly ComboBox trackingSelect = new();
    private readonly ComboBox alignmentSelect = new();
    private readonly ComboBox rotationSelect = new();
    private readonly CheckBox mirrorH = new();
    private readonly CheckBox mirrorV = new();
    private readonly CheckBox doublePressCheck = new();
    private readonly NumericUpDown doublePressGap = new();
    private readonly PictureBox preview = new();
    private readonly TextBox logBox = new();
    private readonly Panel pageHost = new();
    private readonly Label headerStatus = new();
    private readonly Label cameraStatus = new();
    private readonly Label currentGearLabel = new();
    private readonly Label trackerStatus = new();
    private readonly Label outputStatus = new();
    private readonly Label calibrationProgress = new();
    private readonly Label keyCaptureHint = new();
    private readonly Label alignmentValue = new();
    private readonly Label featureValue = new();
    private readonly Label candidateValue = new();
    private readonly Label activeValue = new();
    private readonly Button armButton = new();
    private readonly Button startOutputButton = new();
    private readonly FlowLayoutPanel calibrationButtons = new();
    private readonly Panel keyMapPanel = new();
    private readonly Panel padMapPanel = new();
    private readonly Label markerModeHint = new();
    private GearPatternView? patternView;

    private string language = "en";
    private string selectedOutput = "keyboard";
    private string? statusKey = "status.cameraMissing";
    private bool statusSuccess;
    private string selectedCandidate = "—";
    private string? activeGear;
    private string? candidateGear;
    private string? lastConfirmedGear;
    private string? lastAlignmentLogState;
    private long candidateSince;
    private int selectedCamera;
    private int selectedRotation;
    private int selectedViewIndex = 1;
    private int selectedTrackingIndex = 1;
    private bool selectedMirrorH;
    private bool selectedMirrorV;
    private bool selectedVersion2 = true;
    private bool adaptiveAlignment = true;
    private bool doublePressEnabled = true;
    private int doublePressGapMs = 90;
    private bool suppressUiEvents;
    private bool armed;
    private bool outputReady;
    private bool outputBusy;
    private bool v2PumpRunning;
    private bool autoStartGamepad;
    private Task? cameraScanTask;
    private string? alignmentAnchorBase64;
    private (int H, int S, int V)? markerColor;
    private (double X, double Y)? detected;
    private TextBox? keyCaptureTarget;
    private VideoCapture? capture;
    private Mat? lastFrame;
    private VJoyOutput? vJoyOutput;
    private HidMaestroGamepadOutput? gamepadOutput;

    private static readonly (string Id, HMButton Button, HMHat Hat, string Short)[] PadChoices =
    [
        ("DpadUp", HMButton.None, HMHat.North, "D-Pad ↑"),
        ("DpadDown", HMButton.None, HMHat.South, "D-Pad ↓"),
        ("DpadLeft", HMButton.None, HMHat.West, "D-Pad ←"),
        ("DpadRight", HMButton.None, HMHat.East, "D-Pad →"),
        ("A", HMButton.A, HMHat.None, "A"), ("B", HMButton.B, HMHat.None, "B"),
        ("X", HMButton.X, HMHat.None, "X"), ("Y", HMButton.Y, HMHat.None, "Y"),
        ("LeftBumper", HMButton.LeftBumper, HMHat.None, "LB"),
        ("RightBumper", HMButton.RightBumper, HMHat.None, "RB"),
        ("LeftStick", HMButton.LeftStick, HMHat.None, "L3"), ("RightStick", HMButton.RightStick, HMHat.None, "R3")
    ];

    public MainForm(bool startVirtualGamepad = false)
    {
        autoStartGamepad = startVirtualGamepad;
        Text = "ShifterTracker 1.0.1";
        MinimumSize = new Size(1080, 720);
        Size = new Size(1320, 850);
        BackColor = Canvas;
        ForeColor = Ink;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        LoadSettings();
        BuildUi();
        frameTimer.Interval = selectedVersion2 ? 16 : 33;
        frameTimer.Tick += (_, _) => FrameTick();
        frameTimer.Start();
        ApplyCameraProfile();
        RefreshVersionUi();
        RefreshOutputUi();
        SetStatus(statusKey ?? "status.cameraMissing", statusSuccess);
        Log(T("log.started"));

        Shown += async (_, _) =>
        {
            if (autoStartGamepad && selectedOutput == "xbox")
            {
                autoStartGamepad = false;
                await StartOutputAsync();
            }
            if (!IsDisposed) await ScanCamerasAsync(false);
        };
        FormClosing += (_, _) => Shutdown();
    }

    private string T(string key)
    {
        if (!Copy.TryGetValue(key, out var value)) return key;
        return language == "de" ? value.De : value.En;
    }

    private TControl Bind<TControl>(TControl control, string key) where TControl : Control
    {
        localizedControls[control] = key;
        control.Text = T(key);
        return control;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Canvas, Padding = new Padding(18, 14, 18, 16),
            ColumnCount = 1, RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Canvas };
        root.Controls.Add(header, 0, 0);
        var appTitle = Bind(new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), ForeColor = Ink, Location = new Point(0, 0) }, "app.title");
        header.Controls.Add(appTitle);
        var subtitle = Bind(new Label { AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Muted, Location = new Point(3, 35) }, "app.subtitle");
        header.Controls.Add(subtitle);
        headerStatus.AutoSize = true;
        headerStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        headerStatus.Location = new Point(790, 16);
        headerStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        header.Controls.Add(headerStatus);
        languageSelect.DropDownStyle = ComboBoxStyle.DropDownList;
        languageSelect.Items.AddRange(["English", "Deutsch"]);
        languageSelect.SelectedIndex = language == "de" ? 1 : 0;
        StyleCombo(languageSelect);
        languageSelect.Width = 120;
        languageSelect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        languageSelect.Location = new Point(1110, 11);
        languageSelect.SelectedIndexChanged += (_, _) =>
        {
            if (suppressUiEvents) return;
            language = languageSelect.SelectedIndex == 1 ? "de" : "en";
            ApplyLanguage();
            SaveSettings();
        };
        header.Controls.Add(languageSelect);
        header.Resize += (_, _) =>
        {
            languageSelect.Left = header.ClientSize.Width - languageSelect.Width;
            headerStatus.Left = languageSelect.Left - headerStatus.Width - 24;
        };

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Canvas, ColumnCount = 2, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(body, 0, 1);

        var rail = new Panel { Dock = DockStyle.Fill, BackColor = Canvas, Padding = new Padding(0, 14, 14, 0) };
        body.Controls.Add(rail, 0, 0);
        var railRule = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Border };
        rail.Controls.Add(railRule);
        var navFlow = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Canvas };
        rail.Controls.Add(navFlow);
        AddNav(navFlow, "nav.live", "01", () => ShowPage(0));
        AddNav(navFlow, "nav.setup", "02", () => ShowPage(1));
        AddNav(navFlow, "nav.inputs", "03", () => ShowPage(2));
        AddNav(navFlow, "nav.dev", "04", () => ShowPage(3));
        var footer = new Label { Text = "V1.0.1  /  WINDOWS X64", ForeColor = Muted, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 0, 0, 12) };
        rail.Controls.Add(footer);

        pageHost.Dock = DockStyle.Fill;
        pageHost.BackColor = Canvas;
        pageHost.Padding = new Padding(14, 8, 0, 0);
        body.Controls.Add(pageHost, 1, 0);

        BuildMonitorPage();
        BuildSetupPage();
        BuildInputsPage();
        BuildDiagnosticsPage();
        ShowPage(0);
    }

    private readonly List<Button> navButtons = [];
    private readonly List<Panel> pages = [];

    private void AddNav(Control parent, string key, string number, Action action)
    {
        var b = new Button
        {
            Width = 164, Height = 46, Margin = new Padding(0, 0, 0, 7), FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0),
            BackColor = Surface, ForeColor = Muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        Bind(b, key);
        b.Text = number + "     " + T(key);
        b.Tag = (number, key);
        b.Click += (_, _) => action();
        parent.Controls.Add(b);
        navButtons.Add(b);
    }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Count) return;
        foreach (var p in pages) p.Visible = false;
        pages[index].Visible = true;
        for (int i = 0; i < navButtons.Count; i++)
        {
            bool active = i == index;
            navButtons[i].BackColor = active ? Raised : Surface;
            navButtons[i].ForeColor = active ? Teal : Muted;
            if (navButtons[i].Tag is ValueTuple<string, string> tag)
                navButtons[i].Text = tag.Item1 + "     " + T(tag.Item2);
        }
    }

    private Panel NewPage()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Canvas, Visible = false };
        pageHost.Controls.Add(page);
        pages.Add(page);
        return page;
    }

    private void BuildMonitorPage()
    {
        var page = NewPage();
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Canvas, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 0, 0, 4) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        page.Controls.Add(grid);

        var cameraCard = NewCard();
        cameraCard.Dock = DockStyle.Fill;
        cameraCard.Margin = new Padding(0, 0, 10, 0);
        grid.Controls.Add(cameraCard, 0, 0);
        var cameraHead = Bind(new Label { AutoSize = true, Location = new Point(16, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Muted }, "monitor.live");
        cameraCard.Controls.Add(cameraHead);
        cameraStatus.AutoSize = true;
        cameraStatus.ForeColor = Amber;
        cameraStatus.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        cameraStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        cameraStatus.Location = new Point(660, 13);
        cameraCard.Controls.Add(cameraStatus);
        preview.Dock = DockStyle.None;
        preview.SizeMode = PictureBoxSizeMode.Zoom;
        preview.BackColor = Color.FromArgb(9, 11, 13);
        preview.SetBounds(14, 42, 620, 420);
        preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        preview.Cursor = Cursors.Cross;
        preview.Click += PreviewClick;
        cameraCard.Controls.Add(preview);
        cameraCard.Resize += (_, _) =>
        {
            preview.SetBounds(14, 42, Math.Max(80, cameraCard.ClientSize.Width - 28), Math.Max(80, cameraCard.ClientSize.Height - 56));
            cameraStatus.Left = Math.Max(250, cameraCard.ClientSize.Width - cameraStatus.Width - 14);
        };

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Canvas, ColumnCount = 1, RowCount = 5 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 146));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        grid.Controls.Add(right, 1, 0);

        var currentCard = NewCard();
        currentCard.Dock = DockStyle.Fill;
        currentCard.Margin = new Padding(0, 0, 0, 10);
        right.Controls.Add(currentCard, 0, 0);
        currentCard.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 13), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Muted }, "monitor.current"));
        currentGearLabel.Text = "—";
        currentGearLabel.Location = new Point(16, 32);
        currentGearLabel.AutoSize = true;
        currentGearLabel.Font = new Font("Segoe UI", 40F, FontStyle.Bold);
        currentGearLabel.ForeColor = Teal;
        currentCard.Controls.Add(currentGearLabel);
        trackerStatus.AutoSize = true;
        trackerStatus.Location = new Point(18, 100);
        trackerStatus.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        trackerStatus.ForeColor = Muted;
        currentCard.Controls.Add(trackerStatus);

        var patternCard = NewCard();
        patternCard.Dock = DockStyle.Fill;
        patternCard.Margin = new Padding(0, 0, 0, 10);
        right.Controls.Add(patternCard, 0, 1);
        patternCard.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(14, 12), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Muted }, "monitor.pattern"));
        patternView = new GearPatternView { BackColor = Card };
        patternView.SetBounds(8, 34, 300, 300);
        patternView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        patternCard.Controls.Add(patternView);
        patternCard.Resize += (_, _) => patternView.SetBounds(8, 34, Math.Max(120, patternCard.ClientSize.Width - 16), Math.Max(140, patternCard.ClientSize.Height - 42));

        var detailCard = NewCard();
        detailCard.Dock = DockStyle.Fill;
        detailCard.Margin = new Padding(0, 0, 0, 8);
        right.Controls.Add(detailCard, 0, 2);
        detailCard.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(13, 10), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Muted }, "monitor.alignment"));
        alignmentValue.Text = "—";
        alignmentValue.Location = new Point(13, 31);
        alignmentValue.Size = new Size(320, 39);
        alignmentValue.ForeColor = Ink;
        alignmentValue.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        detailCard.Controls.Add(alignmentValue);

        armButton.Dock = DockStyle.Fill;
        armButton.Margin = new Padding(0, 0, 0, 6);
        StyleButton(armButton, primary: true);
        armButton.Click += (_, _) => ToggleArmed();
        right.Controls.Add(armButton, 0, 3);
        var setupButton = MakeButton("monitor.openSetup", () => ShowPage(1), primary: false);
        setupButton.Dock = DockStyle.Fill;
        setupButton.Margin = new Padding(0);
        right.Controls.Add(setupButton, 0, 4);
    }

    private void BuildSetupPage()
    {
        var page = NewPage();
        var scroll = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Canvas, Padding = new Padding(0, 0, 10, 14) };
        page.Controls.Add(scroll);
        var title = Bind(new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold), ForeColor = Ink, Margin = new Padding(0, 3, 0, 5) }, "setup.title");
        scroll.Controls.Add(title);

        var camera = NewCard(); camera.Size = new Size(830, 112); camera.Margin = new Padding(0, 8, 0, 0);
        scroll.Controls.Add(camera);
        camera.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "camera.heading"));
        camera.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 36), Font = new Font("Segoe UI", 8F), ForeColor = Muted }, "camera.desc"));
        StyleCombo(cameraSelect); cameraSelect.DropDownStyle = ComboBoxStyle.DropDownList; cameraSelect.SetBounds(16, 66, 300, 28); camera.Controls.Add(cameraSelect);
        var scan = MakeButton("camera.scan", () => { _ = ScanCamerasAsync(true); }, false); scan.SetBounds(326, 64, 132, 32); camera.Controls.Add(scan);
        var connect = MakeButton("camera.connect", ConnectCamera, true); connect.SetBounds(468, 64, 116, 32); camera.Controls.Add(connect);

        var settings = NewCard(); settings.Size = new Size(830, 220); settings.Margin = new Padding(0, 10, 0, 0);
        scroll.Controls.Add(settings);
        settings.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "setup.orientation"));
        AddComboField(settings, "setup.view", viewSelect, 16, 47, 230, ["setup.top", "setup.bottom"]);
        AddComboField(settings, "setup.rotation", rotationSelect, 258, 47, 240, ["setup.rotate0", "setup.rotate90", "setup.rotate180", "setup.rotate270"]);
        AddComboField(settings, "setup.tracking", trackingSelect, 16, 113, 340, ["setup.markerMode", "setup.imageMode"]);
        AddComboField(settings, "setup.engine", alignmentSelect, 368, 113, 430, ["setup.legacy", "setup.adaptive"]);
        mirrorH.AutoSize = true; mirrorH.BackColor = Card; mirrorH.ForeColor = Ink; mirrorH.SetBounds(16, 178, 190, 24); Bind(mirrorH, "setup.mirrorH"); settings.Controls.Add(mirrorH);
        mirrorV.AutoSize = true; mirrorV.BackColor = Card; mirrorV.ForeColor = Ink; mirrorV.SetBounds(218, 178, 190, 24); Bind(mirrorV, "setup.mirrorV"); settings.Controls.Add(mirrorV);
        AddComboField(settings, "setup.version", versionSelect, 500, 166, 300, ["setup.v1", "setup.v2"]);

        viewSelect.SelectedIndex = selectedViewIndex;
        rotationSelect.SelectedIndex = selectedRotation;
        trackingSelect.SelectedIndex = selectedTrackingIndex;
        alignmentSelect.SelectedIndex = adaptiveAlignment ? 1 : 0;
        mirrorH.Checked = selectedMirrorH;
        mirrorV.Checked = selectedMirrorV;
        versionSelect.SelectedIndex = selectedVersion2 ? 1 : 0;
        viewSelect.SelectedIndexChanged += (_, _) => { if (suppressUiEvents) return; selectedViewIndex = viewSelect.SelectedIndex; InvalidateCalibration("Camera perspective changed", "Kameraperspektive geändert"); };
        rotationSelect.SelectedIndexChanged += (_, _) => { if (suppressUiEvents) return; selectedRotation = Math.Max(0, rotationSelect.SelectedIndex); InvalidateCalibration("Image rotation changed", "Bilddrehung geändert"); };
        trackingSelect.SelectedIndexChanged += (_, _) => { if (suppressUiEvents) return; selectedTrackingIndex = Math.Max(0, trackingSelect.SelectedIndex); InvalidateCalibration("Recognition method changed", "Erkennungsmethode geändert"); RefreshSetupStatus(); };
        alignmentSelect.SelectedIndexChanged += (_, _) =>
        {
            if (suppressUiEvents) return;
            adaptiveAlignment = alignmentSelect.SelectedIndex == 1;
            armed = false; ReleaseOutputs();
            SetStatus("status.paused", false); SaveSettings(); RefreshSetupStatus();
            Log(adaptiveAlignment ? "Adaptive camera alignment selected." : "Legacy fixed-camera tracking selected.");
        };
        mirrorH.CheckedChanged += (_, _) => { if (suppressUiEvents) return; selectedMirrorH = mirrorH.Checked; InvalidateCalibration("Horizontal mirror changed", "Horizontale Spiegelung geändert"); };
        mirrorV.CheckedChanged += (_, _) => { if (suppressUiEvents) return; selectedMirrorV = mirrorV.Checked; InvalidateCalibration("Vertical mirror changed", "Vertikale Spiegelung geändert"); };
        versionSelect.SelectedIndexChanged += (_, _) => ChangeProtocol();

        var calibration = NewCard(); calibration.Size = new Size(830, 232); calibration.Margin = new Padding(0, 10, 0, 0);
        scroll.Controls.Add(calibration);
        calibration.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "setup.calibration"));
        calibration.Controls.Add(Bind(new Label { AutoSize = false, Size = new Size(790, 40), Location = new Point(15, 39), Font = new Font("Segoe UI", 8F), ForeColor = Muted }, "setup.calibrationDesc"));
        calibrationProgress.AutoSize = true; calibrationProgress.Location = new Point(15, 84); calibrationProgress.ForeColor = Teal; calibrationProgress.Font = new Font("Segoe UI", 8F, FontStyle.Bold); calibration.Controls.Add(calibrationProgress);
        calibrationButtons.Location = new Point(12, 112); calibrationButtons.Size = new Size(790, 70); calibrationButtons.FlowDirection = FlowDirection.LeftToRight; calibrationButtons.WrapContents = true; calibrationButtons.BackColor = Card; calibration.Controls.Add(calibrationButtons);
        var clear = MakeButton("setup.clear", ClearCalibration, false); clear.SetBounds(15, 187, 160, 31); calibration.Controls.Add(clear);
        markerModeHint.AutoSize = true; markerModeHint.ForeColor = Muted; markerModeHint.Font = new Font("Segoe UI", 8F); markerModeHint.Location = new Point(190, 195); calibration.Controls.Add(markerModeHint);
        BuildCalibrationButtons();
    }

    private void BuildInputsPage()
    {
        var page = NewPage();
        var scroll = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Canvas, Padding = new Padding(0, 0, 10, 14) };
        page.Controls.Add(scroll);
        scroll.Controls.Add(Bind(new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold), ForeColor = Ink, Margin = new Padding(0, 3, 0, 5) }, "inputs.title"));

        var output = NewCard(); output.Size = new Size(830, 184); output.Margin = new Padding(0, 8, 0, 0); scroll.Controls.Add(output);
        output.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "inputs.output"));
        StyleCombo(outputSelect); outputSelect.DropDownStyle = ComboBoxStyle.DropDownList; outputSelect.SetBounds(16, 42, 430, 30); output.Controls.Add(outputSelect);
        startOutputButton.SetBounds(462, 41, 160, 32); StyleButton(startOutputButton, primary: true);
        startOutputButton.Click += async (_, _) =>
        {
            if (OutputRunning) await StopOutputAsync();
            else await StartOutputAsync();
        };
        output.Controls.Add(startOutputButton);
        outputStatus.AutoSize = true; outputStatus.Location = new Point(16, 83); outputStatus.ForeColor = Teal; outputStatus.Font = new Font("Segoe UI", 8F, FontStyle.Bold); output.Controls.Add(outputStatus);
        output.Controls.Add(Bind(new Label { AutoSize = false, Size = new Size(790, 54), Location = new Point(16, 107), Font = new Font("Segoe UI", 8F), ForeColor = Muted }, "inputs.driverHelp"));
        outputSelect.SelectedIndex = selectedOutput switch { "vjoy" => 1, "xbox" => 2, _ => 0 };
        outputSelect.SelectedIndexChanged += (_, _) =>
        {
            if (suppressUiEvents) return;
            selectedOutput = outputSelect.SelectedIndex switch { 1 => "vjoy", 2 => "xbox", _ => "keyboard" };
            armed = false; ReleaseOutputs(); outputReady = false;
            vJoyOutput?.Dispose(); vJoyOutput = null; gamepadOutput?.Dispose(); gamepadOutput = null;
            SaveSettings(); RefreshOutputUi(); SetStatus("status.paused", false);
            Log($"Output mode selected: {T(OutputModeKey())}.");
        };

        keyMapPanel.Size = new Size(830, 236); keyMapPanel.BackColor = Card; keyMapPanel.Margin = new Padding(0, 10, 0, 0); scroll.Controls.Add(keyMapPanel);
        keyMapPanel.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "inputs.keyMap"));
        keyMapPanel.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 36), Font = new Font("Segoe UI", 8F), ForeColor = Muted }, "inputs.capture"));
        for (int i = 0; i < AllStates.Length; i++)
        {
            string state = AllStates[i];
            int x = 16 + (i % 3) * 258, y = 72 + (i / 3) * 47;
            var lbl = new Label { Text = StateName(state), ForeColor = Ink, Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Location = new Point(x, y), Size = new Size(54, 28) };
            var box = new TextBox { Text = keyAssignments[state], Location = new Point(x + 58, y), Size = new Size(125, 28), ReadOnly = true, BackColor = Raised, ForeColor = Teal, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center, Cursor = Cursors.Hand, TabStop = true };
            box.PreviewKeyDown += (_, e) => e.IsInputKey = true;
            box.Click += (_, _) => BeginKeyCapture(state, box);
            box.KeyDown += (_, e) => CaptureKey(state, box, e);
            keyMapPanel.Controls.Add(lbl); keyMapPanel.Controls.Add(box); keyCaptions[state] = lbl; keyBoxes[state] = box;
        }
        keyCaptureHint.AutoSize = true; keyCaptureHint.ForeColor = Muted; keyCaptureHint.Location = new Point(16, 207); keyMapPanel.Controls.Add(keyCaptureHint);

        padMapPanel.Size = new Size(830, 276); padMapPanel.BackColor = Card; padMapPanel.Margin = new Padding(0, 10, 0, 0); scroll.Controls.Add(padMapPanel);
        padMapPanel.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "inputs.padMap"));
        padMapPanel.Controls.Add(Bind(new Label { AutoSize = false, Size = new Size(790, 38), Location = new Point(15, 36), Font = new Font("Segoe UI", 8F), ForeColor = Muted }, "inputs.padHelp"));
        for (int i = 0; i < AllStates.Length; i++)
        {
            string state = AllStates[i];
            int x = 16 + (i % 3) * 258, y = 84 + (i / 3) * 48;
            var lbl = new Label { Text = StateName(state), ForeColor = Ink, Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Location = new Point(x, y), Size = new Size(54, 28) };
            var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x + 58, y), Size = new Size(145, 28), BackColor = Raised, ForeColor = Ink, FlatStyle = FlatStyle.Flat };
            padMapPanel.Controls.Add(lbl); padMapPanel.Controls.Add(combo); padBoxes[state] = combo;
            foreach (var choice in PadChoices) combo.Items.Add(new PadChoice(choice.Id, choice.Short, choice.Button, choice.Hat));
            int selectedPad = Array.FindIndex(PadChoices, p => p.Id == padAssignments[state]);
            if (combo.Items.Count > 0) combo.SelectedIndex = Math.Clamp(selectedPad, 0, combo.Items.Count - 1);
            combo.SelectedIndexChanged += (_, _) =>
            {
                if (combo.SelectedItem is PadChoice choice)
                {
                    padAssignments[state] = choice.Id;
                    SaveSettings();
                }
            };
        }

        var options = NewCard(); options.Size = new Size(830, 104); options.Margin = new Padding(0, 10, 0, 0); scroll.Controls.Add(options);
        options.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "inputs.double"));
        doublePressCheck.AutoSize = true; doublePressCheck.BackColor = Card; doublePressCheck.ForeColor = Ink; doublePressCheck.Location = new Point(16, 44); doublePressCheck.Checked = doublePressEnabled; options.Controls.Add(doublePressCheck);
        options.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(500, 45), ForeColor = Muted }, "inputs.gap"));
        doublePressGap.Minimum = 20; doublePressGap.Maximum = 500; doublePressGap.Increment = 10; doublePressGap.Value = Math.Clamp(doublePressGapMs, 20, 500); doublePressGap.SetBounds(610, 40, 82, 28); doublePressGap.BackColor = Raised; doublePressGap.ForeColor = Ink; options.Controls.Add(doublePressGap);
        doublePressCheck.CheckedChanged += (_, _) => { if (suppressUiEvents) return; doublePressEnabled = doublePressCheck.Checked; SaveSettings(); };
        doublePressGap.ValueChanged += (_, _) => { doublePressGapMs = (int)doublePressGap.Value; SaveSettings(); };
        var vjoyInfo = Bind(new Label { AutoSize = false, Size = new Size(790, 34), Location = new Point(16, 70), ForeColor = Amber, Font = new Font("Segoe UI", 8F) }, "inputs.vjoyHelp");
        options.Controls.Add(vjoyInfo);
    }

    private void BuildDiagnosticsPage()
    {
        var page = NewPage();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Canvas, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(root);
        var head = new Panel { Dock = DockStyle.Fill, BackColor = Canvas };
        head.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(0, 0), Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold), ForeColor = Ink }, "diag.title"));
        head.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(2, 38), Font = new Font("Segoe UI", 8F), ForeColor = Muted }, "diag.desc"));
        root.Controls.Add(head, 0, 0);

        var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Canvas, ColumnCount = 4, RowCount = 1 };
        for (int i = 0; i < 4; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        root.Controls.Add(metrics, 0, 1);
        AddMetric(metrics, 0, "diag.alignment", alignmentValue, 120);
        AddMetric(metrics, 1, "diag.features", featureValue, 120);
        AddMetric(metrics, 2, "diag.candidate", candidateValue, 120);
        AddMetric(metrics, 3, "diag.active", activeValue, 120);

        var logCard = NewCard(); logCard.Dock = DockStyle.Fill; logCard.Margin = new Padding(0, 8, 0, 0); root.Controls.Add(logCard, 0, 2);
        logCard.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(15, 11), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Ink }, "diag.log"));
        var clear = MakeButton("diag.clear", () => logBox.Clear(), false); clear.SetBounds(690, 7, 106, 29); logCard.Controls.Add(clear);
        var copy = MakeButton("diag.copy", () => { if (!string.IsNullOrWhiteSpace(logBox.Text)) Clipboard.SetText(logBox.Text); }, false); copy.SetBounds(574, 7, 106, 29); logCard.Controls.Add(copy);
        logBox.Multiline = true; logBox.ReadOnly = true; logBox.ScrollBars = ScrollBars.Vertical; logBox.BackColor = Color.FromArgb(16, 18, 21); logBox.ForeColor = Color.FromArgb(196, 206, 212); logBox.Font = new Font("Cascadia Mono", 9F); logBox.BorderStyle = BorderStyle.None; logBox.Dock = DockStyle.Fill; logBox.Margin = new Padding(14, 43, 14, 14);
        logCard.Controls.Add(logBox);
    }

    private void AddMetric(TableLayoutPanel parent, int column, string key, Label value, int height)
    {
        var card = NewCard(); card.Dock = DockStyle.Fill; card.Margin = new Padding(0, 0, 8, 0); parent.Controls.Add(card, column, 0);
        card.Controls.Add(Bind(new Label { AutoSize = true, Location = new Point(12, 12), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Muted }, key));
        value.Location = new Point(12, 36); value.Size = new Size(205, 68); value.Font = new Font("Segoe UI", 9F, FontStyle.Bold); value.ForeColor = Ink;
        card.Controls.Add(value);
    }

    private void AddComboField(Control parent, string labelKey, ComboBox combo, int x, int y, int width, string[] optionKeys)
    {
        var label = Bind(new Label { AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Muted }, labelKey);
        parent.Controls.Add(label);
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        StyleCombo(combo);
        combo.SetBounds(x, y + 20, width, 28);
        combo.Tag = optionKeys;
        PopulateCombo(combo, optionKeys, 0);
        parent.Controls.Add(combo);
    }

    private static Panel NewCard() => new() { BackColor = Card, Padding = new Padding(0), Margin = new Padding(0) };

    private Button MakeButton(string key, Action action, bool primary)
    {
        var button = new Button { Height = 34, Width = 130, Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Margin = new Padding(0, 0, 8, 0) };
        Bind(button, key);
        StyleButton(button, primary);
        button.Click += (_, _) => action();
        return button;
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        button.BackColor = primary ? Teal : Raised;
        button.ForeColor = primary ? Color.FromArgb(15, 24, 26) : Ink;
        button.FlatStyle = FlatStyle.Flat;
        button.Cursor = Cursors.Hand;
    }

    private static void StyleCombo(ComboBox combo)
    {
        combo.BackColor = Raised;
        combo.ForeColor = Ink;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = new Font("Segoe UI", 8.5F);
    }

    private void PopulateCombo(ComboBox combo, string[] keys, int selected)
    {
        bool wasSuppressing = suppressUiEvents;
        suppressUiEvents = true;
        combo.Items.Clear();
        foreach (string key in keys) combo.Items.Add(T(key));
        combo.SelectedIndex = Math.Clamp(selected, 0, Math.Max(0, combo.Items.Count - 1));
        suppressUiEvents = wasSuppressing;
    }

    private void ApplyLanguage()
    {
        suppressUiEvents = true;
        foreach (var pair in localizedControls)
            if (!pair.Key.IsDisposed) pair.Key.Text = T(pair.Value);
        foreach (var button in navButtons)
            if (button.Tag is ValueTuple<string, string> tag) button.Text = tag.Item1 + "     " + T(tag.Item2);
        if (viewSelect.Tag is string[] viewKeys) PopulateCombo(viewSelect, viewKeys, selectedViewIndex);
        if (rotationSelect.Tag is string[] rotationKeys) PopulateCombo(rotationSelect, rotationKeys, selectedRotation);
        if (trackingSelect.Tag is string[] trackingKeys) PopulateCombo(trackingSelect, trackingKeys, selectedTrackingIndex);
        if (alignmentSelect.Tag is string[] engineKeys) PopulateCombo(alignmentSelect, engineKeys, adaptiveAlignment ? 1 : 0);
        if (versionSelect.Tag is string[] versionKeys) PopulateCombo(versionSelect, versionKeys, selectedVersion2 ? 1 : 0);
        PopulateOutputCombo();
        languageSelect.SelectedIndex = language == "de" ? 1 : 0;
        suppressUiEvents = false;
        RefreshVersionUi();
        RefreshOutputUi();
        RefreshSetupStatus();
        SetStatus(statusKey ?? "status.ready", statusSuccess);
        UpdateDynamicLabels();
    }

    private void PopulateOutputCombo()
    {
        int selected = selectedOutput switch { "vjoy" => 1, "xbox" => 2, _ => 0 };
        bool wasSuppressing = suppressUiEvents;
        suppressUiEvents = true;
        outputSelect.Items.Clear();
        outputSelect.Items.Add(T("inputs.keyboard"));
        outputSelect.Items.Add(T("inputs.vjoy"));
        outputSelect.Items.Add(T("inputs.virtual"));
        outputSelect.SelectedIndex = selected;
        suppressUiEvents = wasSuppressing;
    }

    private void RefreshVersionUi()
    {
        foreach (string state in AllStates)
        {
            bool visible = selectedVersion2 || state is not ("NL" or "NR");
            if (keyBoxes.TryGetValue(state, out var keyBox)) keyBox.Visible = visible;
            if (keyCaptions.TryGetValue(state, out var caption)) caption.Visible = visible;
            if (padBoxes.TryGetValue(state, out var padBox)) padBox.Visible = visible;
        }
        BuildCalibrationButtons();
        UpdateDynamicLabels();
        RefreshOutputUi();
        RefreshSetupStatus();
    }

    private void RefreshOutputUi()
    {
        bool keyboard = selectedOutput == "keyboard";
        bool virtualPad = selectedOutput == "xbox";
        keyMapPanel.Visible = keyboard;
        padMapPanel.Visible = virtualPad;
        doublePressCheck.Enabled = keyboard && !selectedVersion2;
        doublePressGap.Enabled = keyboard && !selectedVersion2 && doublePressEnabled;
        suppressUiEvents = true;
        doublePressCheck.Checked = doublePressEnabled;
        suppressUiEvents = false;
        startOutputButton.Text = OutputRunning ? T("inputs.stop") : T("inputs.start");
        outputStatus.Text = OutputRunning ? T("inputs.ready") : T("inputs.paused");
        outputStatus.ForeColor = OutputRunning ? Teal : Amber;
        if (selectedOutput == "xbox" && OutputRunning) outputStatus.Text = T("status.driverReady");
        if (selectedOutput == "vjoy" && vJoyOutput is not null) outputStatus.Text = T("status.vjoyReady");
        if (armButton.IsHandleCreated) armButton.Text = armed ? T("monitor.pause") : T("monitor.arm");
    }

    private bool OutputRunning => outputReady && (selectedOutput == "keyboard" ||
        (selectedOutput == "vjoy" && vJoyOutput is not null) ||
        (selectedOutput == "xbox" && gamepadOutput is not null));

    private void RefreshSetupStatus()
    {
        markerModeHint.Text = selectedTrackingIndex == 0 ? T("setup.markerHint") : string.Empty;
        int learned = CurrentLearnedStates().Count;
        calibrationProgress.Text = $"{learned}/{ActiveStates.Length} {T("setup.progress")}";
    }

    private void UpdateDynamicLabels()
    {
        if (trackerStatus.IsHandleCreated)
            trackerStatus.Text = candidateGear is null ? T("monitor.notDetected") :
                (selectedTrackingIndex == 0 ? T("monitor.marker") : T("monitor.image")) + " · " + StateName(candidateGear);
        if (currentGearLabel.IsHandleCreated) currentGearLabel.Text = activeGear is null ? "—" : StateName(activeGear);
        if (patternView is not null) { patternView.SetActive(activeGear); patternView.Invalidate(); }
        if (armButton.IsHandleCreated) armButton.Text = armed ? T("monitor.pause") : T("monitor.arm");
        if (cameraStatus.IsHandleCreated) cameraStatus.Text = capture is not null && capture.IsOpened() ? T("camera.ready") : T("camera.none");
    }

    private void ChangeProtocol()
    {
        if (suppressUiEvents) return;
        selectedVersion2 = versionSelect.SelectedIndex == 1;
        frameTimer.Interval = selectedVersion2 ? 16 : 33;
        armed = false; ReleaseOutputs();
        activeGear = candidateGear = lastConfirmedGear = null;
        selectedCandidate = "—"; v2NeutralSent.Clear(); v2OutputQueue.Clear();
        ApplyCameraProfile(); SaveSettings(); RefreshVersionUi(); SetStatus("status.paused", false);
        Log(selectedVersion2 ? "V2 protocol selected · teach N-L, N and N-R." : "V1 protocol selected · direct gear output.");
    }

    private void BuildCalibrationButtons()
    {
        calibrationButtons.SuspendLayout();
        calibrationButtons.Controls.Clear();
        foreach (string state in ActiveStates)
        {
            bool learned = CurrentLearnedStates().Contains(state);
            var button = new Button
            {
                Text = T("setup.learn") + " " + StateName(state), Width = 122, Height = 28,
                Margin = new Padding(4), BackColor = learned ? Color.FromArgb(37, 69, 68) : Raised,
                ForeColor = learned ? Teal : Ink, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = learned ? Teal : Border;
            button.Click += (_, _) => LearnState(state);
            calibrationButtons.Controls.Add(button);
        }
        calibrationButtons.ResumeLayout();
    }

    private string[] ActiveStates => selectedVersion2 ? V2States : V1States;
    private bool UsesImageMatching => selectedTrackingIndex == 1;
    private bool UsesVJoy => selectedOutput == "vjoy";
    private bool UsesVirtualGamepad => selectedOutput == "xbox";

    private static string StateName(string state) => state switch { "NL" => "N-L", "NR" => "N-R", _ => state };
    private static bool IsNeutralLane(string? state) => state is "NL" or "N" or "NR";
    private static bool IsGear(string? state) => state is "1" or "2" or "3" or "4" or "5" or "R";
    private static int Lane(string state) => state switch { "1" or "2" or "NL" => 0, "5" or "R" or "NR" => 2, _ => 1 };
    private static string NeutralState(int lane) => lane switch { 0 => "NL", 2 => "NR", _ => "N" };

    private string[] BuildNeutralRoute(string target)
    {
        int destination = Lane(target);
        if (lastConfirmedGear is null) return [NeutralState(destination)];
        int source = Lane(lastConfirmedGear);
        if (source == destination) return [NeutralState(source)];
        int step = source < destination ? 1 : -1;
        var route = new List<string>();
        for (int lane = source; ; lane += step)
        {
            route.Add(NeutralState(lane));
            if (lane == destination) break;
        }
        return route.ToArray();
    }

    private async Task ScanCamerasAsync(bool report)
    {
        if (cameraScanTask is not null)
        {
            await cameraScanTask;
            return;
        }
        Task task = ScanCamerasCoreAsync(report);
        cameraScanTask = task;
        try { await task; }
        finally { if (ReferenceEquals(cameraScanTask, task)) cameraScanTask = null; }
    }

    private async Task ScanCamerasCoreAsync(bool report)
    {
        int previous = selectedCamera;
        if (IsDisposed || cameraSelect.IsDisposed) return;
        cameraSelect.Enabled = false;
        cameraStatus.Text = language == "de" ? "Kameras werden gesucht…" : "Searching for cameras…";
        try
        {
            int[] indices = await ProbeCameraIndicesAsync();
            if (IsDisposed || cameraSelect.IsDisposed) return;
            cameraSelect.BeginUpdate();
            try
            {
                cameraSelect.Items.Clear();
                foreach (int index in indices) cameraSelect.Items.Add(new CameraItem(index, CameraLabel(index)));
                int choice = -1;
                for (int i = 0; i < cameraSelect.Items.Count; i++)
                    if (cameraSelect.Items[i] is CameraItem item && item.Index == previous) choice = i;
                cameraSelect.SelectedIndex = choice >= 0 ? choice : (cameraSelect.Items.Count > 0 ? 0 : -1);
                if (cameraSelect.SelectedItem is CameraItem selected) selectedCamera = selected.Index;
            }
            finally { cameraSelect.EndUpdate(); }

            if (cameraSelect.Items.Count == 0)
            {
                SetStatus("status.cameraMissing", false);
                cameraStatus.Text = T("camera.none");
                if (report) Log(T("log.noCamera"));
            }
            else
            {
                if (capture is null || !capture.IsOpened()) SetStatus("status.ready", true);
                cameraStatus.Text = string.Format(language == "de" ? "{0} Kamera(s) gefunden" : "{0} camera(s) found", cameraSelect.Items.Count);
                if (report) Log(string.Format(T("log.scan"), cameraSelect.Items.Count));
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                SetStatus("status.cameraMissing", false);
                cameraStatus.Text = T("camera.error");
                if (report) Log(string.Format(T("log.cameraError"), previous) + " " + ex.Message);
            }
        }
        finally
        {
            if (!IsDisposed && !cameraSelect.IsDisposed) cameraSelect.Enabled = true;
        }
    }

    private static Task<int[]> ProbeCameraIndicesAsync()
    {
        var completion = new TaskCompletionSource<int[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var found = new List<int>();
                for (int index = 0; index < 10; index++)
                {
                    try
                    {
                        using var test = new VideoCapture(index, VideoCaptureAPIs.DSHOW);
                        if (!test.IsOpened()) continue;
                        using var frame = new Mat();
                        if (test.Read(frame) && !frame.Empty()) found.Add(index);
                    }
                    catch { }
                }
                completion.TrySetResult(found.ToArray());
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        }) { IsBackground = true, Name = "ShifterTracker camera scan" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private string CameraLabel(int index) => (language == "de" ? "Kamera " : "Camera ") + index;

    private async void ConnectCamera()
    {
        if (cameraSelect.SelectedItem is not CameraItem item)
        {
            await ScanCamerasAsync(true);
            if (cameraSelect.SelectedItem is not CameraItem found) return;
            item = found;
        }
        if (capture is not null && capture.IsOpened() && selectedCamera != item.Index)
            InvalidateCalibration("Camera source changed", "Kameraquelle geändert");
        capture?.Release(); capture?.Dispose(); capture = null;
        try
        {
            capture = new VideoCapture(item.Index, VideoCaptureAPIs.DSHOW);
            if (!capture.IsOpened()) throw new InvalidOperationException("DirectShow could not open this camera.");
            selectedCamera = item.Index;
            ApplyCameraProfile(); SaveSettings();
            SetStatus("status.cameraConnected", true);
            cameraStatus.Text = T("camera.ready");
            Log(string.Format(T("log.cameraConnected"), item.Index));
        }
        catch (Exception ex)
        {
            capture?.Dispose(); capture = null;
            SetStatus("status.cameraMissing", false);
            cameraStatus.Text = T("camera.error");
            Log(string.Format(T("log.cameraError"), item.Index) + " " + ex.Message);
        }
    }

    private void ApplyCameraProfile()
    {
        if (capture is null || !capture.IsOpened()) return;
        capture.Set(VideoCaptureProperties.FrameWidth, 640);
        capture.Set(VideoCaptureProperties.FrameHeight, 480);
        capture.Set(VideoCaptureProperties.Fps, selectedVersion2 ? 60 : 30);
        capture.Set(VideoCaptureProperties.BufferSize, 1);
    }

    private void PreviewClick(object? sender, EventArgs e)
    {
        if (lastFrame is null || preview.Image is null || selectedTrackingIndex != 0) return;
        var mouse = preview.PointToClient(Cursor.Position);
        double scale = Math.Min((double)preview.ClientSize.Width / lastFrame.Width, (double)preview.ClientSize.Height / lastFrame.Height);
        double dx = (preview.ClientSize.Width - lastFrame.Width * scale) / 2;
        double dy = (preview.ClientSize.Height - lastFrame.Height * scale) / 2;
        int x = (int)((mouse.X - dx) / scale), y = (int)((mouse.Y - dy) / scale);
        if (x < 0 || y < 0 || x >= lastFrame.Width || y >= lastFrame.Height) return;
        using var hsv = new Mat();
        Cv2.CvtColor(lastFrame, hsv, ColorConversionCodes.BGR2HSV);
        var px = hsv.At<Vec3b>(y, x);
        markerColor = (px.Item0, px.Item1, px.Item2);
        Log($"Marker color learned · HSV {markerColor.Value.H}/{markerColor.Value.S}/{markerColor.Value.V}.");
        SaveSettings();
    }

    private void StartCalibration()
    {
        if (lastFrame is null)
        {
            MessageBox.Show(this, language == "de" ? "Verbinde zuerst die Kamera." : "Connect a camera first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!UsesImageMatching && markerColor is null)
        {
            MessageBox.Show(this, T("setup.markerHint"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (adaptiveAlignment && !motionCompensator.IsReady)
        {
            bool hasOldCalibration = positions.Count > 0 || imageTemplates.Count > 0;
            if (hasOldCalibration)
            {
                var choice = MessageBox.Show(this,
                    language == "de"
                        ? "Für die adaptive Ausrichtung wird jetzt eine neue Kamerareferenz aufgenommen. Die vorhandenen Einlerndaten werden gelöscht und müssen neu aufgenommen werden. Fortfahren?"
                        : "A new adaptive camera reference will be captured. Existing learned positions will be cleared and must be captured again. Continue?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (choice != DialogResult.Yes) return;
            }
            if (!motionCompensator.SetReference(lastFrame, out string message))
            {
                MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Log(message);
                return;
            }
            alignmentAnchorBase64 = Convert.ToBase64String(lastFrame.ToBytes(".png"));
            ClearLearnedPositions();
            Log(string.Format(T("log.anchor"), motionCompensator.AnchorFeatureCount));
            SaveSettings();
        }

        var dialog = new Form
        {
            Text = language == "de" ? $"V{(selectedVersion2 ? 2 : 1)} · Positionen einlernen" : $"V{(selectedVersion2 ? 2 : 1)} · Teach positions",
            BackColor = Canvas, ForeColor = Ink, Size = new Size(470, 365), StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
        };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Canvas, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        dialog.Controls.Add(flow);
        var note = new Label
        {
            Text = selectedVersion2
                ? (language == "de" ? "Stelle den Hebel in jede Zone. V2 erfasst auch die kurzen Neutralgassen." : "Move the lever into each zone. V2 also captures the brief neutral gates.")
                : (language == "de" ? "Stelle den Hebel in jede Position und lerne sie einzeln ein." : "Place the lever in each position and capture it individually."),
            ForeColor = Muted, Size = new Size(420, 50), Margin = new Padding(0, 0, 0, 12)
        };
        flow.Controls.Add(note);
        foreach (string state in ActiveStates)
        {
            var button = new Button { Text = T("setup.learn") + " " + StateName(state), Width = 122, Height = 34, Margin = new Padding(4), BackColor = Raised, ForeColor = Ink, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            button.FlatAppearance.BorderColor = Border;
            button.Click += (_, _) => LearnState(state);
            flow.Controls.Add(button);
        }
        dialog.ShowDialog(this);
        RefreshSetupStatus();
    }

    private void LearnState(string state)
    {
        try
        {
            if (lastFrame is null) return;
            if (adaptiveAlignment && !motionCompensator.IsReady)
            {
                if (positions.Count > 0 || imageTemplates.Count > 0)
                {
                    var answer = MessageBox.Show(this,
                        language == "de"
                            ? "Eine neue adaptive Kamerareferenz wird aufgenommen. Vorhandene Positionen werden gelöscht und müssen neu eingelernt werden. Fortfahren?"
                            : "A new adaptive camera reference will be captured. Existing learned positions will be cleared and must be captured again. Continue?",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes) return;
                    ClearLearnedPositions();
                }
                if (!motionCompensator.SetReference(lastFrame, out string why))
                    throw new InvalidOperationException(why);
                alignmentAnchorBase64 = Convert.ToBase64String(lastFrame.ToBytes(".png"));
                Log(string.Format(T("log.anchor"), motionCompensator.AnchorFeatureCount));
            }
            if (UsesImageMatching)
            {
                if (imageTemplates.Remove(state, out Mat? old)) old.Dispose();
                imageTemplates[state] = CreateImageTemplate(lastFrame);
                Log(string.Format(T("log.learnedImage"), StateName(state)));
            }
            else
            {
                if (detected is null) throw new InvalidOperationException(language == "de" ? "Farbmarker im Livebild nicht erkannt." : "The color marker is not visible in the live image.");
                positions[state] = detected.Value;
                Log(string.Format(T("log.learnedPosition"), StateName(state), detected.Value.X, detected.Value.Y));
            }
            SaveSettings(); BuildCalibrationButtons(); RefreshSetupStatus();
        }
        catch (Exception ex)
        {
            Log(string.Format(T("log.error"), ex.Message));
            MessageBox.Show(this, language == "de" ? "Position konnte nicht eingelernt werden:\n" + ex.Message : "Could not capture this position:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearCalibration()
    {
        var confirm = MessageBox.Show(this,
            language == "de" ? "Alle Gangpositionen und die adaptive Kamerareferenz löschen?" : "Clear all learned gear positions and the adaptive camera reference?",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;
        armed = false; ReleaseOutputs();
        ClearLearnedPositions();
        motionCompensator.Reset(); alignmentAnchorBase64 = null;
        activeGear = candidateGear = lastConfirmedGear = null;
        SaveSettings(); RefreshVersionUi(); SetStatus("status.needCalibration", false);
        Log("Calibration cleared by user.");
    }

    private void InvalidateCalibration(string reasonEn, string reasonDe)
    {
        armed = false; ReleaseOutputs();
        ClearLearnedPositions();
        motionCompensator.Reset(); alignmentAnchorBase64 = null;
        activeGear = candidateGear = lastConfirmedGear = null;
        detected = null;
        currentGearLabel.Text = "—";
        patternView?.Invalidate();
        SetStatus("status.needCalibration", false);
        Log(string.Format(T("log.calibrationReset"), language == "de" ? reasonDe : reasonEn));
        SaveSettings(); RefreshSetupStatus();
    }

    private void ClearLearnedPositions()
    {
        positions.Clear();
        foreach (Mat template in imageTemplates.Values) template.Dispose();
        imageTemplates.Clear();
    }

    private HashSet<string> CurrentLearnedStates() =>
        (UsesImageMatching ? imageTemplates.Keys.AsEnumerable() : positions.Keys.AsEnumerable()).Where(ActiveStates.Contains).ToHashSet();

    private void ToggleArmed()
    {
        var missing = ActiveStates.Where(state => !CurrentLearnedStates().Contains(state)).ToArray();
        if (!armed && missing.Length > 0)
        {
            MessageBox.Show(this,
                (language == "de" ? "Bitte zuerst alle aktiven Positionen einlernen: " : "Teach all active positions first: ") + string.Join(", ", missing.Select(StateName)),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowPage(1);
            return;
        }
        if (!armed && !OutputRunning)
        {
            MessageBox.Show(this, language == "de" ? "Starte zuerst die gewählte Ausgabe im Bereich Eingaben & Ausgabe." : "Start the selected output first on the Inputs & output page.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowPage(2);
            return;
        }

        armed = !armed;
        if (!armed)
        {
            ReleaseOutputs(); v2OutputQueue.Clear(); v2NeutralSent.Clear(); lastConfirmedGear = null;
        }
        else if (activeGear is not null)
        {
            if (selectedVersion2)
            {
                QueueV2States(BuildNeutralRoute(activeGear));
                if (IsGear(activeGear)) { QueueV2States([activeGear]); lastConfirmedGear = activeGear; v2NeutralSent.Clear(); }
            }
            else DispatchState(activeGear, null);
        }
        SetStatus(armed ? "status.armed" : "status.paused", armed);
        UpdateDynamicLabels();
        Log(armed ? "Shift detection armed." : "Shift detection paused.");
    }

    private async Task StartOutputAsync()
    {
        if (outputBusy) return;
        if (selectedOutput == "keyboard")
        {
            outputReady = true;
            RefreshOutputUi();
            Log(string.Format(T("log.outputStarted"), T(OutputModeKey())));
            return;
        }
        if (selectedOutput == "xbox" && !Program.IsAdministrator())
        {
            var answer = MessageBox.Show(this, T("inputs.elevateConfirm"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
            try
            {
                SaveSettings();
                string exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName
                    ?? throw new InvalidOperationException("Could not locate the application executable.");
                var start = new ProcessStartInfo(exe, "--enable-virtual-gamepad") { UseShellExecute = true, Verb = "runas", WorkingDirectory = AppContext.BaseDirectory };
                Process.Start(start);
                frameTimer.Stop(); capture?.Release(); capture?.Dispose(); capture = null;
                Close();
            }
            catch (Exception ex)
            {
                Log(string.Format(T("log.error"), ex.Message));
                MessageBox.Show(this, T("inputs.notAdmin") + Environment.NewLine + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        if (selectedOutput == "xbox")
        {
            bool installed;
            try { installed = await Task.Run(HidMaestroGamepadOutput.IsDriverInstalled); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!installed)
            {
                var answer = MessageBox.Show(this, T("inputs.driverConfirm"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes) return;
            }
        }

        outputBusy = true; startOutputButton.Enabled = false;
        try
        {
            if (selectedOutput == "vjoy")
            {
                vJoyOutput?.Dispose();
                vJoyOutput = await Task.Run(() => new VJoyOutput(selectedVersion2 ? 9 : 7));
            }
            else if (selectedOutput == "xbox")
            {
                gamepadOutput?.Dispose();
                gamepadOutput = await Task.Run(() => new HidMaestroGamepadOutput());
            }
            outputReady = true;
            RefreshOutputUi();
            Log(string.Format(T("log.outputStarted"), T(OutputModeKey())));
            SetStatus(selectedOutput == "xbox" ? "status.driverReady" : selectedOutput == "vjoy" ? "status.vjoyReady" : "status.ready", true);
        }
        catch (Exception ex)
        {
            outputReady = false;
            RefreshOutputUi();
            Log(string.Format(T("log.error"), ex.Message));
            MessageBox.Show(this, T("inputs.error") + Environment.NewLine + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            outputBusy = false;
            startOutputButton.Enabled = true;
            RefreshOutputUi();
        }
    }

    private async Task StopOutputAsync()
    {
        armed = false;
        ReleaseOutputs();
        v2OutputQueue.Clear(); v2NeutralSent.Clear();
        vJoyOutput?.Dispose(); vJoyOutput = null;
        if (gamepadOutput is not null)
        {
            await Task.Run(() => gamepadOutput.Dispose());
            gamepadOutput = null;
        }
        outputReady = false;
        RefreshOutputUi(); SetStatus("status.paused", false);
        Log(T("log.outputStopped"));
    }

    private void QueueV2States(IEnumerable<string> states)
    {
        foreach (string state in states)
        {
            if (IsNeutralLane(state) && !v2NeutralSent.Add(state)) continue;
            if (v2OutputQueue.Count == 0 || v2OutputQueue.Last() != state) v2OutputQueue.Enqueue(state);
        }
        if (!v2PumpRunning) _ = PumpV2Output();
    }

    private async Task PumpV2Output()
    {
        v2PumpRunning = true;
        try
        {
            while (v2OutputQueue.Count > 0)
            {
                if (!armed || !OutputRunning) { v2OutputQueue.Clear(); break; }
                string state = v2OutputQueue.Dequeue();
                DispatchState(state, activeGear);
                await Task.Delay(45);
            }
        }
        catch (Exception ex) { Log(string.Format(T("log.error"), ex.Message)); }
        finally { v2PumpRunning = false; if (armed && v2OutputQueue.Count > 0) _ = PumpV2Output(); }
    }

    private void DispatchState(string state, string? previous)
    {
        if (!OutputRunning) return;
        if (selectedOutput == "vjoy")
        {
            try
            {
                int button = StateButton(state, selectedVersion2);
                vJoyOutput!.SetStateButton((byte)button);
                Log(string.Format(T("log.outputPad"), StateName(state), $"vJoy button {button}"));
            }
            catch (Exception ex) { Log(string.Format(T("log.error"), ex.Message)); }
            return;
        }
        if (selectedOutput == "xbox")
        {
            try
            {
                string buttonId = padAssignments[state];
                var choice = PadChoices.First(item => item.Id == buttonId);
                gamepadOutput!.SetButtons(choice.Button, choice.Hat);
                Log(string.Format(T("log.outputPad"), StateName(state), buttonId));
            }
            catch (Exception ex) { Log(string.Format(T("log.error"), ex.Message)); }
            return;
        }

        string key = keyAssignments[state].Trim().ToUpperInvariant();
        bool doubleTap = !selectedVersion2 && doublePressEnabled && previous is not null && previous != "N" && state != "N";
        _ = SendShiftKeys(key, previous, state, doubleTap, doublePressGapMs);
    }

    private async Task SendShiftKeys(string key, string? previous, string state, bool doubleTap, int gapMs)
    {
        if (!SendKey(key, out string error))
        {
            Log(string.Format(T("log.error"), $"{key}: {error}"));
            return;
        }
        int taps = 1;
        if (doubleTap)
        {
            await Task.Delay(gapMs);
            if (SendKey(key, out error)) taps = 2;
            else Log(string.Format(T("log.error"), error));
        }
        Log(string.Format(T("log.shift"), StateName(previous ?? "—"), StateName(state), string.Format(T("log.outputKey"), key, taps)));
    }

    private void ReleaseOutputs()
    {
        try { vJoyOutput?.ReleaseActiveButton(); } catch { }
        try { gamepadOutput?.SetButtons(HMButton.None); } catch { }
    }

    private void BeginKeyCapture(string state, TextBox box)
    {
        keyCaptureTarget = box;
        box.Focus();
        keyCaptureHint.Text = language == "de" ? $"Taste für {StateName(state)} drücken …" : $"Press a key for {StateName(state)} …";
        keyCaptureHint.ForeColor = Amber;
    }

    private void CaptureKey(string state, TextBox box, KeyEventArgs e)
    {
        if (keyCaptureTarget != box) return;
        string? name = KeyName(e.KeyCode);
        e.Handled = true; e.SuppressKeyPress = true;
        if (name is null)
        {
            keyCaptureHint.Text = language == "de" ? "Nicht unterstützte Taste – bitte eine andere drücken." : "Unsupported key · press a different key.";
            keyCaptureHint.ForeColor = Amber;
            return;
        }
        keyAssignments[state] = name;
        box.Text = name;
        keyCaptureTarget = null;
        keyCaptureHint.Text = language == "de" ? $"{StateName(state)} → {name} · gespeichert" : $"{StateName(state)} → {name} · saved";
        keyCaptureHint.ForeColor = Teal;
        SaveSettings();
    }

    private static string? KeyName(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z) return key.ToString();
        if (key >= Keys.D0 && key <= Keys.D9) return ((int)key - (int)Keys.D0).ToString();
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return ((int)key - (int)Keys.NumPad0).ToString();
        if (key >= Keys.F1 && key <= Keys.F24) return key.ToString();
        return key switch
        {
            Keys.Space => "SPACE", Keys.Enter => "ENTER", Keys.Tab => "TAB", Keys.Escape => "ESC",
            Keys.Back => "BACKSPACE", Keys.Delete => "DELETE", Keys.Up => "UP", Keys.Down => "DOWN",
            Keys.Left => "LEFT", Keys.Right => "RIGHT", Keys.Insert => "INSERT", Keys.Home => "HOME",
            Keys.End => "END", Keys.PageUp => "PAGEUP", Keys.PageDown => "PAGEDOWN", _ => null
        };
    }

    private void FrameTick()
    {
        if (capture is null || !capture.IsOpened()) return;
        try
        {
            using var raw = new Mat();
            if (!capture.Read(raw) || raw.Empty()) return;
            using var transformed = TransformFrame(raw);
            Mat frame;
            bool alignmentOk = true;
            string alignmentMessage;

            if (adaptiveAlignment && motionCompensator.IsReady)
            {
                if (motionCompensator.TryAlign(transformed, out Mat? aligned, out alignmentMessage) && aligned is not null)
                    frame = aligned;
                else
                {
                    frame = transformed.Clone();
                    alignmentOk = false;
                    if (lastAlignmentLogState != alignmentMessage)
                    {
                        Log(T("log.alignLost") + " " + alignmentMessage);
                        lastAlignmentLogState = alignmentMessage;
                    }
                }
            }
            else
            {
                frame = transformed.Clone();
                alignmentMessage = adaptiveAlignment ? T("diag.noAnchor") : T("diag.legacy");
                if (lastAlignmentLogState != alignmentMessage && adaptiveAlignment)
                {
                    Log(T("diag.noAnchor"));
                    lastAlignmentLogState = alignmentMessage;
                }
            }

            lastFrame?.Dispose();
            lastFrame = frame;
            if (alignmentOk)
            {
                detected = UsesImageMatching ? null : TrackMarker(frame);
                string? gear = Classify(detected, frame);
                UpdateShift(gear);
                if (gear is not null) lastAlignmentLogState = null;
            }
            else
            {
                detected = null;
                UpdateShift(null); // Fail closed: do not emit a false gear while alignment is uncertain.
            }

            UpdateAlignmentUi(alignmentMessage, alignmentOk);
            RenderPreview(frame, alignmentOk);
            UpdateDynamicLabels();
        }
        catch (Exception ex)
        {
            Log(string.Format(T("log.error"), ex.Message));
        }
    }

    private Mat TransformFrame(Mat raw)
    {
        Mat result = raw.Clone();
        if (selectedRotation == 1) Cv2.Rotate(result, result, RotateFlags.Rotate90Clockwise);
        else if (selectedRotation == 2) Cv2.Rotate(result, result, RotateFlags.Rotate180);
        else if (selectedRotation == 3) Cv2.Rotate(result, result, RotateFlags.Rotate90Counterclockwise);
        if (selectedMirrorH || selectedMirrorV)
        {
            FlipMode mode = selectedMirrorH && selectedMirrorV ? FlipMode.XY : selectedMirrorH ? FlipMode.Y : FlipMode.X;
            Cv2.Flip(result, result, mode);
        }
        if (result.Width != 640 || result.Height != 480)
        {
            var resized = new Mat();
            Cv2.Resize(result, resized, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Area);
            result.Dispose();
            result = resized;
        }
        return result;
    }

    private void UpdateAlignmentUi(string text, bool good)
    {
        alignmentValue.Text = text;
        alignmentValue.ForeColor = good ? Teal : Amber;
        alignmentStatusText = text;
        alignmentStatusGood = good;
        alignmentValue.Refresh();
        featureValue.Text = adaptiveAlignment && motionCompensator.IsReady
            ? $"{motionCompensator.LastInlierCount}/{motionCompensator.LastMatchCount} inliers · {motionCompensator.LastInlierRatio:P0}"
            : "—";
        candidateValue.Text = candidateGear is null ? "—" : StateName(candidateGear);
        activeValue.Text = activeGear is null ? "—" : StateName(activeGear);
        if (!good && armed) SetStatus("status.trackingLost", false);
        else if (armed) SetStatus("status.armed", true);
        else if (capture is not null && capture.IsOpened()) SetStatus("status.paused", false);
    }

    private string? alignmentStatusText;
    private bool alignmentStatusGood = true;

    private void RenderPreview(Mat frame, bool alignmentOk)
    {
        var bitmap = BitmapConverter.ToBitmap(frame);
        using (Graphics g = Graphics.FromImage(bitmap))
        using (var pen = new Pen(alignmentOk ? Teal : Amber, 2.0F))
        using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
        using (var textBrush = new SolidBrush(Color.FromArgb(225, 14, 17, 20)))
        using (var textInk = new SolidBrush(alignmentOk ? Teal : Amber))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (UsesImageMatching)
            {
                var roi = new Rectangle((int)(frame.Width * .25), (int)(frame.Height * .22), (int)(frame.Width * .55), (int)(frame.Height * .44));
                g.DrawRectangle(pen, roi);
                g.FillRectangle(textBrush, roi.X, roi.Y - 22, 190, 20);
                g.DrawString(language == "de" ? "VORLAGEN-SUCHBEREICH" : "TEMPLATE SEARCH AREA", font, textInk, roi.X + 5, roi.Y - 20);
            }
            if (detected is { } point)
            {
                float x = (float)(point.X * frame.Width), y = (float)(point.Y * frame.Height);
                g.DrawEllipse(pen, x - 11, y - 11, 22, 22);
                g.DrawLine(pen, x - 18, y, x + 18, y);
                g.DrawLine(pen, x, y - 18, x, y + 18);
            }
            string tag = alignmentOk
                ? (adaptiveAlignment && motionCompensator.IsReady ? $"ALIGN  {motionCompensator.LastInlierCount} INLIERS" : "LEGACY / UNANCHORED")
                : "ALIGNMENT LOST · SAFE HOLD";
            var tagSize = g.MeasureString(tag, font);
            var box = new RectangleF(10, 10, tagSize.Width + 18, tagSize.Height + 10);
            using var fill = new SolidBrush(Color.FromArgb(205, 18, 22, 26));
            g.FillRectangle(fill, box);
            g.DrawString(tag, font, textInk, 19, 15);
        }
        Image? old = preview.Image;
        preview.Image = bitmap;
        old?.Dispose();
    }

    private static Mat CreateImageTemplate(Mat frame)
    {
        int x = (int)(frame.Width * .25), y = (int)(frame.Height * .22);
        int w = (int)(frame.Width * .55), h = (int)(frame.Height * .44);
        using var roi = new Mat(frame, new Rect(x, y, w, h));
        using var small = new Mat();
        Cv2.Resize(roi, small, new OpenCvSharp.Size(160, 120));
        using var gray = new Mat();
        Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.EqualizeHist(gray, gray);
        Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(3, 3), 0);
        return gray.Clone();
    }

    private (double X, double Y)? TrackMarker(Mat frame)
    {
        if (markerColor is null) return null;
        using var hsv = new Mat(); using var mask = new Mat();
        Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);
        var (h, s, v) = markerColor.Value;
        var lower = new Scalar(Math.Max(0, h - 11), Math.Max(45, s - 100), Math.Max(45, v - 100));
        var upper = new Scalar(Math.Min(179, h + 11), 255, 255);
        Cv2.InRange(hsv, lower, upper, mask);
        if (h < 11 || h > 168)
        {
            using var m1 = new Mat(); using var m2 = new Mat();
            Cv2.InRange(hsv, new Scalar(0, Math.Max(45, s - 100), Math.Max(45, v - 100)), new Scalar(Math.Min(179, h + 11), 255, 255), m1);
            Cv2.InRange(hsv, new Scalar(Math.Max(0, h - 11), Math.Max(45, s - 100), Math.Max(45, v - 100)), new Scalar(179, 255, 255), m2);
            Cv2.BitwiseOr(m1, m2, mask);
        }
        Cv2.MedianBlur(mask, mask, 5);
        Cv2.FindContours(mask, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        OpenCvSharp.Point[]? best = contours.Where(c => Cv2.ContourArea(c) > 12 && Cv2.ContourArea(c) < frame.Width * frame.Height * .18).OrderByDescending(c => Cv2.ContourArea(c)).FirstOrDefault();
        if (best is null) return null;
        Moments moments = Cv2.Moments(best);
        if (moments.M00 == 0) return null;
        return (moments.M10 / moments.M00 / frame.Width, moments.M01 / moments.M00 / frame.Height);
    }

    private string? Classify((double X, double Y)? point, Mat frame)
    {
        if (UsesImageMatching)
        {
            var templates = imageTemplates.Where(t => ActiveStates.Contains(t.Key)).ToArray();
            if (templates.Length == 0) return null;
            using var current = CreateImageTemplate(frame);
            var scores = templates.Select(t =>
            {
                using var diff = new Mat();
                Cv2.Absdiff(current, t.Value, diff);
                return (Gear: t.Key, Score: Cv2.Mean(diff).Val0);
            }).OrderBy(x => x.Score).ToArray();
            return scores.Length > 0 && scores[0].Score < 64 && (scores.Length == 1 || scores[1].Score - scores[0].Score >= 2.5)
                ? scores[0].Gear : null;
        }

        var known = positions.Where(p => ActiveStates.Contains(p.Key)).ToArray();
        if (point is null || known.Length == 0) return null;
        var nearest = known.OrderBy(p => Distance(p.Value.X, p.Value.Y, point.Value.X, point.Value.Y)).First();
        return Distance(nearest.Value.X, nearest.Value.Y, point.Value.X, point.Value.Y) <= .18 ? nearest.Key : null;
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        double dx = x1 - x2, dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void UpdateShift(string? gear)
    {
        long now = clock.ElapsedMilliseconds;
        if (gear != candidateGear)
        {
            candidateGear = gear;
            candidateSince = now;
            selectedCandidate = gear is null ? "—" : StateName(gear);
            if (!(selectedVersion2 && IsNeutralLane(gear))) return;
        }
        if (gear is null || gear == activeGear) return;
        int settleMs = !selectedVersion2 ? 220 : IsNeutralLane(gear) ? 0 : 70;
        if (now - candidateSince < settleMs) return;
        string? previous = activeGear;
        activeGear = gear;
        currentGearLabel.Text = StateName(gear);
        patternView?.SetActive(gear);
        if (armed)
        {
            if (selectedVersion2)
            {
                QueueV2States(BuildNeutralRoute(gear));
                if (IsGear(gear)) { QueueV2States([gear]); lastConfirmedGear = gear; v2NeutralSent.Clear(); }
            }
            else DispatchState(gear, previous);
        }
        else
        {
            Log(string.Format(T("log.shift"), StateName(previous ?? "—"), StateName(gear), language == "de" ? "Ausgabe pausiert" : "output paused"));
        }
        candidateValue.Text = StateName(gear);
        activeValue.Text = StateName(gear);
    }

    private void SetStatus(string key, bool success)
    {
        statusKey = key;
        statusSuccess = success;
        if (!headerStatus.IsHandleCreated) return;
        headerStatus.Text = "●  " + T(key);
        headerStatus.ForeColor = success ? Teal : Amber;
    }

    private void Log(string message)
    {
        if (logBox.IsDisposed || !logBox.IsHandleCreated) return;
        logBox.AppendText($"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
        logBox.SelectionStart = logBox.TextLength;
        logBox.ScrollToCaret();
    }

    private string OutputModeKey() => selectedOutput switch { "vjoy" => "inputs.vjoy", "xbox" => "inputs.virtual", _ => "inputs.keyboard" };

    private static int StateButton(string state, bool v2) => state switch
    {
        "1" => 1, "2" => 2, "3" => 3, "4" => 4, "5" => 5, "R" => 6,
        "NL" => 7, "N" => v2 ? 8 : 7, "NR" => 9, _ => 0
    };

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
            var serializedTemplates = imageTemplates.ToDictionary(t => t.Key, t => Convert.ToBase64String(t.Value.ToBytes(".png")));
            var settings = new Settings
            {
                Positions = positions.ToDictionary(p => p.Key, p => new[] { p.Value.X, p.Value.Y }),
                MarkerHsv = markerColor is null ? null : [markerColor.Value.H, markerColor.Value.S, markerColor.Value.V],
                Keys = new Dictionary<string, string>(keyAssignments),
                GamepadButtons = new Dictionary<string, string>(padAssignments),
                ImageTemplates = serializedTemplates,
                View = selectedViewIndex == 0 ? "top" : "bottom",
                Tracking = selectedTrackingIndex == 0 ? "marker" : "image",
                OutputMode = selectedOutput,
                Rotation = selectedRotation,
                CameraIndex = selectedCamera,
                MirrorH = selectedMirrorH,
                MirrorV = selectedMirrorV,
                DoublePressOnDirectShift = doublePressEnabled,
                DoublePressGapMs = doublePressGap.IsHandleCreated ? (int)doublePressGap.Value : doublePressGapMs,
                Version2 = selectedVersion2,
                AdaptiveAlignment = adaptiveAlignment,
                AlignmentAnchor = alignmentAnchorBase64,
                Language = language
            };
            string temp = ConfigFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, ConfigFile, true);
        }
        catch (Exception ex) { Log(string.Format(T("log.error"), "Settings could not be saved: " + ex.Message)); }
    }

    private void LoadSettings()
    {
        if (!File.Exists(ConfigFile)) return;
        try
        {
            Settings? s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(ConfigFile));
            if (s is null) return;
            foreach (var p in s.Positions) if (p.Value.Length == 2) positions[p.Key] = (p.Value[0], p.Value[1]);
            if (s.MarkerHsv is { Length: 3 }) markerColor = (s.MarkerHsv[0], s.MarkerHsv[1], s.MarkerHsv[2]);
            foreach (var pair in s.Keys) if (keyAssignments.ContainsKey(pair.Key)) keyAssignments[pair.Key] = pair.Value;
            foreach (var pair in s.GamepadButtons) if (padAssignments.ContainsKey(pair.Key) && PadChoices.Any(p => p.Id == pair.Value)) padAssignments[pair.Key] = pair.Value;
            foreach (var pair in s.ImageTemplates)
            {
                try
                {
                    using var bytes = new Mat();
                    var decoded = Cv2.ImDecode(Convert.FromBase64String(pair.Value), ImreadModes.Grayscale);
                    if (!decoded.Empty()) imageTemplates[pair.Key] = decoded;
                    else decoded.Dispose();
                }
                catch { }
            }
            selectedViewIndex = s.View.Contains("top", StringComparison.OrdinalIgnoreCase) || s.View.Contains("oben", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            selectedTrackingIndex = s.Tracking.Contains("marker", StringComparison.OrdinalIgnoreCase) || s.Tracking.Contains("Farb", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            selectedRotation = Math.Clamp(s.Rotation, 0, 3);
            selectedCamera = Math.Clamp(s.CameraIndex, 0, 9);
            selectedMirrorH = s.MirrorH; selectedMirrorV = s.MirrorV;
            doublePressEnabled = s.DoublePressOnDirectShift;
            doublePressGapMs = Math.Clamp(s.DoublePressGapMs, 20, 500);
            if (s.Version2.HasValue) selectedVersion2 = s.Version2.Value;
            selectedOutput = s.OutputMode.ToLowerInvariant() switch
            {
                "vjoy" => "vjoy", "xbox" or "virtual-xbox" => "xbox",
                "tastaturtasten" or "keyboard keys" => "keyboard",
                "virtueller joystick (vjoy)" => "vjoy", _ => "keyboard"
            };
            if (s.AdaptiveAlignment.HasValue) adaptiveAlignment = s.AdaptiveAlignment.Value;
            else adaptiveAlignment = false; // Existing installs retain their fixed-camera behavior until opted in.
            language = s.Language.Equals("de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";
            alignmentAnchorBase64 = s.AlignmentAnchor;
            if (!string.IsNullOrWhiteSpace(alignmentAnchorBase64))
            {
                using var anchor = Cv2.ImDecode(Convert.FromBase64String(alignmentAnchorBase64), ImreadModes.Color);
                if (!anchor.Empty() && motionCompensator.SetReference(anchor, out _)) { }
                else alignmentAnchorBase64 = null;
            }
        }
        catch
        {
            // A damaged or older settings file should never prevent startup.
        }
    }

    private void Shutdown()
    {
        frameTimer.Stop();
        armed = false;
        ReleaseOutputs();
        vJoyOutput?.Dispose(); vJoyOutput = null;
        gamepadOutput?.Dispose(); gamepadOutput = null;
        capture?.Release(); capture?.Dispose(); capture = null;
        lastFrame?.Dispose(); lastFrame = null;
        foreach (Mat template in imageTemplates.Values) template.Dispose();
        imageTemplates.Clear();
        motionCompensator.Dispose();
        preview.Image?.Dispose(); preview.Image = null;
        SaveSettings();
    }

    private sealed class Settings
    {
        public Dictionary<string, double[]> Positions { get; set; } = new();
        public int[]? MarkerHsv { get; set; }
        public Dictionary<string, string> Keys { get; set; } = new();
        public Dictionary<string, string> GamepadButtons { get; set; } = new();
        public Dictionary<string, string> ImageTemplates { get; set; } = new();
        public string View { get; set; } = "bottom";
        public string Tracking { get; set; } = "image";
        public string OutputMode { get; set; } = "keyboard";
        public int Rotation { get; set; }
        public int CameraIndex { get; set; }
        public bool MirrorH { get; set; }
        public bool MirrorV { get; set; }
        public bool DoublePressOnDirectShift { get; set; } = true;
        public int DoublePressGapMs { get; set; } = 90;
        public bool? Version2 { get; set; }
        public bool? AdaptiveAlignment { get; set; }
        public string? AlignmentAnchor { get; set; }
        public string Language { get; set; } = "en";
    }

    private sealed record CameraItem(int Index, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record PadChoice(string Id, string Display, HMButton Button, HMHat Hat)
    {
        public override string ToString() => Display;
    }

    private sealed class GearPatternView : Panel
    {
        private string? activeState;
        public GearPatternView() { DoubleBuffered = true; MinimumSize = new Size(220, 240); }

        public void SetActive(string? state)
        {
            if (activeState == state) return;
            activeState = state;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float w = ClientSize.Width, h = ClientSize.Height;
            float[] xs = [w * .22F, w * .50F, w * .78F];
            float top = h * .22F, middle = h * .50F, bottom = h * .78F;
            using var railPen = new Pen(Border, Math.Max(3F, w / 110F)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (int i = 0; i < 3; i++) e.Graphics.DrawLine(railPen, xs[i], top, xs[i], bottom);
            e.Graphics.DrawLine(railPen, xs[0], middle, xs[2], middle);
            DrawPosition(e.Graphics, "1", xs[0], top);
            DrawPosition(e.Graphics, "3", xs[1], top);
            DrawPosition(e.Graphics, "5", xs[2], top);
            DrawPosition(e.Graphics, "N-L", xs[0], middle);
            DrawPosition(e.Graphics, "N", xs[1], middle);
            DrawPosition(e.Graphics, "N-R", xs[2], middle);
            DrawPosition(e.Graphics, "2", xs[0], bottom);
            DrawPosition(e.Graphics, "4", xs[1], bottom);
            DrawPosition(e.Graphics, "R", xs[2], bottom);
        }

        private void DrawPosition(Graphics g, string label, float cx, float cy)
        {
            string state = label switch { "N-L" => "NL", "N-R" => "NR", _ => label };
            bool active = activeState == state;
            float radius = active ? 18F : 14F;
            using var fill = new SolidBrush(active ? Teal : Raised);
            using var outline = new Pen(active ? Teal : Border, active ? 2F : 1F);
            using var font = new Font("Segoe UI", active ? 9F : 8F, FontStyle.Bold);
            using var ink = new SolidBrush(active ? Color.FromArgb(15, 24, 26) : Ink);
            g.FillEllipse(fill, cx - radius, cy - radius, radius * 2, radius * 2);
            g.DrawEllipse(outline, cx - radius, cy - radius, radius * 2, radius * 2);
            var size = g.MeasureString(label, font);
            g.DrawString(label, font, ink, cx - size.Width / 2, cy - size.Height / 2);
        }
    }

    private static bool SendKey(string name, out string error)
    {
        error = string.Empty;
        ushort vk;
        if (name.Length >= 2 && name[0] == 'F' && ushort.TryParse(name[1..], out ushort f) && f is >= 1 and <= 24)
            vk = (ushort)(0x70 + f - 1);
        else if (name.Length == 1 && char.IsLetterOrDigit(name[0]))
            vk = (ushort)char.ToUpperInvariant(name[0]);
        else
            vk = name switch
            {
                "SPACE" => 0x20, "ENTER" => 0x0D, "TAB" => 0x09, "ESC" => 0x1B, "BACKSPACE" => 0x08,
                "DELETE" => 0x2E, "UP" => 0x26, "DOWN" => 0x28, "LEFT" => 0x25, "RIGHT" => 0x27,
                "INSERT" => 0x2D, "HOME" => 0x24, "END" => 0x23, "PAGEUP" => 0x21, "PAGEDOWN" => 0x22, _ => 0
            };
        if (vk == 0) { error = "Unsupported key."; return false; }
        var inputs = new[]
        {
            new INPUT { type = 1, U = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk } } },
            new INPUT { type = 1, U = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = 2 } } }
        };
        uint sent = SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        if (sent != 2) { error = $"Windows SendInput error {Marshal.GetLastWin32Error()} (a game running as administrator may block it)."; return false; }
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint n, INPUT[] pInputs, int cbSize);
    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public INPUTUNION U; }
    [StructLayout(LayoutKind.Explicit)] private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public nint dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public nint dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }
}
