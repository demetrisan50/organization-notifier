# Organization Notifier

The **Organization Notifier** is a professional, high-performance desktop application designed for IT administrators to send instant, eye-catching notifications across a local network or to a local machine.

## 🚀 Key Features

### 1. Modern Interactive UI (WPF)
- **Apple-Style Aesthetics**: Clean, breathable layout with professional typography and vibrant accents.
- **Dynamic Icon Selection**: Real-time thumbnail previews of notification icons.
- **Full Responsiveness**: Adaptive layout with smart scrollbars that appear when the window is resized.
- **Instant Validation**: Real-time error feedback on every field (On Change & On Blur).

### 2. Predefined Scenarios (Quick Actions)
- **5 Custom Slots**: Save your most-used notification templates (Title, Message, Icon, Duration).
- **One-Click Execution**: Launch saved notifications instantly without re-typing.
- **Auto-Persistence**: Your configurations are automatically saved and restored on startup.

### 3. Advanced PowerShell Backend
- **Silent Background Execution**: No intrusive command windows; everything runs seamlessly in the background.
- **Smart Networking**: Automatic discovery of active computers in the local subnet.
- **Native Windows Runtime API**: Uses the same technology as system alerts for 100% native toast notifications.
- **Self-Elevation**: Automatically requests Administrator privileges only when performing network-wide actions.

### 4. Deployment & Packaging
- **Portable Folder**: The `OrganizationNotifier_Ready` folder can be copied and run on any client PC immediately.
- **Professional Installer**: A dedicated `installer.iss` script for creating a standard Windows `setup.exe` with shortcuts.

---

## 🛠️ Technical Specifications
- **Framework**: .NET 9.0 (WPF)
- **Language**: C# / XAML
- **Scripting**: PowerShell 7+
- **Configuration**: JSON-based local persistence in `%AppData%`.

## ⚙️ Setup Instructions

### For Administrators
To enable notification delivery, ensure the following commands have been run in an elevated PowerShell:

1. **Set Execution Policy**:
   ```powershell
   Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

2. **Enable Remote Management** (for network-wide alerts):
   ```powershell
   Enable-PSRemoting -Force
   ```

### For Users
1. Simply run `organization-notifier.exe`.
2. Fill out the fields and click **Send Notification**.
3. Use the **?** button in the header for the built-in Help guide anytime.

---
*Created by IT Support Team*