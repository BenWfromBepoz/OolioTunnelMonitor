# Cloudflared Monitor

Cloudflared Monitor is a small Windows utility designed to help operations
teams diagnose and repair Cloudflare Zero Trust tunnels running on point of
sale (POS) servers.  It presents a simple system tray icon and window to
summarise the health of the local `cloudflared` service and its
corresponding Cloudflare tunnel, collect diagnostics, and reinstall
cloudflared when necessary.

## Features

* **One‑click diagnostics** – display the Windows service state, tunnel name,
  tunnel ID, remote status and ingress mappings.
* **Repair existing tunnel** – stops the service, kills any stray
  `cloudflared.exe` processes, deletes the Windows service, optionally
  reinstalls the latest `cloudflared` MSI, fetches a fresh tunnel token
  through the Cloudflare API, installs the service and restarts it.
* **Log to file** – all actions and diagnostics are logged to a file under
  `%ProgramData%\Bepoz\CloudflaredMonitor\logs` with rotation by date.
* **Export diagnostics** – generate a ZIP bundle with status, ingress
  information, UI logs and the persistent log file for easy attachment to
  support tickets.

## Configuration

Configuration values live in `Services/AppConfig.cs` and include:

* `AccountId` – the Cloudflare account ID that owns your tunnels.
* `Key` / `EncryptedApiToken` – the AES key and encrypted API token used
  to authenticate API calls.  See below for instructions on generating
  these values.
* `CloudflaredMsiUrl` – link to the latest cloudflared installer.
* `ServiceName` – Windows service name for cloudflared.

### Generating the encrypted token

The Cloudflare API token must be encrypted before being embedded in the
repository.  Use PowerShell on a secure workstation to generate the
encrypted string.

```powershell
$token = "YOUR_API_TOKEN"               # token without the "Bearer" prefix
$key   = [byte[]](11,92,33,54,76,21,44,87,91,62,17,203,44,56,78,19,22,89,120,45,65,11,98,74,31,44,58,73,92,10,44,61)
$secure = ConvertTo-SecureString $token -AsPlainText -Force
ConvertFrom-SecureString $secure -Key $key
```

Replace `EncryptedApiToken` in `AppConfig.cs` with the output of the last
command.  Keep the key and encrypted token secret.

## Building

The project targets .NET 8 and Windows.  To build it:

```bash
dotnet restore
dotnet build -c Release
```

You can then run `CloudflaredMonitor.exe` from the build output directory.

When compiled with the included `app.manifest`, the executable will
request administrative privileges on launch.  This is necessary to stop
services, kill processes and install software.

## Limitations

* The tool assumes the tunnel is installed as a Windows service with a
  token passed via the `--token` argument.  If your deployment uses a
  different pattern (e.g. local `config.yml`), the tunnel ID may not be
  discoverable and the repair flow will not work.
* API token decryption uses `powershell.exe` at runtime.  PowerShell must
  be available on the host.  A future enhancement could embed a pure C#
  decryptor.

## License

This project is provided as‑is for internal use.  See `LICENSE` for
license terms.