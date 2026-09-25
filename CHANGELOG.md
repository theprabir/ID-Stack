# Changelog

All notable changes to **ID Stack** — the ID Card Design & Batch Printing Software by Prabir kumar Das — are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.3.2] — PSD Import Fixed, Dark Theme (2026-09-25)

### Fixed

**PSD import works in both the Template Editor and Data Import** (previously every import produced an empty design):

- **Visibility-flag workaround** — PsdSharp 1.0.2 mis-parses the PSD layer visibility flags and reports `IsVisible=False` for *every* layer, so the importer's `Where(l => l.IsVisible)` filter silently dropped all text layers. The importer no longer trusts that property and imports every layer carrying usable pixel data
- **Per-layer composite fallback** — PSDs saved as CMYK + Zip crash PsdSharp's merged-composite decoder (`IndexOutOfRangeException` in `DeltaDecode`). `RenderCompositeBackground` now tries the merged composite first and, on failure, flattens the design from per-layer pixel data (bottom-up, honoring layer opacity), which decodes reliably — verified on the sample PSD (7 layers composited, 5 text layers detected)
- **Editor canvas refresh** — `ApplyLoadedTemplate` swaps the whole `Template` object, but the canvas only re-hooks element collections on a `CurrentSide` change notification; the side (Front) was unchanged so the canvas kept showing the previous empty side. `ApplyLoadedTemplate` now raises `CurrentSide`/`Elements` change notifications explicitly after any file/PSD load
- **Data Import dialogs dead after navigation** — the view subscribed to the view model's `ExcelFilePicked`/`PhotoFolderPicked` events only in `DataContextChanged`, which never fires when the singleton view model is assigned before the view's constructor; `Unloaded` also detached handlers that were never re-attached when navigating back. The view now subscribes at construction, re-subscribes on `Loaded`, and cleans up on `Unloaded` — Excel and photo browsing load reliably on every visit
- **Stray "Ready" text floating mid-window** — the main-window status bar was docked *after* the filled page content in the same `DockPanel`, pushing it into the content area; it now docks before the content and sits properly at the bottom
- **Sample PSD restored** — `tools/sample-data/demopsd.psd` had been truncated to 4 KB (importers reported "PSD file is corrupt"); restored to the full 2.9 MB file

### Changed

- **Dark theme** — the entire shell (background, surfaces, borders, sidebar, menus, status bar, buttons, text boxes) now uses a dark slate palette; the **editor canvas artboard stays white** (`TemplateSide.BackgroundColor` remains `#FFFFFF` and the dark surround/rulers frame it), and the data-preview grid keeps its light background for readability
- UI-test scripts (`run-dataimport-ui-test.ps1`, diag scripts) prefer UIA `InvokePattern` over synthetic mouse clicks — window-activation was swallowing first-clicks and leaving imports untriggered; editor gains an `IDSTACK_AUTO_OPEN` dialog bypass hook for test automation

### Tests

- **128 unit tests, all passing**; 16-step end-to-end Data Import UI test passes with verified dark-theme screenshots; PSD verified in the editor (6 layers incl. 5 text elements) and in Data Import (5 placeholders + rendered preview)

## [0.3.1] — Phase 3 Redesign: Design-Driven Data Import (2026-09-24)

Complete rebuild of the Data Import flow around the real user workflow: the design is the source of truth, placeholders are detected from it, and everything is mapped before processing.

### Added

**Photoshop design import:**

- `PsdSharp` 1.0.2 (pure managed, net48-compatible) for reading .psd files without Photoshop
- `PsdDesignImporter` — converts a PSD into an editable template at 300 DPI print scale: the composite raster is flattened into a background image so the design stays **100% visually identical**, while Photoshop text layers (detected via type-tool tagged blocks `tySh`/`TySh`) become individually editable text elements positioned exactly where Photoshop placed them; a per-layer report is shown after import

**Design import service:**

- `IDesignImportService` / `DesignImportService` — loads .idcard files, imports .psd files, and **auto-detects every mappable placeholder** in the design: text layers → text placeholders, image layers → image placeholders (the flattened design background is excluded); placeholder names come from layer names/text and are guaranteed unique

**Rebuilt Data Import (3 steps matching the workflow):**

- **① Import** — .idcard/.psd design, Excel/CSV sheet, and photo folder together on one screen, each with browse buttons, status lines, and supported-format hints
- **② Mapping** — placeholders auto-detected from the design are listed with editable names (rename to avoid confusion, duplicates rejected), Excel columns auto-bound to text placeholders by name match, photos auto-matched to rows via a smart photo column picker (prefers PhotoFile/Photo/Image columns), sample values shown
- **③ Preview & Validate** — data preview grid with one column per Excel header, validation report (missing required values, duplicate IDs, missing photos), ID-column duplicate check, and a "Ready for processing" gate for Phase 4
- All state lives in the view model (MVVM); the view hosts only dialogs; stale event-handler subscriptions are cleaned up on navigation

### Changed

- `DesignPlaceholder` raises property-change notifications so mapping edits update the UI live
- Photo validation now checks **only the photo-match column** instead of scanning every column for file-like values (eliminated false "photo not found" warnings on names/emails)
- `NavigationItem.ToString()` returns the localized display name so UI Automation can address sidebar items reliably
- Editor view (`TemplateEditorView`) unsubscribes stale singleton view-model events on re-attach, fixing a `NullReferenceException` after navigating away and back

### Fixed

- Data Import no longer auto-jumps to validation after loading a file — the user stays on the current step and navigates with explicit Next/Back buttons
- Preview grid shows real column data (DataTable-backed) instead of raw object rows
- Native file dialogs no longer crash after re-entering the page (stale-handler fix)

### Tests

- Rewritten DataImportViewModel tests for the design-driven flow (13 tests covering placeholder detection, uniqueness, auto-bind, rename validation, ready gate) — **124 total, all passing**
- End-to-end UI test (`tools/scripts/run-dataimport-ui-test.ps1`) drives the real app through all 3 steps with sample data: **12/12 steps pass** with verified screenshots

### Sample data

- `tools/sample-data/` — sample-badge.idcard design, employees.csv (3 rows × 5 columns), photos/ (3 generated PNGs), and a photo-generator project

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
