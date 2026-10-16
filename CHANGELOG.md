# Changelog

## 1.0.1 — 2026-10-16 — Stable patch

- Show the main window before probing cameras; move DirectShow enumeration to a background STA thread so a slow device does not block startup.
- Add visible startup/unhandled-error dialogs and a persistent `%LOCALAPPDATA%\ShifterTracker\startup.log` for diagnosis.

## 1.0.0 — 2026-10-16 — Stable

- First stable release in the `realTimexo/ShifterTracker` repository.
- Added adaptive V2 alignment to tolerate small camera translation, rotation and scale changes before gear classification.
- Added structured camera/setup, input/output, live-view and diagnostics sections with English/German UI selection.
- Preserved V1 fixed-camera tracking and existing keyboard/vJoy output paths.
- Added an optional bundled Xbox 360-compatible virtual gamepad path; its virtual-HID driver is installed only after user choice, in-app consent and Windows administrator approval.
- Added persistent local settings, state-by-state H-pattern calibration, V2 neutral-lane routing, a self-contained Windows x64 build, third-party notices and automated tagged releases.

### Release history note

This repository was empty when the 1.0.0 work began: it had no commits, tags or GitHub releases. Earlier prototype builds existed outside this repository, but there were no published assets or tag metadata to verify their order and exact source. They are therefore not retroactively labeled as alpha/beta releases. Future `v*-alpha*` and `v*-beta*` tags are automatically published as GitHub pre-releases.
