# Installer

Goal: make Anki Importer feel like a normal Windows application for end users.

## End-user experience

The supported installation path is:

```text
Download AnkiImporterSetup.exe
→ Next
→ Next
→ Install
→ Connect to ChatGPT
→ Finish
```

The user must not be asked to configure:
- server URLs
- MCP endpoints
- bearer tokens
- WebSocket addresses
- ports
- environment variables
- PowerShell commands
- JSON files

Those details are implementation concerns only.

## Installer responsibilities

- Install `AnkiImporter.Companion.exe` for the current Windows user.
- Register automatic startup.
- Open a graphical first-run wizard.
- Detect Anki Desktop / AnkiConnect.
- Pair the computer securely with the user's ChatGPT integration.
- Store device credentials using Windows-protected storage.
- Start the Companion silently after setup.
- Support uninstall.

## Pairing UX

Target UX: one button labeled **Connect to ChatGPT**.

A short pairing code may exist internally and can remain available as a development/recovery fallback, but copying a pairing code manually must not be the normal end-user flow.

## Distribution

GitHub Releases publish a single end-user artifact:

```text
AnkiImporterSetup.exe
```

The raw Companion executable is an internal component and is not the primary download offered to users.

## Security / release requirements

Before public distribution:
- use a production HTTPS/WSS service URL embedded by the release pipeline;
- code-sign the installer and Companion;
- replace development bearer-token account mapping with production authentication/OAuth;
- keep AnkiConnect bound to localhost only;
- provide automatic updates or a simple in-app update path.
