# Changelog

All notable changes to **ID Stack** — the ID Card Design & Batch Printing Software by Prabir kumar Das — are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.1.0] — Phase 1: Project Foundation (2026-09-23)

### Added

**Solution & projects** (`.NET Framework 4.8`, C# 7.3, WPF, MVVM):

- `IDCardSoftware.sln` with three projects:
  - `src/IDCardSoftware/` — main WPF application (`WinExe`)
  - `src/IDCardSoftware.Core/` — shared models, interfaces, and constants (class library)
  - `src/IDCardSoftware.Tests/` — MSTest unit tests with Moq
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

- `ISettingsService` / `SettingsService` — async JSON persistence under `%APPDATA%\IDCardSoftware\settings.json`, atomic writes, corrupt-file recovery, defaults reset
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
