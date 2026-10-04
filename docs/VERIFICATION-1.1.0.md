# Auto-Hotkeys 1.1.0 verification

Verified on Windows on 2026-10-04. The existing C#/Windows Forms stack is preserved, as requested by the standing app-development instructions. The outside-in geometry adapts the stretch reference from GeorgeFejer91/uncodixfy-pretext using native text measurement.

## Build and regression checks

- `scripts/build.ps1 -InnoCompiler <Inno Setup 6.7.3 ISCC.exe>`: standalone executable and per-user installer compiled successfully.
- `scripts/test.ps1`: 55 checks passed. They cover quiet/default startup, explicit `--show`, settings defaults/persistence/recovery, physical startup paths, frozen-image cropping, clipboard Bitmap/PNG pixels, the action catalog, native resize geometry, pagination, enlarged text, and close-to-tray behavior.
- Native client-size fixtures: 640×440, 730×520, 1200×440, 640×900, and 1200×900. At each size, control groups and footer actions fit, primary instructions remain visible, groups do not overlap, and the main panel has no scrollbar. Additional height increases inter-group spacing; type grows only when both dimensions allow it.
- The fixture also checks seven actions using three-row pages, bounded recent activity, and explicitly enlarged 20-point text with a larger native minimum window size. Native panel renders were inspected at compact and expanded sizes.
- `git diff --check`: no whitespace errors.

## Installed runtime

- Inno Setup upgrade ran through Windows Task Scheduler to avoid the calling packaged application's AppData redirection. Installer exit result: 0.
- Installed executable SHA-256 matches the build executable: `6f7c989c153979f3b38823f130188561edbe35d221251985214663596296ea3f`.
- Installer SHA-256: `cbaece0fe067f2da8adc617766c6b119d9c9c577b45e7dcc4fb57c37bef1399f`.
- Installed at `%LOCALAPPDATA%\Programs\Auto-Hotkeys\Auto-Hotkeys.exe`, version 1.1.0.0.
- A native first launch with no arguments stays in the background. A second ordinary launch keeps the same instance hidden. `--show` opens the existing management window. A clean stop and native restart leave it hidden again.
- Runtime log reports `Alt + S: Active`. Settings retain `AutoStart: true` and the screenshot action enabled. Both sign-in and minute recovery tasks point at the physical installed executable with `--background`.
- Shutdown/restart exposed a pre-existing repeated-disposal exception; the application context now disposes its native resources only once, and the final shutdown/restart checks completed without that exception.
- Temporary installer and verification tasks were removed. The app remains running in the background.

## Limits

The checks do not claim browser/Pretext verification, a Rust/Tauri migration, phone layouts, localized translations, or every per-monitor DPI configuration. Physical Alt+S drag-and-paste interaction was not repeated in this update; registration and frozen-image/clipboard regression paths were checked. The installer remains unsigned. Full activity and unbounded action details use separate native dialogs with their own scrolling policy.
