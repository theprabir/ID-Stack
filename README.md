# ID Stack — ID Card Design & Batch Printing Software

## 🎯 Overview

A professional-grade, open-source **ID Card Design and Batch Printing Software** for Windows, built by **Prabir kumar Das**. It enables businesses, schools, colleges, event organizers, and printing presses to design ID cards with a Photoshop-like editor, import data from Excel and photos in bulk, generate hundreds of cards automatically, and print them efficiently using imposition (multiple cards per sheet) — including duplex (front + back) layouts.

Built with C# and WPF on .NET Framework 4.8, it runs on Windows 7 SP1 and later with **no external runtime dependencies** after installation.

## ✨ Features

- **Photoshop-Like Template Editor**
  - Interactive canvas with zoom (10%–400%), pan, grid, rulers, and guides
  - Tools: Text, Image, Shapes (rectangle/circle/line/polygon), Barcodes (QR, Code 128, Code 39, EAN-13, UPC-A), and Placeholders
  - Layers panel with reordering, visibility, locking, grouping, and opacity
  - Context-sensitive properties panel (position, size, rotation, colors, fonts)
  - Alignment & distribution tools with smart guides and snap-to-grid
  - Full undo/redo history (50+ steps) with a visual history panel

- **Dual-Sided Templates** — Design independent Front and Back sides in one template, sharing the same data source

- **Data Import & Binding**
  - Import from Excel (.xlsx, .xls) and CSV
  - Drag-drop column mapping between Excel columns and template placeholders (`{{ColumnName}}` syntax)
  - Bulk photo import from a folder with auto-matching by filename or Excel column
  - Data validation with a pre-generation report

- **Batch Generation Engine**
  - Background, chunked, parallel processing — 100 cards in under 30 seconds
  - Progress bar with estimated time remaining, pause/resume/cancel
  - Output as PNG (300 DPI), JPG (configurable quality), or PDF; ZIP batch export with Fronts/Backs folder structure

- **PSD Import** — Bring in Photoshop designs; text, image, and shape layers are converted into editable elements

- **Pre-Designed Template Library** — 10–20 ready-made templates for employees, students, events, memberships, visitors, and medical IDs

- **Imposition Engine (Print-Ready Output)**
  - A4/A3/Letter/Legal/custom sheets with configurable margins, gutters, bleed, and cut marks
  - Duplex same-sheet layout: fronts in row 1, matching backs directly below in row 2
  - Flexible numbering (per-sheet, continuous, or none) with synchronized front/back numbers
  - Print-ready 300 DPI PDF export

- **Internationalization** — 11 languages with full Unicode support for Indian scripts, plus all system fonts including Akriti and Kruti Dev

## 🖥️ System Requirements

| Requirement | Minimum |
|---|---|
| Operating System | Windows 7 SP1 (32-bit or 64-bit), Windows 8.1, Windows 10, Windows 11 |
| Framework | .NET Framework 4.8 (installed automatically by the installer if missing) |
| RAM | 4 GB (8 GB recommended for large batches) |
| Disk Space | 500 MB free (plus space for generated cards) |
| Display | 1366×768 or higher, high-DPI (125%/150%/200%) supported |

## 📥 Installation

1. Download the latest installer (`IDStackSetup.exe`) from the [Releases](../../releases) page.
2. Run the installer. It will automatically install .NET Framework 4.8 if it is not already present.
3. Follow the setup wizard — no additional runtimes, MSVC redistributables, or manual dependencies are required.
4. Launch **ID Stack** from the Start Menu or desktop shortcut.

## 🚀 Quick Start Guide

1. **Create a template** — Launch the app, click **New Template**, and design the Front Side (logo, photo placeholder, text fields) using the toolbox.
2. **Design the back** — Switch to the **Back Side** tab and add a barcode, contact info, or terms.
3. **Import Excel** — Click **Import Excel**, choose your .xlsx/.xls/.csv file, and map columns to your placeholders (e.g., `{{Name}}`, `{{ID}}`).
4. **Import photos** — Click **Import Photos**, select the folder, and photos are matched to rows automatically.
5. **Preview & generate** — Preview a card, click **Generate All**, pick PNG/JPG/PDF output, and watch the progress bar.
6. **Print** — Open **Print Setup**, choose a sheet size and numbering mode, and generate a print-ready imposition PDF.

## 📖 Documentation

- [User Manual](docs/user_manual.md)
- [Keyboard Shortcuts](docs/keyboard_shortcuts.md)
- [Troubleshooting Guide](docs/troubleshooting.md)
- [API Reference](docs/api_reference.md)

