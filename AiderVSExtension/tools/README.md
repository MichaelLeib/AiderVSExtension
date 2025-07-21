# AgentAPI Installation Guide

This directory was originally intended for bundled AgentAPI binaries, but the extension now expects AgentAPI to be installed separately by the user.

## Installation Instructions

AgentAPI is required for the Aider VS Extension to communicate with Aider. Please install it using one of the following methods:

### Method 1: Download from GitHub Releases (Recommended)

1. Visit https://github.com/coder/agentapi/releases
2. Download the appropriate binary for your platform:
   - Windows: `agentapi_windows_amd64.exe` (rename to `agentapi.exe`)
   - macOS: `agentapi_darwin_amd64` or `agentapi_darwin_arm64` (rename to `agentapi`)
   - Linux: `agentapi_linux_amd64` (rename to `agentapi`)
3. Make the binary executable (macOS/Linux): `chmod +x agentapi`
4. Add the binary to your system PATH

### Method 2: Install via Package Manager

**macOS (Homebrew):**
```bash
brew tap coder/tap
brew install agentapi
```

**Windows (Chocolatey):**
```powershell
choco install agentapi
```

**Linux:**
Follow the manual download method above or use your distribution's package manager if available.

## Verification

After installation, verify AgentAPI is accessible:
```bash
agentapi --version
```

## Configuration

The extension will automatically detect AgentAPI if it's in your system PATH. You can also specify a custom path in the extension settings.

## Troubleshooting

If the extension cannot find AgentAPI:
1. Ensure AgentAPI is in your system PATH
2. Restart Visual Studio after installation
3. Check the extension output window for detailed error messages
4. Manually configure the AgentAPI path in extension settings

## Required Dependencies

In addition to AgentAPI, ensure you have:
- Python 3.8 or higher
- Aider installed via pip: `pip install aider-chat`

For more information about AgentAPI, visit: https://github.com/coder/agentapi