# Installer

Goal: one-time installation of the Anki Desktop Companion with minimal user interaction.

## MVP requirements

- Install companion under `%LOCALAPPDATA%\AnkiImporter`.
- Register automatic startup for the current Windows user.
- Verify that Anki Desktop is installed or at least provide a clear message when it is not found.
- Verify AnkiConnect availability after Anki is opened.
- Support uninstall.

## Distribution target

Phase 1: signed PowerShell/bootstrap installer for development testing.

Phase 2: signed Windows installer (MSIX/MSI or equivalent) with automatic updates.

The end-user experience should avoid manual ZIP extraction and recurring PowerShell commands.