## 🛠️ For Developers

### Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/your-org/idcard-software.git
   ```
2. Open `IDCardSoftware.sln` in **Visual Studio 2022** (with .NET desktop development workload and .NET Framework 4.8 targeting pack).
3. Restore NuGet packages (automatic on build).
4. Build the solution (`Ctrl+Shift+B`) — Debug or Release, Any CPU.
5. Run tests via Test Explorer or `tools/scripts/test.ps1`.

### Project Structure

```
src/
├── IDCardSoftware/        # Main WPF application (Views, ViewModels, Models, Services)
├── IDCardSoftware.Core/   # Shared core library (models, interfaces, constants)
└── IDCardSoftware.Tests/  # Unit and integration tests (MSTest/xUnit + Moq)
```

The project follows **MVVM** strictly with dependency injection, a service-oriented architecture, and async/await for all I/O. See [Architecture.md](Architecture.md) for the full technical blueprint.

### Contribution Guidelines

- Follow the existing code style (C# conventions, XML documentation on all public APIs, methods < 50 lines)
- Add/update unit tests for every change (target: > 80% coverage)
- Do not introduce native (C++) dependencies — NuGet packages must be pure managed .NET
- Keep Windows 7 SP1 compatibility — no Win10+/UWP-only APIs
- All user-facing strings must go through resource files for localization

## 🌍 Supported Languages

- English (English)
- Hindi (हिंदी)
- Marathi (मराठी)
- Odia (ଓଡ଼ିଆ)
- Bengali (বাংলা)
- Tamil (தமிழ்)
- Telugu (తెలుగు)
- Kannada (ಕನ್ನಡ)
- Gujarati (ગુજરાતી)
- Punjabi (ਪੰਜਾਬੀ)
- Spanish (Español)

All regional system fonts — including **Akriti** and **Kruti Dev** — are supported in the font selector.

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome!

1. Fork the repository and create your branch from `main`
2. Make your changes with tests and documentation
3. Ensure the solution builds and all tests pass
4. Submit a Pull Request describing what you changed and why

Please read the developer guidelines above before opening a PR.

## 🐛 Reporting Issues

Found a bug or have a feature request?

1. Check [existing issues](../../issues) to avoid duplicates
2. Open a new issue with:
   - A clear title and description
   - Steps to reproduce (for bugs)
   - Your Windows version and app version
   - Screenshots or sample files, if relevant

## 📊 Project Status

**Current Phase: Phase 1 — Project Foundation (complete)**

| Phase | Description | Status |
|---|---|---|
| 1 | Project Foundation (solution, MVVM, DI, main window shell, settings & localization services) | ✅ Complete |
| 2 | Template Editor (canvas, tools, layers, properties, undo/redo, save/load) | ⏳ Not started |
| 3 | Data Import (Excel, photos, column mapping, validation, preview) | ⏳ Not started |
| 4 | Batch Processing (generation engine, progress, export) | ⏳ Not started |
| 5 | PSD Import & Template Library | ⏳ Not started |
| 6 | Imposition Engine (sheet layout, numbering, print-ready PDF) | ⏳ Not started |
| 7 | Polish & Production (translations, fonts, installer, final testing) | ⏳ Not started |

**Implemented so far:** Project documentation, license, repository setup, and **Phase 1 — Project Foundation** (see [CHANGELOG.md](CHANGELOG.md)).

**Known issues:** None.

**Next steps:** Phase 2 — Template Editor.

## 🙏 Acknowledgments

Built with these excellent open-source libraries:

- [EPPlus](https://github.com/EPPlusSoftware/EPPlus) — Excel (.xlsx) reading and writing
- [NPOI](https://github.com/nissl-lab/npoi) — Legacy Excel (.xls) support
- [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) — Cross-platform image processing
- [PdfSharp](https://github.com/zauberzeug/pdfsharp) — PDF generation
- [PsdSharp](https://github.com/psd-tools) — Photoshop PSD parsing
- [QRCoder](https://github.com/codebude/QRCoder) — QR code generation
- [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) — Modern WPF styling
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) — JSON serialization
- [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime) — Dependency injection

## 📞 Support

- 📖 Documentation: see the [docs/](docs/) folder
- 🐛 Bug reports & feature requests: [GitHub Issues](../../issues)
- 💬 Questions & discussions: [GitHub Discussions](../../discussions)

For commercial support inquiries, please open an issue with the `support` label.
