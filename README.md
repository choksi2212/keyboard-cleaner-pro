# Keyboard Cleaner Pro

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4?style=for-the-badge&logo=windows" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" />
</p>

A lightweight, enterprise-grade Windows utility that **truly disables your laptop's internal keyboard** at the hardware/driver level — not just a keyboard hook — so you can safely clean it without accidental keystrokes.

Built with **C# .NET 9 + WPF** and calling **Windows SetupAPI + PnP Manager** directly.

---

## ✨ Features

- 🔒 **True hardware disable** via `SetupDiCallClassInstaller` / `DIF_PROPERTYCHANGE`
- 🔍 **Smart device detection** — scoring model (ACPI +50, PS/2 +40, I2C +30, USB/BT excluded)
- ⏱ **Auto-unlock timer** — configurable 1–30 min countdown
- 🛡️ **Multi-method recovery** — in-process timer + startup check + Windows Scheduled Task
- 💥 **Crash-safe** — state persisted atomically before disable
- 📋 **Rolling log files** — `%ProgramData%\KeyboardCleaner\Logs`
- 🎨 **Windows 11 Fluent dark UI** — custom chrome, animated toggle button

---

## 🏗️ Architecture

```
KeyboardCleanerPro.sln
├── src/
│   ├── KeyboardCleanerPro.Core/     # Service interfaces + implementations
│   │   ├── Infrastructure/Native/   # SetupAPI + CfgMgr32 P/Invoke
│   │   ├── Models/                  # Domain models (immutable)
│   │   └── Services/                # Detection, Control, Recovery, Logging, Config, State
│   ├── KeyboardCleanerPro/          # WPF app (MVVM)
│   └── KeyboardCleanerPro.Recovery/ # Console recovery agent
└── tests/
    └── KeyboardCleanerPro.Tests/    # xUnit + FluentAssertions (no mocks)
```

---

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET 9 Runtime
- **Run as Administrator** (required for SetupAPI device control)

### Build
```powershell
dotnet build KeyboardCleanerPro.sln
```

### Run Tests
```powershell
dotnet test tests/KeyboardCleanerPro.Tests/
```

### Run App
```powershell
dotnet run --project src/KeyboardCleanerPro/
```

---

## 🧪 Testing

All tests are **production-real** — no mocks, stubs, or fake logic:

| Test Class | Type | Coverage |
|---|---|---|
| `DeviceScorerTests` | Unit | All 5 scoring paths + guard clauses + case sensitivity |
| `OperationResultTests` | Unit | Success/failure factories, error propagation |
| `KeyboardDeviceTests` | Unit | Equality, immutability, `WithEnabled` |
| `AppSettingsTests` | Unit | Validation, clamping, defaults |
| `ConfigurationServiceTests` | Integration | File I/O, round-trip, corruption recovery |
| `StateManagerTests` | Integration | Atomic write, overwrite, corruption, computed props |
| `LoggingServiceTests` | Integration | Real file writes, buffer, async |
| `RecoveryServiceTests` | Integration | Timer fire, cancel, guard clauses |
| `KeyboardDetectionServiceIntegrationTests` | System | Real SetupAPI calls |

---

## 🔒 Security

- UAC `requireAdministrator` in app manifest
- All device operations require elevated token
- No telemetry, no network calls, no data collection

---

## 👤 Author

**Manas Choksi**

- GitHub: [@choksi2212](https://github.com/choksi2212)

---

## 📜 License

MIT License — see [LICENSE](LICENSE)
