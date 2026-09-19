# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-09-19

### Changed

- Hour labels and event times use `KalendsMetrics.TimeFormat` (`HH:mm` by default) instead of the culture `"t"` pattern.

### Added

- `KalendsView` with agenda, day, week, month, and year layouts.
- `IKalendsEvent` / `IKalendsCalendar` host contracts without sync or storage fields.
- `EventLayoutCalculator` with shared JSON fixtures for overlap lanes.
- Drag, resize, keyboard move, and culture-aware first weekday.
- Sample app and `dotnet pack` for `Kalends.WinUI`.
