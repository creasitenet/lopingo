# Contributing

Hey, thanks for wanting to make Lopingo better. Most of what you need to know is below — it's not long, on purpose.

If you just want to run Lopingo on your server, you don't need this file. The README's quick start is enough.

---

## Local dev

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Git. That's it.

```bash
git clone https://github.com/creasitenet/lopingo.git
cd lopingo
dotnet restore
cd Lopingo
dotnet run
```

Open <http://localhost:5130>. On a fresh database the signup page appears — pick a username and password, you're in.

For hot reload while you edit:

```bash
dotnet watch run
```

To wipe your local database and start over: stop the app, delete `Lopingo/lopingo.db`, run again.

There's no `.env` to set up locally. Defaults work.

---

## Running tests

```bash
dotnet test Lopingo.Tests
```

That's it. The suite covers the probe logic, the worker tick, the repositories, and a smoke flow that signs up and creates a monitor over HTTP. CI runs the same command on every push and PR.

If you change something user-visible, update the README in the same PR. Otherwise just keep the diff focused.

---

## Project layout

```
Lopingo/
├── Components/        Blazor pages and UI (MudBlazor)
├── Core/
│   ├── Buses/         In-memory events (alerts + live UI)
│   ├── Engine/        HTTP probe and retry logic
│   └── Workers/       Background check + notification workers
├── Data/              EF Core DbContext and entities
├── Repositories/      SQLite data access
├── Services/
│   ├── Auth/          Cookie auth, signup, login throttle
│   └── Notifications/ Telegram notifier
└── Program.cs         Wire-up and configuration

Lopingo.Tests/           xUnit tests
Dockerfile
docker-compose.yml
```

A few things worth knowing if you're diving in:

- **Login and signup are static SSR** (form POST + cookie). Cookie auth must run on an HTTP response — it does not work from a SignalR circuit. Every other page uses `@rendermode InteractiveServer`.
- **Two background services run in production.** `MonitorCheckWorker` does the probing, `NotificationWorker` reads from the in-memory bus and sends Telegram alerts. Tests replace them with `WebApplicationFactory` so they don't actually run during smoke tests.
- **No EF migrations yet.** The schema is built on first boot via `EnsureCreated`. If you change entity shapes, delete the SQLite file and restart. EF migrations are on the roadmap but not done yet — that's a known gap, not an oversight.

---

## Style

There's no formal style guide. The codebase does this:

- Nullable reference types are on.
- Table and column names are snake_case via `EFCore.NamingConventions` — don't fight it.
- Comments only where the code is non-obvious. If you're writing a comment to explain what the next line does, delete the line and rename instead.
- Prefer `await` and `async` over `.Result` / `.Wait()`.

If your IDE adds stuff automatically (using directives, file-scoped namespaces, etc.), let it. Don't hand-format against the auto-formatter.

---

## Reporting issues

Include: Lopingo version or commit, how you're running it (Docker or `dotnet run`), which env vars you set (redact secrets), and the steps to reproduce. If the worker is involved, the logs around `MonitorCheckWorker started` are usually enough.

---

## License

By contributing, you agree your contributions will be licensed under the [MIT License](LICENSE).
This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).