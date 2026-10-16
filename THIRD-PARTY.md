# Third-party software and notices

ShifterTracker's own source code is licensed under the MIT License in `LICENSE`. The Windows release bundles or uses the following separately licensed components:

| Component | Use | License / notice |
|---|---|---|
| HIDMaestro v1.12.0 | Optional virtual Xbox 360-compatible gamepad | MIT; the upstream license and third-party notices are included beside the executable and in `src/ShiftTracker/ThirdParty/`. The upstream release archive is pinned and SHA-256 verified by the build script. |
| OpenCvSharp4 / OpenCvSharp4.Extensions | Managed OpenCV API and bitmap conversion | Apache-2.0, as declared in the NuGet package metadata; the full license text is included as `Apache-2.0.txt` in the release ZIP. |
| OpenCvSharp4.runtime.win | Windows native OpenCV runtime | Apache-2.0, as declared in the NuGet package metadata; the full license text is included as `Apache-2.0.txt` in the release ZIP. |
| .NET 10 Windows Desktop Runtime | WinForms and platform runtime | Microsoft .NET runtime license; the self-contained release uses Microsoft's standard .NET distribution. |
| vJoy | Optional DirectInput output | Not bundled. Install and license vJoy separately from its maintainers if you choose this output mode. |

OpenCvSharp project: <https://github.com/shimat/opencvsharp>

HIDMaestro project and license: <https://github.com/hifihedgehog/HIDMaestro>

vJoy project: <https://github.com/shauleiz/vJoy>

The virtual gamepad is optional. Its UMDF driver and local signing certificate are installed only after the user chooses that output, confirms the in-app notice, and approves Windows' administrator prompt. The vJoy driver is never installed by ShifterTracker.
