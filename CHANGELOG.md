# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.1] - 2026-10-15

### Fixed

- Month and all-day event previews retain the original card size and pointer grab offset instead of using a fixed-width floating rectangle.
- The destination date is outlined separately; preview cards have an opaque surface and are clipped to the calendar bounds.
- Moving a later segment of a multi-day event preserves that segment under the pointer, along with the event's original time and duration.
- Dropping outside a date cancels the move. The release position is checked again, and adjacent date boundaries are treated as half-open.
- Refreshes requested while dragging are applied after pointer capture ends, without rebuilding cards after ordinary clicks.

## [0.2.0] - 2026-10-14

### Added

- `DayInfoProvider` supplies host-owned day classifications independently of events: working days, ordinary weekends, holidays, transferred days off and transferred working days.
- `KalendsDayInfo` includes shortened-day information and an optional explanation for tooltips and assistive technology. Lookups are cached for each refresh; a missing provider or a null result falls back to Saturday/Sunday weekends.
- Independent `HighlightWeekends`, `HighlightHolidays` and `ShowWorkingDayMarkers` controls across Agenda, Day, Week, Month and Year views. `ShowWeekends` continues to control Saturday/Sunday column visibility.
- Localizable labels and markers in `KalendsStrings`, including a compact `*` for shortened days in Year view.
- Theme brushes for non-working days and working-day markers, with Light, Dark and High Contrast defaults.
- Day-classification tests covering independent highlights, transferred working weekends and shortened-day markers.

### Fixed

- Day and Week views now use the same horizontal coordinates for all-day backgrounds and the hourly grid. Event-card padding and reserved scrollbar space no longer shift the highlighted day boundaries.
- Day headers leave room for date markers at larger display scales.

### Integration notes

- Holiday schedules, transfer rules, storage and preference persistence remain the host application's responsibility. No country-specific calendar data or network requests are built into Kalends.
- After changing the data behind a provider, call `Refresh()` to invalidate the per-refresh cache.

## [0.1.0] - 2026-09-19

### Changed

- Hour labels and event times use `KalendsMetrics.TimeFormat` (`HH:mm` by default) instead of the culture `"t"` pattern.

### Added

- `KalendsView` with agenda, day, week, month, and year layouts.
- `IKalendsEvent` / `IKalendsCalendar` host contracts without sync or storage fields.
- `EventLayoutCalculator` with shared JSON fixtures for overlap lanes.
- Drag, resize, keyboard move, and culture-aware first weekday.
- Sample app and `dotnet pack` for `Kalends.WinUI`.
