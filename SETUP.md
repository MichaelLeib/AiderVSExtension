# Aider Visual Studio Extension - Setup Guide

This guide will walk you through setting up the Aider Visual Studio Extension from source code on a Windows machine.

## Prerequisites

Before starting, ensure you have:
- Windows 10/11 (64-bit)
- Administrative privileges for software installation
- Internet connection for downloading dependencies

## Step 1: Install Visual Studio 2022

1. **Download Visual Studio 2022**
   - Go to https://visualstudio.microsoft.com/downloads/
   - Download Visual Studio 2022 (Community, Professional, or Enterprise)

2. **Install with Required Workloads**
   - Run the installer
   - Select the following workloads:
     - ✅ **.NET desktop development**
     - ✅ **Visual Studio extension development**
   - In Individual Components, ensure these are selected:
     - ✅ **.NET Framework 4.7.2 targeting pack**
     - ✅ **Visual Studio SDK**
   - Click **Install**

## Step 2: Install Git

1. **Download Git for Windows**
   - Go to https://git-scm.com/download/win
   - Download and install Git for Windows
   - Use default settings during installation

## Step 3: Install Python

1. **Download Python 3.8+**
   - Go to https://www.python.org/downloads/windows/
   - Download Python 3.8 or later (3.11+ recommended)

2. **Install Python**
   - Run the installer
   - ✅ **IMPORTANT**: Check "Add Python to PATH"
   - Click "Install Now"

3. **Verify Installation**
   ```cmd
   python --version
   pip --version
   ```

## Step 4: Install Aider

1. **Open Command Prompt as Administrator**
   - Press `Win + X`
   - Select "Command Prompt (Admin)" or "PowerShell (Admin)"

2. **Install Aider**
   ```cmd
   pip install aider-chat
   ```

3. **Verify Aider Installation**
   ```cmd
   aider --version
   ```

## Step 5: Get the Source Code

1. **Clone the Repository**
   ```cmd
   cd C:\
   mkdir Development
   cd Development
   git clone https://github.com/your-repo/Aider-VS.git
   cd Aider-VS
   ```
   
   *Note: Replace `https://github.com/your-repo/Aider-VS.git` with the actual repository URL*

## Step 6: Build the Extension

1. **Open Visual Studio 2022**
   - Launch Visual Studio 2022

2. **Open the Solution**
   - File → Open → Project/Solution
   - Navigate to your cloned directory (e.g., `C:\Development\Aider-VS`)
   - Select `AiderVSExtension.sln`
   
   *Note: The solution file (`AiderVSExtension.sln`) should be in the root directory of the cloned repository*

3. **Restore NuGet Packages**
   - Right-click on the solution in Solution Explorer
   - Select "Restore NuGet Packages"
   - Wait for packages to restore

4. **Set Build Configuration**
   - Change build configuration from "Debug" to "Release"
   - Use the dropdown in the toolbar

5. **Build the Solution**
   - Build → Build Solution (or press `Ctrl+Shift+B`)
   - Wait for build to complete successfully

## Step 7: Install the Extension

1. **Locate the VSIX File**
   - Navigate to your project directory: `C:\Development\Aider-VS\AiderVSExtension\bin\Release\`
   - Find `AiderVSExtension.vsix`

2. **Install the Extension**
   - Double-click `AiderVSExtension.vsix`
   - Click "Install" in the VSIX Installer dialog
   - Close Visual Studio if prompted

3. **Restart Visual Studio**
   - Close and reopen Visual Studio 2022

## Step 8: Configure the Extension

1. **Open Extension Settings**
   - Tools → Options → Aider VS Extension

2. **Configure AI Provider**
   - Choose your preferred AI provider:
     - **ChatGPT**: Requires OpenAI API key
     - **Claude**: Requires Anthropic API key  
     - **Ollama**: Requires local Ollama installation

3. **Set API Keys (if using ChatGPT or Claude)**
   - Enter your API key in the configuration
   - API keys are encrypted and stored securely

## Step 9: Set Up AI Provider (Choose One)

### Option A: ChatGPT (OpenAI)
1. Go to https://platform.openai.com/api-keys
2. Create an API key
3. Add billing information to your OpenAI account
4. Enter the API key in Visual Studio: Tools → Options → Aider VS Extension

### Option B: Claude (Anthropic)
1. Go to https://console.anthropic.com/
2. Create an account and get an API key
3. Enter the API key in Visual Studio: Tools → Options → Aider VS Extension

### Option C: Ollama (Local)
1. **Install Ollama**
   - Go to https://ollama.ai/
   - Download and install Ollama for Windows

2. **Install a Model**
   ```cmd
   ollama pull llama2
   ```

3. **Start Ollama Service**
   ```cmd
   ollama serve
   ```

## Step 10: Verify Installation

1. **Open Aider Chat Window**
   - View → Other Windows → Aider Chat
   - Or use Extensions menu

2. **Test the Extension**
   - Open a code file in Visual Studio
   - Right-click in the editor → "Add to Aider Chat"
   - Type a message in the chat window
   - Verify you get a response from your configured AI

## Troubleshooting

### Build Errors
- **Missing .NET Framework**: Install .NET Framework 4.7.2 or later
- **Missing Visual Studio SDK**: Re-run VS installer and add "Visual Studio extension development"
- **Package conflicts**: Delete `bin` and `obj` folders, then rebuild

### Extension Not Loading
- Check Extensions → Manage Extensions → Installed
- Ensure "Aider VS Extension" is enabled
- Restart Visual Studio

### Aider Connection Issues
- **Python not found**: Reinstall Python with "Add to PATH" checked
- **Aider not installed**: Run `pip install aider-chat` as administrator
- **API key issues**: Verify your API key is correct and has sufficient credits

### Network Issues
- **Corporate firewall**: Configure proxy settings in Visual Studio
- **Certificate errors**: The extension includes certificate pinning for security

## Development Mode

For development and debugging:

1. **Set Startup Project**
   - Right-click `AiderVSExtension` project → Set as Startup Project

2. **Debug the Extension**
   - Press `F5` to launch experimental instance
   - This opens a new Visual Studio with your extension loaded
   - Make changes and rebuild to see updates

## Security Features

The extension includes several security enhancements:
- ✅ **Input validation** and sanitization
- ✅ **Certificate pinning** for API connections
- ✅ **Secure credential storage** using Windows DPAPI
- ✅ **HTTPS enforcement** for external connections
- ✅ **JSON deserialization protection** against attacks

## Support

If you encounter issues:
1. Check the Visual Studio Output window (View → Output → Aider VS Extension)
2. Review the troubleshooting section above
3. Check the project repository for known issues
4. Ensure all prerequisites are correctly installed

## Next Steps

Once installed:
- Explore the chat interface features
- Try the context menu "Add to Aider Chat" on code files
- Use `#` in chat to reference files and folders
- Configure AI completion settings for enhanced coding assistance

The extension is now ready to enhance your development workflow with AI-powered assistance!