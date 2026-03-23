# Changelog

Alle relevanten Änderungen am Plugin `ShowNetworkNumbers` werden in dieser Datei dokumentiert.

Das Format orientiert sich an `Keep a Changelog`.

## [1.0.2.0]

### Fixed

- Horizontaler Scrollbalken bei vielen BACnet Geräten

## [1.0.0.0]

### Added

- Erstes Release des Plugins `ShowNetworkNumbers` für Yabe.
- Visualisierung der BACnet-Netzwerktopologie als Baumstruktur.
- Gruppierung entdeckter Geräte nach Router und Subnetzwerk (SNET).
- Darstellung von Device-ID, MAC-Adresse und SNET in kompakten Gerätekarten.
- Automatische SNET-Erkennung über `BacAdr.RoutedSource.net` mit Fallback auf `BacAdr.net`.
- Ein- und Ausklappen einzelner Netzwerkspalten über `+/-`.
- Globaler Schalter zum Aus- und Einklappen aller Netzwerkspalten.
- Refresh-Funktion zum erneuten Einlesen der in Yabe gefundenen Geräte.
- Hilfe-Dialog innerhalb des Plugins.
- Grafische Verbindungslinien zwischen Router-, Netzwerk- und Geräteebene.
