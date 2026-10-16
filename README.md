# ShifterTracker

A native Windows camera-to-input app for a 3D-printed H-pattern shifter. The live view shows the recognized position, the H-pattern diagram highlights it, and the selected game input is updated when the lever moves.

**Current release: 1.0.0** · Windows 10/11 x64 · .NET 10 self-contained single-file executable

## What is new in 1.0.0

- **Adaptive V2 tracking:** recognizes fixed features on the outer shifter housing, estimates small camera translation/rotation/scale changes every frame with ORB feature matching and a RANSAC similarity transform, then aligns the frame to the saved calibration reference before classifying the lever.
- **Fail-safe behavior:** if the background alignment is ambiguous or outside its supported range, the frame is rejected and the last output is held; it does not guess a different gear.
- **Structured interface:** Live View, Setup & Tracking, Inputs & Output, and Diagnostics are separate sections. English is the default; German is available in the language selector.
- **Three output choices:** keyboard simulation, optional vJoy/DirectInput, and a built-in Xbox 360-compatible virtual gamepad.
- V1 fixed-camera protocol remains available. V2 teaches the brief neutral-left, neutral-middle, and neutral-right gates and sends them in order during lane changes.

## Download and run

1. Download `ShiftTracker-v1.0.0-win-x64.exe` for the direct single-file app, or `ShiftTracker-v1.0.0-win-x64.zip` for the app plus quick-start and license notices, from the [GitHub Releases page](https://github.com/realTimexo/ShifterTracker/releases).
2. Extract it and run `ShiftTracker.exe`. No Python or separately installed .NET runtime is needed.
3. In **Setup & Tracking**, select and connect the camera. The initial view is **Bottom-up (reference photos)**; adjust rotation or mirroring if needed.
4. Select **Adaptive V2** (recommended), then choose **Image matching** for marker-free tracking or **Color marker** if a high-contrast marker is fitted.
5. Capture every position shown in the calibration card. The first adaptive capture stores a fixed-background reference; keep the shifter frame still while the reference is made. V2 includes **N-L**, **N**, and **N-R**.
6. In **Inputs & Output**, select an output and configure its bindings. Start the output, then return to Live View and click **Arm detection**.
7. Bind the chosen keyboard keys or gamepad buttons to the corresponding gear actions in BeamNG once. ShifterTracker does not change BeamNG's control configuration for you.

Settings, camera choice, calibration images, language and mappings are stored locally in `%LOCALAPPDATA%\ShifterTracker\settings.json`.

## Tracking modes

### Adaptive V2 (recommended)

The app extracts ORB features from the visible fixed outer housing, side rails and frame. It masks the central moving mechanism, finds correspondences to the calibration frame, rejects outliers with RANSAC, and applies the resulting small similarity transform before gear recognition. The reference is fixed rather than updated from frame to frame, avoiding cumulative drift. The alignment score, inlier count and live overlay are visible in Diagnostics and the camera preview.

This compensates for **small** changes in camera position and angle; it cannot recover an obstructed shifter, a very different viewpoint, severe blur, a featureless background or large camera displacement. If alignment is lost, stop the output, reposition the camera so the fixed housing is visible, and recalibrate. Tracking is tested against the six supplied shifter views with synthetic small translations, rotations and scale changes; validate it with your own camera, lighting and hardware before driving.

### V1 / Legacy

V1 uses the established fixed-camera position/template matching behavior, without camera-motion correction. Use it if the camera mount is rigid and you prefer the previous protocol. It remains available in **Setup & Tracking**.

### Recognition type

- **Image matching:** learn a small crop for each gear position; no sticker is required. The camera should be focused and lighting should remain reasonably consistent.
- **Color marker:** click the marker in the live view to sample its color, then teach each position.

Changing the camera perspective, recognition method, rotation or mirroring clears the calibration, because the saved coordinates would no longer be valid.

## Output options

### Keyboard keys

Captures real key presses: click a gear's field and press the desired key. Defaults are `F13`–`F19` for 1–5, R and N, with `F20`/`F21` reserved for V2 N-L/N-R. V1 has an optional second key tap for gear-to-gear changes; its interval is configurable. V2 uses its ordered neutral-gate sequence instead.

### Built-in virtual Xbox 360 gamepad

This exposes one held gamepad input for the active state, so BeamNG can bind the positions as controller inputs. The defaults use the four D-pad directions and face/shoulder buttons; they avoid Xbox Start/Back menu buttons. It uses the bundled HIDMaestro virtual-HID driver. Windows requires administrator approval to create a virtual controller: the first use offers a clear consent prompt, installs the bundled UMDF driver and locally trusted signing certificate, and then requests an elevated tracker session for virtual-pad use. The tracker app itself is not installed as a separate program. If you do not want a driver or administrator prompt, choose keyboard output instead. No driver is installed merely by downloading or opening the app.

**Security note:** review the prompt before accepting it. The driver installation changes Windows' local device/certificate state. The driver is optional and only requested when you explicitly start the virtual gamepad output. Driver creation still needs administrator approval each session.

### vJoy / DirectInput

Retained for existing vJoy users. Requires the separately installed vJoy driver, its `vJoyInterface.dll`, and Device 1 configured with at least 7 buttons for V1 or 9 for V2. This project does not install vJoy.

## V2 route behavior

V2 maps the three shifter lanes to N-L / N / N-R. It outputs the neutral lane or lanes along the route before the target gear, even when a short camera observation of a neutral gate falls between frames. These virtual states are sent as brief button holds to give games time to poll them. Example: 1 → 2 sends N-L → N → 2; N → 5 sends N-R → 5. This is a button-sequence simulation, not a physical analog H-shifter device.

## Diagnostics and safety

The **Diagnostics** page records camera frames, alignment confidence, active/candidate state and output events. Uncertain adaptive alignment is rejected. When detection is paused, the app releases any active virtual-controller button. Test in a stationary vehicle and confirm every BeamNG binding before driving. This software is an input prototype, not a safety-critical vehicle control.

## Build from source

Requirements: Windows x64, PowerShell 7 or Windows PowerShell 5.1, and the .NET 10 SDK. The build script downloads the pinned HIDMaestro v1.12.0 release, verifies its SHA-256, then creates a self-contained single-file executable and ZIP:

```powershell
.\scripts\build-windows.ps1
```

The output and checksum are placed under `artifacts/`. Third-party binary dependencies are fetched during the build and are not stored in this repository. See [CHANGELOG.md](CHANGELOG.md), [THIRD-PARTY.md](THIRD-PARTY.md), the included Apache license text, and the bundled HIDMaestro notices.

## Releases

Tags named `v*` build and publish a GitHub Release with the Windows ZIP and SHA-256 checksum. Tags containing `-alpha` or `-beta` are marked pre-releases. The 1.0.0 repository release starts the stable release line; older alpha/beta builds are published only when their original assets are available for verification.

## License

Project code: MIT. Third-party components and notices are listed in [THIRD-PARTY.md](THIRD-PARTY.md).
