# Changelog

## 1.1.0 — 2026-10-21 — Stable

- Replace separate navigation pages with a single structured dashboard: current gear, H-pattern, per-state calibration, output method and bindings on the left; camera preview and settings on the right; compact live diagnostics below the preview.
- Add V3 knob-centered tracking based on sparse pyramidal Lucas–Kanade optical flow. V3 follows features on the physical knob itself, skips whole-frame background stabilization, and does not compare per-gear image templates. Clicking the live preview initializes or reacquires the knob.
- Keep V1 fixed-camera and V2 background-alignment engines selectable. Keep the V1/V2 shift-output protocols separate from the tracking-engine choice.
- Add English/German dashboard language selection and Timexo footer links for PayPal and Gumroad.
- Upgrade the optional virtual Xbox gamepad to the pinned, SHA-256-verified HIDMaestro v2.0.0 SDK. The wrapper now invokes v2's idempotent driver-install/migration path; review the first-upgrade notice and close older v1 ShifterTracker instances before starting it.
- Ship the app, HIDMaestro, .NET and Apache license/notice files in the release ZIP and attach them separately, so direct EXE users can download them too.

## 1.0.1 — 2026-10-16 — Stable patch

- Show the main window before probing cameras; move DirectShow enumeration to a background STA thread so a slow device does not block startup.
- Add visible startup/unhandled-error dialogs and a persistent `%LOCALAPPDATA%\ShifterTracker\startup.log` for diagnosis.
- Populate output-mode choices before selecting a default and guard all ComboBox selections against empty/out-of-range lists; fixes the reported startup `SelectedIndex` crash in the input page.

## 1.0.0 — 2026-10-16 — Stable

- First stable release in the `realTimexo/ShifterTracker` repository.
- Added adaptive V2 alignment to tolerate small camera translation, rotation and scale changes before gear classification.
- Preserved V1 fixed-camera tracking and existing keyboard/vJoy output paths.
- Added an optional bundled Xbox 360-compatible virtual gamepad path with in-app consent and Windows administrator approval.
- Added persistent local settings, H-pattern calibration, V2 neutral-lane routing, a self-contained Windows x64 build, third-party notices and automated tagged releases.

### Release history note

This repository was empty when the 1.0.0 work began: it had no commits, tags or GitHub releases. Earlier prototype builds existed outside this repository, but there were no published assets or tag metadata to verify their order and exact source. They are therefore not retroactively labeled as alpha/beta releases. Future semantic-version tags must match the project version; any prerelease suffix (such as `-alpha.1`, `-beta.2` or `-rc.1`) is automatically published as a GitHub pre-release.
