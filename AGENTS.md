# Auto-Hotkeys development

George's defaults for new desktop apps are Rust/Tauri 2 and a small TypeScript frontend. Read the personal `tauri-rust-developer` skill for Tauri/native work. Preserve this existing C#/Windows Forms implementation unless a migration is explicitly requested.

The authoritative UI framework is GeorgeFejer91/uncodixfy-pretext, installed as the personal `uncodixfy-pretext` skill. Read its SKILL.md, references/pretext.md, and references/accordion-stretch.md before HTML UI work. Its stretch layout is outside-in: allocate the bounded window, keep header/footer anchors and ordered groups, and distribute extra space between groups as both dimensions change. It is not a collapsible-section layout. The main panel should not scroll; give unbounded data a detail/pagination policy. Adapt this geometry using native text measurement for the current native UI; do not add or claim browser Pretext measurement in Windows Forms. HTML work requires the skill's locked Pretext dependency and actual measurements. Keep focus and keyboard order clear.

Auto-Hotkeys is a background utility. Normal startup, installation, and sign-in must leave its management window hidden. Open it only through the tray Open command or explicit `--show`. Closing it hides the window while hotkeys remain active. Fresh installation defaults Start with Windows to On, with a working toggle and uninstall cleanup.

Preserve Alt+S frozen rectangular screenshot-to-clipboard behavior and saved action IDs during changes. Test real Windows startup and installation paths; a packaged host can virtualize AppData, so scheduled tasks must point to physical files. Do not upload screenshots or local activity logs.
