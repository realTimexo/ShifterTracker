# ShifterTracker

A Windows camera-to-input app for a 3D-printed H-pattern shifter. It tracks the physical knob, displays the active gate, and sends keyboard or controller inputs to games such as BeamNG.drive.

**Current release: 1.1.0** · Windows 10/11 x64 · self-contained single-file app · English and German UI

## Download

Download the current release from [GitHub Releases](https://github.com/realTimexo/ShifterTracker/releases):

- **`ShiftTracker-v1.1.0-win-x64.exe`** is the standalone app; no separate Python or .NET runtime is needed.
- **`ShiftTracker-v1.1.0-win-x64.zip`** includes the app, quick-start, changelog and license notices.
- The release also attaches ShifterTracker's MIT license, the HIDMaestro and .NET licenses and third-party notices, the Apache-2.0 license and a consolidated third-party summary as separate assets, including for people who download only the EXE.
- Check the matching `.sha256` files to verify downloads.

## What's new in 1.1.0

- **One-screen dashboard:** live camera preview and camera controls stay on the right; current position, H-pattern, calibration buttons, output mode and gear bindings stay on the left. A compact developer log is always visible. The language control switches between English and German.
- **New V3 knob tracker:** click the knob once in the live view. V3 tracks sparse surface features on the knob itself with fast pyramidal Lucas–Kanade optical flow. It does **not** realign the complete camera image or compare stored pictures of each gear. The detected knob center is classified against the learned position coordinates. Small camera shifts can be tolerated while the knob stays visible; a large viewpoint change still needs recalibration.
- **V1 and V2 remain available:** V1 is the fixed-camera path; V2 keeps the previous background-feature alignment method. Tracking engine (V1/V2/V3) and V1/V2 shift-output protocol are separate choices.
- **HIDMaestro v2.0.0:** the optional built-in virtual Xbox 360-compatible gamepad now uses the upstream 2.0.0 SDK and current driver migration path. The release builder pins and verifies the upstream archive by SHA-256.
- **License distribution:** ShifterTracker, HIDMaestro v2.0.0, .NET and Apache license material is provided in the ZIP and as separate release downloads beside the standalone EXE asset.

## First setup

1. Extract the ZIP (or run the standalone EXE). Exit older ShifterTracker versions before selecting virtual-gamepad output.
2. In **Camera settings**, scan for and connect the camera. Choose the camera perspective, rotation and mirroring that match the live view.
3. Select **V3 · track the knob**. With the knob sharply visible, click its center in the live camera preview. The preview shows its tracked center and feature count. Repeat the click if the track is lost.
4. Move the lever to every position shown under **Teach positions** and click its capture button. In V2 shift protocol, teach N-L, N and N-R as well as 1–5 and R. The gear coordinates and other settings are saved locally; the live optical-flow anchor must be clicked again after each app launch.
5. Choose keyboard, virtual Xbox gamepad or vJoy under **Output**. Configure the visible per-state bindings, start the output, then arm detection. Confirm the highlighted position and log events while the vehicle is stationary.
6. Bind those same keys/buttons to the matching gear actions in BeamNG once. ShifterTracker does not change game controls automatically.

V3 works best when the knob has visible texture, edges or other surface detail and remains in focus. If the tracker loses the knob during a very fast or blurred movement, click the knob again to reacquire and ensure all positions are still calibrated. A changed camera angle, resolution, rotation or mirror invalidates coordinate calibration. Validate with your own camera, hardware and lighting before driving; no camera tracker can guarantee perfect detection under every condition.

## Tracking engines and shift protocols

The **tracking engine** selects how the camera finds the shifter position:

- **V1 — fixed camera:** the established marker or local gear-image method, without camera-motion correction.
- **V2 — background alignment:** matches stable features on the outer housing, rejects outliers, aligns each frame to a fixed reference, then uses the existing marker or gear-position method. Use this only if you want its adaptive background alignment behavior.
- **V3 — direct knob tracking:** sparse optical-flow features are initialized on the knob and followed locally. The scene background is not used for stabilization; no per-gear picture comparison is used in this mode. Click the knob in the preview after opening the app and teach the gear coordinates.

The separate **shift protocol** controls how states are sent: V1 sends the direct gear state; V2 sends its ordered N-L/N/N-R lane route before the destination gear. Example: 1 → 2 sends N-L → N → 2; N → 5 sends N-R → 5. This is a short button sequence, not a physical analog H-shifter device.

V1/V2 can use **Color marker** or **Image matching**. Image matching is the earlier local gear-image method and is not used by V3. Camera perspective, rotation, mirroring or recognition changes require recalibration.

## Output modes

### Keyboard

Click each gear field and press the desired keyboard key. Defaults are F13–F19 for 1–5, R and N; F20/F21 are reserved for V2 N-L/N-R. V1 can optionally send a second quick tap for direct gear-to-gear shifts, with an adjustable interval. No virtual-controller driver is installed in keyboard mode.

### Built-in virtual Xbox gamepad

Each shifter state maps to a held gamepad input that can be bound in BeamNG. The output uses the bundled HIDMaestro 2.0.0 SDK. Windows administrator approval is required to create virtual devices. On the first post-upgrade use, HIDMaestro may need to update its driver; review the in-app confirmation and Windows prompts before accepting. The driver is handled by HIDMaestro, not silently installed when you download or open ShifterTracker. For v1-to-v2 driver migration, close the older ShifterTracker version first; upstream documents that a running 1.x HIDMaestro app can remove HIDMaestro devices while a 2.0.0 app is installing/updating the driver.

### vJoy / DirectInput

Retained for users who already have vJoy installed. Requires vJoy's separate driver, `vJoyInterface.dll` and a configured device. ShifterTracker does not install vJoy.

## Settings, logs and safety

Settings and calibration coordinates are stored in `%LOCALAPPDATA%\ShifterTracker\settings.json`. The app writes startup errors to `%LOCALAPPDATA%\ShifterTracker\startup.log`. The compact developer log is on the dashboard and records tracking status, gear transitions and output activity. If the app fails at startup, include the full startup log in a bug report.

Test every BeamNG binding while stationary. This is an input prototype, not safety-critical control software. V3 is object-centered tracking, not a guarantee that a camera movement, occlusion or motion blur will always be recovered.

## Build from source

Requirements: Windows x64, PowerShell 5.1 or later, and the .NET 10 SDK. Run:

```powershell
.\scripts\build-windows.ps1
```

The script downloads the pinned HIDMaestro v2.0.0 release, checks its SHA-256 before extracting the SDK and licenses, then publishes a self-contained Windows x64 executable, licensed ZIP and checksums under `artifacts/`.

## Release and license

A semantic-version Git tag such as `v1.1.0` triggers the Windows release workflow and must match the project version. Any valid prerelease suffix, such as `-alpha.1`, `-beta.2` or `-rc.1`, is marked as a prerelease. The repository's first stable release was 1.0.0; 1.0.1 fixed startup issues; 1.1.0 adds the dashboard and V3 tracker. Earlier prototypes had no verifiable releases in this repository and are not retroactively tagged.

ShifterTracker source is MIT-licensed. OpenCV, HIDMaestro, .NET and optional vJoy notices are listed in [THIRD-PARTY.md](THIRD-PARTY.md) and shipped with releases.

## Support and contributions

Made by Timexo. [Donate with PayPal](https://www.paypal.com/donate/?hosted_button_id=38GT8LH75W4GU) · [Buy me a coffee](https://timexo.gumroad.com/coffee)
