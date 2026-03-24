# Oolio Tunnel Monitor

A lightweight Windows system tray application for managing and monitoring Cloudflare Zero Trust tunnels at customer sites.

Built for Oolio Group field technicians — provides a single-pane view of tunnel health, service status, and published routes without needing to log in to the Cloudflare dashboard.

---

## Features

- **Real-time tunnel status** — service state, tunnel health (healthy/degraded/inactive) and published routes via the Cloudflare API
- **One-click Repair** — automated stop, uninstall, reinstall MSI, and re-register token workflow
- **Install New Tunnel** — guided form to create, configure and install a new tunnel from scratch
- **HTTP endpoint check** — verifies Cloudflare connectivity even without an API token
- **Daily update check** — automatically checks for a newer version on startup (once per day) and prompts to download
- **System tray** — runs minimised and silently in the system tray; starts minimised on launch

---

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — download and install if not already present
- Cloudflare API token with **Tunnel: Edit** scope — found in LastPass or the HubSpot Company Record under Network & Environment

---

## Installation

1. Download `TunnelMonitor.exe` from the [latest release](https://github.com/BenWfromBepoz/OolioTunnelMonitor/releases/latest)
2. Place it somewhere permanent, e.g. `C:\Program Files\Oolio\TunnelMonitor\`
3. Run it — the Oolio icon will appear in the system tray
4. To start automatically on login, add a shortcut to `shell:startup`

---

## Usage

| Action | How |
|---|---|
| Open the app | Double-click the tray icon, or right-click → Open |
| Check tunnel status | Click **Check Tunnel Status** in the sidebar — also runs automatically on open |
| Repair a broken tunnel | Paste the API token into the token field, then click **Repair Tunnel** |
| Install a new tunnel | Click **Install New Tunnel** and fill in the form |
| Check for updates | Click **Check for Updates** in the sidebar — also runs silently once per day on startup |

---

## API Token

The Cloudflare API token is required for Repair and Install operations. It needs the **Cloudflare Tunnel: Edit** permission scope.

Where to find it:
- **LastPass** — search for the customer site name
- **HubSpot** — open the Company Record → Network & Environment section

---

## How Updates Work

On startup, the app silently checks `version.json` in this repository once per day. If a newer version is available, a prompt appears offering to open the download page. Clicking **Check for Updates** in the sidebar runs the check immediately regardless of the daily throttle.

---

## Release Notes

See [Releases](https://github.com/BenWfromBepoz/OolioTunnelMonitor/releases) for full version history.

---

## Built by

Oolio Group — internal tooling for Bepoz field operations.
