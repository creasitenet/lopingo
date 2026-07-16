# Lopingo

A small uptime monitor for your own server. One `docker compose up`, no database to install, no account to pre-create. Just you, a SQLite file, and a list of URLs to keep an eye on.

It's an Uptime Kuma for people who don't want Node on their VPS — or who just prefer C#.

---

## Quick start

```bash
git clone https://github.com/creasitenet/lopingo.git
cd lopingo
docker compose up -d --build
```

Open <http://localhost:8080>. The first page you'll see asks you to pick a username and a password — that's your owner account, you're in. Add a monitor, choose how often to check it (1 min, 5 min, 15 min, 30 min, 1 h), done.

That's the whole install. No `.env` to fill in, no terminal step after `docker compose up`, no separate database container.

---

## What it does

- Watches HTTP(S) URLs at the interval you choose.
- Tries a probe up to 4 times before giving up — a single 503 won't wake you up.
- Opens an incident when a monitor goes down, closes it on recovery, remembers the duration.
- Shows a live heartbeat on the dashboard — it just works, no page refresh.
- Sends Telegram alerts per monitor (optional, set up in the UI).
- Stores everything in a single SQLite file under `data/` on the host. Inspect it, back it up, restore it — it's just a file. See [Configuration](#configuration) for the details.

What it doesn't do (yet): SSL certificate checks, TCP/ping/DNS monitors, status pages, email/Slack/webhooks, multi-tenant. See [ROADMAP.md](ROADMAP.md) for what comes next.

---

## Configuration

Nothing is required. Defaults are fine for a small VPS running ~50 monitors.

If you want to tweak things, copy `.env.example` to `.env` and uncomment what you need:

| Variable | Default | What it does |
|---|---|---|
| `LOPINGO_DB_PATH` | `/data/lopingo.db` | Where the SQLite file lives. The volume mounts `/data` by default. |
| `LOPINGO_TICK_SECONDS` | `5` | How often the worker wakes up to look for due monitors. |
| `LOPINGO_MAX_PARALLEL` | `10` | How many HTTP probes run at the same time. |
| `LOPINGO_BATCH` | `100` | Max monitors processed per tick. |
| `LOPINGO_CHECK_RETENTION_DAYS` | `30` | Delete check rows older than this (runs at startup, then daily). |

A few practical notes:

- **HTTPS in production**: put Lopingo behind a reverse proxy (Caddy, Nginx, Traefik). It already trusts `X-Forwarded-For` and `X-Forwarded-Proto`.
- **Big monitoring setup**: drop `LOPINGO_TICK_SECONDS` to 1 and bump `LOPINGO_MAX_PARALLEL` to 50 if you have hundreds of monitors on a beefy box.
- **Backup**: stop the container with `docker compose stop lopingo`, copy `data/lopingo.db` somewhere safe, then `docker compose start lopingo`. SQLite is happy to be copied while the container is down; while it's up, prefer `sqlite3 data/lopingo.db ".backup /path/to/backup.db"` if you have the `sqlite3` CLI on the host.
- **Reset everything**: `docker compose down`, `rm data/lopingo.db`, `docker compose up -d --build` — you'll get the signup flow back.

---

## Development

See [CONTRIBUTING.md](CONTRIBUTING.md). Short version: `.NET 10 SDK`, `dotnet run` from the `Lopingo/` folder, hot reload with `dotnet watch run`. Tests live in `Lopingo.Tests/` and run with `dotnet test`.

---

## How it works (the short tour)

```
Lopingo/
├── Components/          Blazor UI (MudBlazor)
├── Core/
│   ├── Buses/           In-memory events (Telegram alerts + live UI)
│   ├── Engine/          HTTP probe + retries (CheckProcessor)
│   └── Workers/         Background workers for checks and notifications
├── Data/                EF Core entities and DbContext
├── Repositories/        SQLite data access
├── Services/
│   ├── Auth/            Cookie auth, signup, login throttle
│   └── Notifications/   Telegram notifier
└── Program.cs
```

A few design choices worth knowing:

- **One owner, one database.** Exactly one `Owners` row for login. Monitors and Telegrams belong to the instance, not to a user id.
- **Retry-aware probing.** A monitor is only marked down after 4 failed attempts in a row, with a 5-second gap. Most transient blips vanish.
- **Single-instance only.** The live dashboard uses an in-memory `Channel`. Run more than one replica and the dashboard will lag — clustering is not on the roadmap yet.
- **No EF migrations yet.** The schema is built on first boot via `EnsureCreated`. If you change entity shapes during development, delete the SQLite file and restart.

---

## License

MIT — see [LICENSE](LICENSE). Changelog: [CHANGELOG.md](CHANGELOG.md).