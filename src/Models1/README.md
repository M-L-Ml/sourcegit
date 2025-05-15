# OS Platform Abstraction Architecture

This refactoring introduces a comprehensive interface-based architecture that follows SOLID principles to enable dependency inversion and better testability.

## Architecture Overview

The refactoring extracts interfaces for OS-specific operations into the Models1 project, creating a proper abstraction layer:

1. **IOSPlatform**: Primary facade interface that aggregates all OS-specific operations
2. **IApplicationSetup**: Interface for Avalonia application initialization
3. **IFileSystem**: Interface for file system operations
4. **IExternalTools**: Interface for discovering external tools
5. **IProcessLauncher**: Interface for launching external processes

## SOLID Principles Implementation

### Single Responsibility Principle (S)
Each interface has a specific, well-defined responsibility:
- **IApplicationSetup**: Only handles app and window setup
- **IFileSystem**: Only handles file system operations
- **IExternalTools**: Only handles external tool discovery
- **IProcessLauncher**: Only handles launching processes

### Open/Closed Principle (O)
The interfaces are open for extension but closed for modification. New functionality can be added by extending the interfaces or creating new ones.

### Liskov Substitution Principle (L)
All implementations of the interfaces (Windows, Linux, MacOS) can be substituted for the interfaces without changing the behavior of the program.

### Interface Segregation Principle (I)
Interfaces are kept small and focused. For example, file operations and process launching are separated into different interfaces.

### Dependency Inversion Principle (D)
High-level modules (like OSAbstraction) depend on abstractions (interfaces) instead of concrete implementations.

## Usage Example

```csharp
// Create a platform factory
var platformFactory = new Sausa.PlatformFactory();

// Create a platform instance using dependency injection
var platform = platformFactory.CreatePlatform();

// Create the OSAbstraction service with the platform implementation
var osService = new Sausa.OSAbstraction(platform);

// Use the service
osService.SetupDataDir();
osService.OpenBrowser("https://example.com");
```

## Migration from Original Code

The original static OS class has been replaced with a proper service class (OSAbstraction) that accepts platform implementations through dependency injection. The original functionality is preserved while enabling better testability and maintainability.
