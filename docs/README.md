# Kalends.WinUI notes

The library draws calendar grids. Applications keep repositories, calendars, and their own sync.

Public surface:

- `KalendsView` — agenda / day / week / month / year
- `IKalendsEvent`, `KalendsEvent`
- `IKalendsCalendar`, `KalendsCalendar`
- `EventLayoutCalculator`, `KalendsDate`
- `KalendsStrings`, `KalendsMetrics`

`grep` on the `src` tree must stay empty for `legalic`, `provider`, `sync`, and `keychain`.
