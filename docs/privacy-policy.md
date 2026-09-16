# Privacy Policy Draft — Anki Importer

_Last updated: 2026-09-16_

This document is a draft for product development and app-directory preparation. It should be reviewed before public launch and updated with the final operator identity, contact information, hosting providers, retention periods, and production URLs.

## What Anki Importer does

Anki Importer helps users send vocabulary cards from ChatGPT to a paired Anki Desktop installation.

## Data processed

The service may process:

- vocabulary text supplied by the user for import;
- destination deck names;
- Anki note model and field names required for the import;
- short-lived pairing codes;
- generated device identifiers;
- device authentication tokens;
- connection and operational metadata needed to route requests to the correct paired device;
- import result metadata such as counts of added, duplicate, invalid, or failed cards.

## Data not intentionally collected

The service does not need the user's AnkiWeb password and should never request it.

AnkiConnect remains local to the user's computer at `127.0.0.1:8765`. The service does not require users to expose the AnkiConnect port to the public internet.

## How data is used

Data is used only to:

- authenticate and route requests to the user's paired Anki Desktop Companion;
- check whether vocabulary cards already exist;
- add requested vocabulary cards;
- return operation status and results;
- maintain and secure the user's device pairing.

## Device credentials

The Desktop Companion receives a unique device credential after pairing. On supported Windows installations, the Companion protects the local credential using Windows-provided protected storage.

The hosted service stores the server-side device credential required to authenticate Companion connections. Production storage must be access-controlled and excluded from source control.

## Vocabulary content

Vocabulary text may transit the hosted MCP/relay service while an import request is being routed to the user's Companion. The production service should minimize logging of vocabulary payloads and should not use imported vocabulary for advertising or unrelated profiling.

## Retention

Final production retention periods must be documented before launch. The intended design is to retain only the minimum persistent data needed for account/device pairing and security, while operational request payloads should not be retained longer than necessary to execute and diagnose requests.

## Sharing

Data may be processed by infrastructure providers used to host the service. A final production policy must name or categorize applicable subprocessors and link to their privacy terms where required.

## Security

The project is designed so that:

- AnkiConnect is not exposed to the internet;
- the Desktop Companion initiates outbound connections;
- devices use unique authentication credentials;
- user accounts are isolated from one another at the device-routing layer;
- existing Anki cards are not modified or deleted by the vocabulary-import workflow.

## User controls

The product should provide users with a way to unpair a device and revoke its access. Account deletion and server-side data deletion procedures must be finalized before public launch.

## Contact

TODO before public launch: add the legal/operator name and a privacy/support contact address.
