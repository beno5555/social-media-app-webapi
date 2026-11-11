# 01 - Email off switch for tests

Status: ready-for-agent
Difficulty: routine

## Problem

`CustomWebApplicationFactory` does not replace `EmailSender`, so the integration suite sends real SMTP email on every run (deactivation, password reset, failed-register warning).

## What to do

- Add an on/off setting to `EmailConfiguration` (for example `Enabled`). When it is off, `EmailSender.SendAsync` sends nothing and reports success, so calling flows behave as before.
- Turn it off in the Testing environment. `appsettings.Testing.json` is gitignored: tell the user which key to add rather than printing or committing the file. Prefer setting it from `CustomWebApplicationFactory` so a fresh clone is safe by default.
- Keep to the "no interfaces" rule: a config switch, not a fake implementation.
- Add `EmailConfiguration:Enabled` to the config prerequisites list in `CLAUDE.md`.

## Done when

- The full integration suite passes and sends no email.
- With the setting on (the default outside Testing), sending is unchanged.
