# Changelog

All notable changes to **ID Stack** — the ID Card Design & Batch Printing Software by Prabir kumar Das — are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.3.0] — Phase 3: Data Import (2026-09-24)

### Added

**Data models (`IDStack.Core/Models/Import`):**

- `ExcelData` (columns + rows + source metadata), `DataRow` (case-insensitive column access, missing cells = empty string), `ColumnMapping` (placeholder ↔ column binding with required flag), `PhotoRecord` (file path, match key), `CropMode` (Fit/Center/Stretch), `ValidationIssue` (Info/Warning/Error)

**Service interfaces & implementations:**

- `IExcelService` / `ExcelService` — loads **.xlsx** (EPPlus 4.5, LGPL), **.xls** (NPOI), and **.csv** (manual RFC-4180-style parser with quotes, `,`/`;`/tab delimiters, BOM detection); column preview, row loading, structural validation; all I/O async off the UI thread
- `IPhotoService` / `PhotoService` — folder scan (JPG/PNG/BMP/TIFF), three-stage matching (exact → with-extension → fuzzy contains), 300-DPI print processing with a bounded in-memory cache and `ClearPhotoCache()`
- `IImageProcessingService` / `ImageProcessingService` — resize (aspect-preserving), crop, rotate, dimension probe, plus `ResizeExact` for fill/fit/stretch modes; fully managed via SixLabors.ImageSharp (no native deps, Win7-safe)
- `IDataValidationService` / `DataValidationService` — missing required columns/values, duplicate-ID warnings, photo-file existence checks; issues ordered by row and severity

**Data Import wizard (replaces the Phase 3 placeholder page):**

- `DataImportViewModel` orchestrating 4 steps: ① Excel file → ② Column mapping → ③ Photos → ④ Validation
- `ExcelImportViewModel` — file picking via dialog event, loading state, row/column status, `TestSetData` hook for tests
- `ColumnMappingViewModel` + `ColumnMappingItemViewModel` — auto-builds one row per template placeholder (both sides), auto-matches by name, live sample values from the first data row, ✓/⚠ status icons, required-mapped tracking
- `PhotoImportViewModel` — folder browse event, photo list, match-by-column, matched-count status, rematch command
- `DataImportView` — step tabs, per-step panels (file picker, mapping grid, photo list, validation report with ID-column duplicate check), Win7-compatible dialogs (`OpenFileDialog` + WinForms `FolderBrowserDialog` owned by the WPF window)
- `CanGenerate` gate: data loaded + all required placeholders mapped (ready for Phase 4 batch generation)

**Dependencies:** EPPlus 4.5.3.3, NPOI 2.6.2, SixLabors.ImageSharp 2.1.4 (all pure managed NuGet packages; Windows Forms enabled for the folder dialog)

### Tests

- 50 new tests: ExcelService (14), PhotoService + ImageProcessingService (17), DataValidationService (9), DataImportViewModels (9), 1000-row import performance guardrail (< 5 s per spec) — **120 total, all passing**

## [0.2.1] — Editor Hardening (2026-09-24)

### Added

