# Anki Importer

Importador local e gratuito de vocabulário do ChatGPT para o Anki Desktop.

<p align="center">
  <a href="https://github.com/gilbreis/Anki-Importer/releases/download/latest/AnkiImporterSetup.exe">
    <img alt="Baixar Anki Importer" src="https://img.shields.io/badge/Baixar-AnkiImporterSetup.exe-2ea44f?style=for-the-badge&logo=windows">
  </a>
  <a href="docs/GUIA-USUARIO.md">
    <img alt="Guia de Instalação" src="https://img.shields.io/badge/Guia-Instalação-0969da?style=for-the-badge&logo=readthedocs">
  </a>
</p>

> **Windows SmartScreen:** o instalador ainda pode aparecer como **Unknown publisher** por não possuir certificado comercial de assinatura. Isso, por si só, não significa detecção de vírus. Consulte o guia para o passo a passo.

## Arquitetura

```text
TXT + deck
   ↓
ChatGPT gera .ankiimport
   ↓
Anki Importer local
   ↓
AnkiConnect
   ↓
Anki Importer AwesomeTTS Bridge
   ↓
AwesomeTTS
   ↓
Google Translate / en-US / speed 1.0
   ↓
Anki Desktop
```

Não há servidor, login, token, mensalidade ou processo permanente em segundo plano.

## Requisitos

- Anki Desktop
- AnkiConnect — código `2055492159`
- AwesomeTTS — código `1436550454`
- `AnkiImporterSetup.exe`

O Setup instala automaticamente o **Anki Importer AwesomeTTS Bridge** na pasta de add-ons do Anki. Depois da instalação, reinicie o Anki.

## Regras

- Português → `Front`
- Inglês → `Back`
- Áudio → gerado pelo próprio AwesomeTTS
- Serviço → Google Translate
- Voz → English, American (`en-US`)
- Velocidade → `1.0`
- Se o cartão não existe → cria + áudio
- Se já existe com áudio → ignora
- Se já existe sem áudio → adiciona apenas o áudio ao `Back`
- Nunca cria cartão duplicado dentro do mesmo deck

## Formato `.ankiimport`

```json
{
  "deck": "English",
  "model": "Basic",
  "frontField": "Front",
  "backField": "Back",
  "tts": true,
  "cards": [
    { "front": "montar", "back": "assemble" },
    { "front": "dividir", "back": "split" }
  ]
}
```

O deck pode ter qualquer nome. Se não existir, é criado automaticamente.

## Estrutura

```text
local-importer/             aplicativo Windows em Python
anki-addon/                 bridge entre AnkiConnect e AwesomeTTS
installer/                  instalador Windows e associação .ankiimport
docs/GUIA-USUARIO.md        manual do usuário final
.github/workflows/          build e release
```

## Build

```text
Python → PyInstaller → AnkiImporter.exe → Inno Setup → AnkiImporterSetup.exe
```
