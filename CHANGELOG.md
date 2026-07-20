# Changelog

## Unreleased

### Fixed

- Docker healthcheck uses `curl` (the `aspnet` image has no `wget`).
- Event bus allows concurrent writers so parallel probe ticks cannot race the channel.
- Owner signup is keyed to a single row (`Id = 1`) so concurrent first-boot signups cannot create two owners.
- Pin `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 to clear the transitive NU1903 / CVE-2025-6965 warning.
- Live dashboard/status refresh: read monitors with `AsNoTracking` (Blazor Server was serving stale tracked entities), fan-out check events to every UI subscriber, and serialize page reloads so monitor headers cannot lag behind the checks table.

### Added

- `SECURITY.md`, Contributor Covenant `CODE_OF_CONDUCT.md`, GitHub issue/PR templates.
- CI job that builds the Docker image and checks `curl` is present.

## [0.1.0] — 2026-07-10

First public release. Self-hosted, single-user uptime monitor in one `docker compose up`.

### Added

- **One-command install** — `docker compose up -d --build`, SQLite on a volume, no external DB.
- **Zero-config onboarding** — first visit shows `/signup`; pick username and password, land on the dashboard. No env vars required.
- **HTTP monitors** — URL, check interval (1 min – 1 h), HEAD with GET fallback on 405/501.
- **Retry-aware probing** — 1 attempt + 3 retries (5 s apart) before marking a monitor down.
- **Incidents** — opened on DOWN transitions, closed on recovery with duration.
- **Live dashboard** — Blazor Server UI refreshes on status changes via an in-memory event bus.
- **Telegram notifications** — configure bots in the UI, link them to monitors.
- **Settings** — change the owner password.
- **Health endpoint** — `GET /healthz` for container health checks.
- **Tunable worker** — `LOPINGO_TICK_SECONDS`, `LOPINGO_MAX_PARALLEL`, `LOPINGO_BATCH`, `LOPINGO_DB_PATH`.
- **Tests** — xUnit suite covering probe logic, repositories, the worker tick, and a signup-to-monitor smoke flow.
- **CI** — GitHub Actions on `ubuntu-latest` with .NET 10.

[0.1.0]: https://github.com/creasitenet/lopingo/releases/tag/v0.1.0