- **New Document dialog** (`NewDocumentDialog`): Photoshop-style preset picker (CR80 portrait/landscape, A6, A7, business card, badge) with editable width/height, unit selection (mm/cm/in/px), and orientation toggle; applied to both card sides via Ctrl+N
- **File logging**: `ILogger` interface in `IDStack.Core` and `LogService` writing daily files to `%APPDATA%\IDStack\logs\`; startup, errors, and unhandled exceptions are logged (never throws)
- **Live element editing**: `CanvasElement` now raises `INotifyPropertyChanged` so the properties panel, layers panel, and canvas stay in sync during drags and edits

### Changed

- `CanvasControl` upgraded to a professional design surface: dark surround with card border, rulers, adaptive grid, 8-way resize handles, drag-move, pan (space/middle-drag), zoom at cursor, inline text editing, keyboard shortcuts (arrows nudge, Delete, Ctrl+D duplicate, Ctrl+Z/Y undo/redo)
- `MainWindow` opens maximized with smaller minimum size (960×600) for 1366×768 displays; sidebar contrast fix
- Automation names added to editor buttons for UI testing and accessibility
- Global exception handlers log to file before showing the error dialog

### Fixed

- `NewDocument` no longer throws `NullReferenceException` when elements exist or after undo (regression covered by `NewDocumentCrashTests`)

### Tests

- 2 new crash-regression tests (`NewDocumentCrashTests`) — **70 total, all passing**

## [0.2.0] — Phase 2: Template Editor (2026-09-23)

### Added

**Template editor (Phase 2):**

- Core models: `CardTemplate`, `TemplateSide`, element hierarchy (`TextElement`, `ImageElement`, `ShapeElement`, `BarcodeElement`, `PlaceholderElement`) with CR80 default size (85.6 × 54 mm)
- `ITemplateService` / `TemplateService` — creates, saves, and loads `.idcard` files (JSON) with atomic writes and a polymorphic element converter; template validation
- `IHistoryService<T>` / `HistoryService` — snapshot undo/redo with 100-step bounded stack
- Editor view models: `TemplateEditorViewModel` (add/delete/duplicate/selection/zoom/side switching), `LayersPanelViewModel` (z-order, visibility, lock), `PropertiesPanelViewModel` (position, size, rotation, opacity, type-specific properties), `ToolsPanelViewModel`, `CanvasViewModel`
- `CanvasControl` — interactive WPF canvas: renders element visuals in mm→px scale, click selection, mouse dragging with bounds clamping, selection highlight
- `TemplateEditorView` — four-panel editor layout (tools | canvas | layers | properties) with front/back side switching and zoom controls
- File dialogs wired through shell events (Open/Save/Save As)

**Branding:**

- Generated multi-size application icon (`Assets/app.ico`) wired to exe + window title bar
- Full assembly metadata: Product "ID Stack", copyright "Prabir kumar Das", version 0.2.0

**Project rename:**

- Projects, folders, namespaces, and assemblies renamed `IDCardSoftware*` → `IDStack*` throughout

### Tests

- 25 new tests: HistoryService (6), TemplateService (7), TemplateEditorViewModel (12) — **68 total, all passing**

## [0.1.0] — Phase 1: Project Foundation (2026-09-23)

### Added

**Solution & projects** (`.NET Framework 4.8`, C# 7.3, WPF, MVVM):

- `IDStack.sln` with three projects:
  - `src/IDStack/` — main WPF application (`WinExe`)
  - `src/IDStack.Core/` — shared models, interfaces, and constants (class library)
  - `src/IDStack.Tests/` — MSTest unit tests with Moq
- SDK-style `csproj` files; NuGet-only managed dependencies (no native code, Win7 SP1 compatible)
- `app.manifest` with Windows 7–11 supported-OS declarations and per-monitor high-DPI awareness

**MVVM framework:**

- `ViewModelBase` / `ObservableObject` — `INotifyPropertyChanged` bases with `SetProperty`
- `RelayCommand`, `AsyncRelayCommand` (re-entrancy safe), `DelegateCommand<T>`
- Converters: `BoolToVisibilityConverter`, `InverseBoolConverter`, `ColorToBrushConverter`, `MillimeterToPixelConverter`

**Dependency injection:**

- `Microsoft.Extensions.DependencyInjection` wired in `App.xaml.cs` (service provider built on startup, disposed on exit)
- Singleton services, singleton shell, transient page view models; global unhandled-dispatcher-exception handler

**Main window shell & navigation:**

- Modern dark-sidebar shell: menu bar, sidebar navigation, page header with language selector, status bar
- `INavigationService`/`NavigationService` (registry + ViewModel cache) and `NavigationKeys`
- Pages: Home (quick actions), Settings, plus localized placeholder pages for Template Editor / Template Library / Data Import / Batch Processing
- Keyboard shortcuts: Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+E
- Modern WPF styles: `Colors.xaml`, `Buttons.xaml`, `TextBoxes.xaml`, `Panels.xaml`, `GlobalStyles.xaml`

**Services:**

- `ISettingsService` / `SettingsService` — async JSON persistence under `%APPDATA%\IDStack\settings.json`, atomic writes, corrupt-file recovery, defaults reset
- `ILocalizationService` / `LocalizationService` — runtime language switching via `ResourceManager`; `LanguageChanged` event; 11 languages

**Localization:**

- `Strings.resx` (English, default) plus 10 satellite cultures: hi-IN, mr-IN, or-IN, bn-IN, ta-IN, te-IN, kn-IN, gu-IN, pa-IN, es-ES
- All user-facing strings routed through resource keys (no hardcoded UI text)

### Tests

- 43 unit tests covering SettingsService, LocalizationService, NavigationService, commands, MainViewModel, SettingsViewModel — all passing
- Both Debug and Release builds compile with 0 warnings / 0 errors

### Documentation

- README project-status table updated to Phase 1 complete
- Tooling scripts added under `tools/scripts/` for building and testing without Visual Studio
