# Roadmap

Open an issue if something important is missing or if you want to work on one of these.

## Near term

- **EF Core migrations** — replace `EnsureCreated` so schema changes survive upgrades
- **Monitor display name** — friendly label separate from the URL
- **Pause / resume** a monitor without deleting it
- **Clone** a monitor (config + linked Telegram bots)

## Probes

- Per-monitor timeout, retry count, retry delay
- HTTP method (GET / HEAD / POST), optional body and headers
- Accepted status codes (e.g. treat `204` or `301` as up)
- "Ignore TLS errors" toggle
- Failure threshold across ticks (not only in-tick retries)

## SSL / TLS

- Read cert issuer / validity / days remaining on HTTPS monitors
- Show expiry on the monitor page; warn when close
- Telegram alert N days before expiry

## Product polish

- Dashboard search (and pagination if lists get huge)
- Public status page (`/status`, no auth)
- More notification channels: email, Slack, Discord, webhooks
- Export / import monitors as JSON
- GHCR image published from CI on tag; compose example using the published image

## Later

- **Daily rollups / aggregates** — keep long-term uptime % and avg latency without storing every 1-minute check for 30+ days
- More probe types: TCP, ICMP, DNS, keyword-in-body, heartbeat/push
- Multi-owner / multi-tenant
- Multi-instance (today the live UI bus is in-memory — one process only)
