# NyxLaunch

NyxLaunch is an open-source Windows game launcher built with C# and modern .NET 10. Designed to bring your PC game library together in one place, NyxLaunch provides a clean, modern interface for managing, organizing, and launching your games.

Import your Steam library, add games manually, organize your collection, and access everything through one unified launcher.

## Features

- **Steam Library Importing**: Scan and import games from your locally installed Steam library.
- **Manual Game Management**: Add non-Steam games using their executable files.
- **Unified Game Library**: Keep Steam and manually added games together in one place.
- **Game Launching**: Launch games directly from NyxLaunch.
- **Launch Options**: Configure custom launch arguments for individual games.
- **Game Collections**: Create custom collections to organize your library.
- **Recently Played**: Quickly access your recently played games.
- **Library Search**: Find games quickly with built-in search.
- **Playtime Tracking**: Track playtime for supported game entries.
- **Persistent Storage**: Automatically save your library and collection data locally.
- **Modern Interface**: Dark-themed WPF interface with purple accents and custom dialogs.

## Requirements

- **Windows 10/11** (64-bit)
- **.NET 10 SDK** (only required for building from source)
- **Steam** (optional, for Steam library importing)

## Installation

### From GitHub Releases (Recommended)

1. Visit the [NyxLaunch Releases](https://github.com/builtbynyx/NyxLaunch/releases) page.
2. Download the latest `NyxLaunch.exe`.
3. Run `NyxLaunch.exe`.

**No .NET 10 SDK or Runtime installation is required.** The released executable is self-contained and includes the required .NET runtime.

### Build from Source

```powershell
# Clone the repository
git clone https://github.com/builtbynyx/NyxLaunch.git
cd NyxLaunch

# Build the project
dotnet build

# Run the application
dotnet run --project .\src\NyxLaunch.App\NyxLaunch.App.csproj

# Publish a self-contained Windows executable
dotnet publish .\src\NyxLaunch.App\NyxLaunch.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Published files can be found in:

```text
src/NyxLaunch.App/bin/Release/net10.0-windows/win-x64/publish/
```

## Usage

### Importing Steam Games

Scan your local Steam installation to import detected games into your library.

### Adding Custom Games

Add games outside of Steam by selecting their `.exe` executable.

### Organizing Your Library

Create custom collections to group games and use the navigation and search functionality to find games quickly.

### Launch Options

Configure custom launch arguments for individual games.

## Data Storage

NyxLaunch stores application data locally in:

```text
%LOCALAPPDATA%\BuiltByNyx\NyxLaunch\
```

This directory contains your saved game library and collection data.

Your data is stored separately from the application executable, so updating or replacing NyxLaunch does not normally remove your saved games and collections.

## Technology

- **C#** — Primary programming language
- **.NET 10** — Application framework
- **WPF** — Windows desktop interface
- **XAML** — Interface layout and styling

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

**NyxLaunch** — Your games. One library. BuiltByNyx.
