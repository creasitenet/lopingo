# Security Policy

## Supported versions

Security fixes are applied to the latest release on `main`. There are no long-term support branches yet.

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security problems.

Email the maintainer at the address listed on the [GitHub profile](https://github.com/creasitenet) for the `creasitenet/lopingo` repository, with:

- Lopingo version or commit
- How you run it (Docker Compose, `dotnet run`, …)
- A clear description of the issue and steps to reproduce
- Impact (who can trigger it, what they gain)

You should get an acknowledgement within a few days. Please give us reasonable time to fix and release before any public disclosure.

## Threat model (self-hosted)

Lopingo is a **single-owner** uptime monitor. The person who can sign in is trusted with the instance.

Known implications worth understanding before you expose it on the internet:

- **HTTP probes can reach internal URLs.** The owner can add monitors for `http://127.0.0.1`, private LAN hosts, or cloud metadata endpoints. That is useful for homelab monitoring and is intentional. Do not share the owner account. Put Lopingo behind a reverse proxy and keep the UI off the public internet if that risk matters to you.
- **Telegram bot tokens** are stored in the SQLite database. Protect `data/lopingo.db` like a secrets file.
- **Trust reverse-proxy headers.** When `X-Forwarded-*` is present, Lopingo trusts them (common for Caddy / Nginx / Traefik). Only expose the app through a proxy you control; do not put an untrusted hop in front of it.
- **First signup wins.** Until an owner exists, `/signup` is open on the instance. Complete signup immediately after the first boot, or bind the port to localhost until then.
- **Container runs as root** today so the `./data` volume works without a host-side `chown`. Hardening to a non-root user is on the roadmap once image publish / volume docs are in place.
