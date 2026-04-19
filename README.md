# Desk Of Paul — DOP

A personal Windows terminal dashboard. One executable, one config file, zero bloat.

DOP runs as a shell inside Windows Terminal — a tiled command center with a launcher, live clock, weather, system stats, and network info. No mouse required.

```
.·:''''''''''''''''''''''''''''''''''''''''''''''''''''''''':·.
: :  ____            _       ___   __   ____             _  : :
: : |  _ \  ___  ___| | __  / _ \ / _| |  _ \ __ _ _   _| | : :
: : | | | |/ _ \/ __| |/ / | | | | |_  | |_) / _` | | | | | : :
: : | |_| |  __/\__ \   <  | |_| |  _| |  __/ (_| | |_| | | : :
: : |____/ \___||___/_|\_\  \___/|_|   |_|   \__,_|\__,_|_| : :
'·:.........................................................:·'
```

---

## Panels

| Panel | What it shows |
|---|---|
| **Launcher** | Keyboard-navigable shortcut grid — arrow keys to move, Enter to launch, F1–F12 hotkeys |
| **Clock** | Live clock, 12hr or 24hr |
| **Weather** | Current conditions via [wttr.in](https://wttr.in) — no API key needed |
| **System** | CPU % and available RAM, refreshed every 3 seconds |
| **Network** | Connection status, SSID/adapter, ping latency, local IP, public IP |

---

## Keyboard

| Key | Action |
|---|---|
| `↑` / `↓` | Move through launcher |
| `Enter` | Launch selected item |
| `F1`–`F12` | Direct launch hotkeys |
| `Ctrl+T` | Cycle accent color |
| `F5` | Reload config.ini |
| `Esc` | Exit to prompt |

---

## Deploy

Publish as a single self-contained exe — no runtime install needed on the target machine.

```
dotnet publish -c Release
```

Drop both files in any folder and run:

```
/DOP
    dop.exe
    config.ini
```

---

## Configuration — config.ini

```ini
[General]
AccentColor=Cyan        ; Cyan | Green | Yellow | Red | White
GridColor=DarkGray
Background=Black
ClockFormat=12          ; 12 or 24

[Weather]
WeatherZip=78628

[Network]
PingTarget=8.8.8.8

[Links]
Link1=Visual Studio 2026 | C:\path\to\devenv.exe | Dev | F1
Link2=VS Code | C:\path\to\code.exe | Dev | F2
Link3=---
Link4=PowerShell | C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe | Terminal | F3
Link5=---
Link6=Close / Exit to Prompt | EXIT |  |
```

**Separator** — a `---` entry renders as a divider line in the launcher.

**Reload** — edit the file, press `F5` in DOP. No restart needed.

---

## Tech Stack

| | |
|---|---|
| Language | C# .NET 10 |
| TUI Framework | [Terminal.Gui v2](https://github.com/gui-cs/Terminal.Gui) |
| Weather | [wttr.in](https://wttr.in) — no key required |
| System Stats | `System.Diagnostics.PerformanceCounter` |
| Network | `System.Net.NetworkInformation` + Ping |
| Public IP | [api.ipify.org](https://api.ipify.org) |

---

## Philosophy

Every decision asks: *do we actually need this?*

- Hardcoded layout — no config bloat, no layout bugs
- No file watchers — F5 reloads on demand
- No background threads beyond what the app explicitly needs
- Single folder deployment — drop and run

---

*Built by [SINYDude](https://github.com/SINYDude)*
