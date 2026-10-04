# Auto-Hotkeys

A small Windows app that keeps useful keyboard actions available in the background.

**Alt + S** freezes a screenshot of the current desktop. Drag to choose a rectangle, release to copy the cropped image to the clipboard, then paste with **Ctrl + V**. **Esc** cancels. Capture works across monitors and supports dragging in either direction.

## Install and use

Download **Auto-Hotkeys-Setup-1.1.0.exe** from [Releases](https://github.com/GeorgeFejer91/Auto-Hotkeys/releases/latest) and run it. Installation is for your Windows account and does not need administrator rights. Windows 10/11 with .NET Framework 4.8 is required; current versions include it. This release is unsigned, so Windows may show an unknown-publisher or SmartScreen prompt.

**Start with Windows is On by default.** Installation, sign-in, and ordinary launches start quietly in the notification area. Double-click the tray icon or choose **Open Auto-Hotkeys** to see settings. Change automatic startup in the app window or tray menu. Closing the window hides it; hotkeys keep working. The installer's optional Open settings checkbox starts unchecked.

The app window lists all actions, their shortcuts, On/Off switches, registration status, and recent activity. An unavailable shortcut is retried every ten seconds, and the app shows the conflict instead of silently claiming it works. The first version includes one action: rectangular screenshot to clipboard.

The window uses an outside-in stretch layout: header and action anchors stay in place while extra width and height spread between ordered groups. The main panel has no scrollbar. Actions use pages of three when needed; the latest three activity results stay in view. **Details** and **Activity history** provide the complete text in separate dialogs.

With autostart On, Windows Task Scheduler starts the app at sign-in and restores it within about a minute if it unexpectedly stops. Battery power and long runtimes do not stop it. **Quit until next sign-in** temporarily stops recovery while preserving automatic startup for the next sign-in. Autostart Off removes both startup tasks; the app can still run when opened manually.

Screenshots are copied locally; the app does not upload them or save image files. Settings and a size-limited activity log are stored under `%LOCALAPPDATA%\Auto-Hotkeys`. Logs contain action results and dimensions, not screenshot contents. Uninstall through Windows Settings → Apps; uninstall removes the startup tasks and stops the background process. User settings are retained for a future reinstall.

Hotkeys apply to the normal Windows desktop, including while administrator apps have focus. Lock screens, UAC's secure desktop, and protected video content are controlled by Windows. An application that exclusively intercepts input may affect shortcuts.

## Build

The app uses C#, Windows Forms, and Windows' `RegisterHotKey` API. It does not require AutoHotkey, a separate Snipping Tool process, npm, or NuGet packages. The output is a standalone executable using Windows' .NET Framework runtime.

On Windows:

```powershell
# Get a pinned, checksum-verified Inno Setup compiler if needed.
.\scripts\get-build-tools.ps1

# Build the app and standard Windows installer.
.\scripts\build.ps1

# Run the regression checks. Clipboard is restored after the clipboard check.
.\scripts\test.ps1
```

To use an existing compiler: `scripts\build.ps1 -InnoCompiler 'C:\path\to\ISCC.exe'`. For only the executable: `scripts\build.ps1 -AppOnly`.

Outputs: `build\Auto-Hotkeys.exe`, `dist\Auto-Hotkeys-Setup-1.1.0.exe`, and `dist\SHA256SUMS.txt`.

An optional GitHub Actions template is included in `ci/windows-build.yml`. To enable it, copy it to `.github/workflows/build.yml` using a GitHub login with workflow permission. It builds and tests pushes and pull requests, and version tags attach the installer and checksum to a GitHub Release. The initial release was built and checked locally. Source and tools are separate from generated binaries.

## Add another action

1. Add a subclass of `HotkeyAction` under `src/AutoHotkeys` with a stable `Id`, display `Label`, `Shortcut`, modifier flags, virtual key code, and `Execute` implementation.
2. Add its instance to `ActionCatalog.Create()` in `HotkeyManager.cs`.
3. Rebuild. The action automatically appears in the list and tray menu, receives a global hotkey, and respects its saved On/Off setting.

Screenshot capture, clipboard transfer, action registration, settings, startup, and the window are separate components. Keep action IDs stable so existing user settings remain meaningful. Use `completed(result)` to report activity. Any future action involving external services should make its behavior clear to the user.

## Development commands

`Auto-Hotkeys.exe` and `Auto-Hotkeys.exe --background` start without showing the window, including when an instance is already running. `Auto-Hotkeys.exe --show` explicitly opens the existing management window. `--configure-autostart=on` and `--configure-autostart=off` update startup settings and exit. `--prepare-uninstall` removes startup tasks and signals the running app to stop.

This existing release keeps its C#/Windows Forms stack. It adapts the geometry from [George's uncodixfy-pretext framework](https://github.com/GeorgeFejer91/uncodixfy-pretext/blob/main/references/accordion-stretch.md) using native text measurement. It does not claim browser Pretext verification. Personal defaults for new desktop apps use Rust/Tauri 2; this repository's AGENTS.md preserves that preference without imposing an unrequested migration.

The application resolves its executable's physical file path before registering startup. This prevents the file redirection problem that can occur when an app is launched from another packaged application's environment.

MIT licensed. Installer built using [Inno Setup](https://jrsoftware.org/isinfo.php).
