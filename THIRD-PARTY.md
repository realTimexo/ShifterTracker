# Third-party software and notices

ShifterTracker source code is licensed under the MIT License in `LICENSE`. Windows releases include applicable notices in the ZIP. Standalone license files for ShifterTracker, HIDMaestro, .NET and Apache-2.0, the HIDMaestro and .NET third-party notices, and this consolidated summary are also attached to each GitHub release, including releases that offer a standalone EXE.

| Component | Use | License / notice |
|---|---|---|
| HIDMaestro v2.0.0 | Optional virtual Xbox 360-compatible gamepad | MIT; upstream `LICENSE` and `THIRD-PARTY-NOTICES.txt` are included. The build fetches the upstream release archive pinned at SHA-256 `9678ca1e7d2b689af99d353677403f644f9948bd0553598c953f80e4333a8479` and verifies the extracted Core DLL hash. |
| OpenCvSharp4 / OpenCvSharp4.Extensions | Managed OpenCV API and bitmap conversion | Apache-2.0, as declared in NuGet metadata; full text ships as `Apache-2.0.txt`. |
| OpenCvSharp4.runtime.win | Windows native OpenCV runtime | Apache-2.0, as declared in NuGet metadata; full text ships as `Apache-2.0.txt`. |
| .NET 10 Windows Desktop Runtime | WinForms and platform runtime | Microsoft's `LICENSE.txt` and `ThirdPartyNotices.txt`, copied from the .NET distribution used for the build, ship in the ZIP and as separate release assets. |
| vJoy | Optional DirectInput output | Not bundled. Install and license vJoy separately from its maintainers if you select this output mode. |

Project references:

- OpenCvSharp: <https://github.com/shimat/opencvsharp>
- HIDMaestro and its v2 migration guide: <https://github.com/hifihedgehog/HIDMaestro> · <https://hidmaestro.org/docs/start/migration/>
- vJoy: <https://github.com/shauleiz/vJoy>

The virtual gamepad is optional. Its driver lifecycle is managed by HIDMaestro and may require Windows administrator approval, including a one-time driver migration after upgrading from HIDMaestro 1.x. Review the in-app notice and Windows prompts. The vJoy driver is never installed by ShifterTracker.
