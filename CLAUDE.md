# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an **Aider Visual Studio 2022 Extension** project that integrates AI-powered development capabilities into Visual Studio. The extension provides a chat interface with contextual file referencing, visual diff display, and AI-powered tab completion using configurable models (ChatGPT, Claude, Ollama).

**Architecture**: The extension implements a **client-server model** using a user-installed AgentAPI wrapper for communication with user-installed Aider dependencies.

## Architecture

The project follows a **modular Visual Studio extension architecture** with clear separation of concerns:

- **UI Layer**: WPF-based components for chat interface and visual integration
- **Service Layer**: Business logic and AI service orchestration via AgentAPI wrapper
- **Integration Layer**: Visual Studio API interactions and event handling
- **Configuration Layer**: Settings management and persistence
- **Setup Layer**: Dependency detection and user-guided installation

Key architectural patterns:
- **Dependency Injection**: ServiceContainer manages service lifetime and dependencies
- **Interface-driven Design**: All major components implement well-defined interfaces
- **Client-Server Model**: User-installed AgentAPI server with user-installed Aider dependencies
- **Process Management**: Managed AgentAPI server with HTTP communication layer
- **Circuit Breaker**: Resilient communication with comprehensive error recovery

## Development Commands

### Build Commands
```bash
# Build the extension
msbuild AiderVSExtension/AiderVSExtension.csproj

# Build in Debug mode  
msbuild AiderVSExtension/AiderVSExtension.csproj /p:Configuration=Debug

# Build in Release mode
msbuild AiderVSExtension/AiderVSExtension.csproj /p:Configuration=Release
```

### Testing and Validation
```bash
# Run project structure validation
python validate_project_structure.py
```

### Debugging
The extension can be debugged by:
1. Setting the startup project to the extension project
2. The project is configured to launch Visual Studio experimental instance (`/rootsuffix Exp`)

## Core Components

### Interface Architecture
All major services are defined through interfaces in `AiderVSExtension/Interfaces/`:
- `IAiderService`: Core Aider AI communication service (AgentAPI wrapper)
- `IAgentApiService`: AgentAPI server process management and HTTP communication
- `IAiderDependencyChecker`: Python and Aider dependency validation
- `IAiderSetupManager`: User setup flow orchestration
- `IConfigurationService`: Settings and user preferences management
- `IFileContextService`: File and solution context extraction  
- `IAIModelManager`: AI model configuration and switching
- `ICompletionProvider`: AI-powered tab completion
- `IErrorHandler`: Error handling and recovery
- `IMessageRenderer`: Chat message rendering with syntax highlighting
- `ITelemetryService`: Telemetry and performance monitoring
- `ICorrelationService`: Request correlation and tracking
- `ICircuitBreakerService`: Fault tolerance and resilience patterns

### Data Models  
Located in `AiderVSExtension/Models/`:
- `ChatMessage`: Chat conversation data with file references
- `FileReference`: File context with line numbers and content
- `AIModelConfiguration`: AI provider settings and credentials
- `DiffChange`: Visual diff representation for code changes
- `AgentApiRequest`: HTTP request data for AgentAPI communication
- `AgentApiResponse`: HTTP response data from AgentAPI
- `AiderDependencyStatus`: Python and Aider installation status
- `AgentApiConfig`: AgentAPI server configuration and timeouts

### Service Management
- `ServiceContainer`: Dependency injection container for service lifecycle management
- Services are registered and resolved through the container pattern

## Key Features Implementation

### Chat Interface
- Dockable tool window with WPF UserControl
- Context menu triggered by # key for resource referencing
- Support for files, clipboard, git branches, web search, and documentation
- Automatic dependency checking and setup prompting
- Manual setup dialog trigger with progress tracking

### Editor Integration  
- Text selection handler for "Add to Chat" functionality
- Visual diff display with red (removed) and green (added) line highlighting
- Error integration with "Fix with Aider" quick fix actions

### AI Completion
- IAsyncCompletionSource implementation for AI-powered tab completion
- Multi-provider support with fallback to standard IntelliSense
- Configurable AI models and endpoints

### Dependency Management
- Automatic Python and Aider detection during extension startup
- Interactive setup dialog with Visual Studio theming
- Guided installation via pip with progress tracking
- Graceful degradation when dependencies are unavailable
- Manual setup triggering from chat interface

### AgentAPI Integration
- User-installed AgentAPI server from https://github.com/coder/agentapi
- Local HTTP server management on port 3284
- Process lifecycle management with cleanup
- Circuit breaker patterns for resilient communication
- Comprehensive telemetry and correlation tracking

## Configuration

Extension settings are managed through Visual Studio Settings Store:
- AI model selection and API keys
- Chat interface preferences  
- Diff visualization options
- Completion behavior settings

## Important Implementation Notes

- The extension targets **.NET Framework 4.7.2** and **Visual Studio 2022 (17.0+)**
- Uses **LibGit2Sharp** for Git integration and **Newtonsoft.Json** for serialization
- Follows Visual Studio's theming and styling guidelines for native IDE integration
- Implements comprehensive error handling with graceful degradation when AI services are unavailable
- **Client-Server Model**: Both AgentAPI and Aider installed by user
- **Process Management**: Extension manages AgentAPI server lifecycle with proper cleanup
- **Dependency Validation**: Automatic Python/Aider detection with guided setup flow
- **Resilient Communication**: Circuit breaker patterns and retry logic for stability

## Project Structure

```
AiderVSExtension/
├── Interfaces/          # Service interface definitions
├── Models/             # Data models and enums  
├── Services/           # Service implementations
├── UI/                 # WPF controls and dialogs
│   ├── Chat/           # Chat interface components
│   └── AiderSetupDialog.xaml # Dependency setup UI
├── Properties/         # Assembly information
├── tools/              # AgentAPI installation guide
├── AiderVSExtension.csproj    # Project file with dependencies
├── AiderVSExtensionPackage.cs # Main extension package
└── source.extension.vsixmanifest # VSIX manifest
```

## Development References

- **Design Document**: `design.md` - Comprehensive architecture and component details
- **Requirements Document**: `requirements.md` - Feature requirements and acceptance criteria  
- **Implementation Plan**: `tasks.md` - Detailed development task breakdown
- **Validation Script**: `validate_project_structure.py` - Project structure verification