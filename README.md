# Universal Extractor

Universal Extractor is a Windows desktop application (WPF, .NET 8) that lets you drag and drop common document formats (PDF, DOCX, DOCM, DOTX, DOTM, TXT, CSV, ODT, RTF, HTML, MD) and extract one category of structured data through a curated set of regular expressions.

## Features

- Single-screen UI with three zones:
  - Left panel: drag-and-drop area that previews the file name and icon.
  - Right-top panel: combo box listing the supported extraction targets (e-mail address, phone number, social network handles, dates, credit card number, IBAN, BIC/SWIFT, IPv4, IPv6, MD5, SHA1, SHA256).
  - Right-bottom panel: “Extract” button that opens a Save-As dialog.
- In-memory parsing for every document type (no temporary files).
- Regex-driven extraction, with the resulting matches sorted and deduplicated before being written to disk.
- Single-file release build for quick distribution and portable usage.

## Development Setup

Requirements:

- .NET SDK 8.0+
- Windows 10 or later

Restore dependencies and build:

```powershell
dotnet build UniversalExtractor.sln
```

Run the app:

```powershell
dotnet run --project UniversalExtractor.App
```

## Release / Packaging

The repository includes a helper script and MSIX scaffolding:

```powershell
pwsh scripts/BuildRelease.ps1             # portable single-file exe in artifacts/publish/...
pwsh scripts/BuildRelease.ps1 -PackageMsix -MakeAppxPath "C:\path\to\makeappx.exe"
```

See `RELEASE.md` for the full workflow (signing, MSIX specifics, optional MSI).

## Project Structure

```
UniversalExtractor.sln
UniversalExtractor.App/
 ├── App.xaml / App.xaml.cs
 ├── MainWindow.xaml / MainWindow.xaml.cs
 ├── Assets/ (icon resources)
 ├── Models/ExtractionDefinition.cs
 ├── Services/DocumentTextExtractor.cs
Packaging/
 ├── AppxManifest.xml
 └── Assets/ (MSIX logos)
scripts/
 └── BuildRelease.ps1
LICENSE (GPL-3.0)
RELEASE.md
README.md
```

## License

This project is licensed under the GNU General Public License v3.0 (see `LICENSE`). Third-party dependencies (UglyToad.PdfPig, DocumentFormat.OpenXml, System.Drawing.Common) are MIT-licensed and compatible with GPL distribution.
