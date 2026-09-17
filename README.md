# Anki Importer

Importador local e gratuito de vocabulário do ChatGPT para o Anki Desktop.

<p align="center">
  <a href="https://github.com/gilbreis/Anki-Importer/releases/download/latest/AnkiImporterSetup.exe">
    <img alt="Baixar Anki Importer" src="https://img.shields.io/badge/Baixar-AnkiImporterSetup.exe-2ea44f?style=for-the-badge&logo=windows">
  </a>
  <a href="https://github.com/gilbreis/Anki-Importer/releases/download/latest/anki-importer-skill.zip">
    <img alt="Baixar Skill do ChatGPT" src="https://img.shields.io/badge/Baixar-Skill%20ChatGPT-6f42c1?style=for-the-badge&logo=openai">
  </a>
  <a href="docs/GUIA-USUARIO.md">
    <img alt="Guia de Instalação" src="https://img.shields.io/badge/Guia-Instalação-0969da?style=for-the-badge&logo=readthedocs">
  </a>
</p>

> **Windows SmartScreen:** o instalador ainda pode aparecer como **Unknown publisher** por não possuir certificado comercial de assinatura. Isso, por si só, não significa detecção de vírus. Consulte o guia para o passo a passo.

## Uso no ChatGPT

Com a Skill **Anki Importer** instalada, em um novo chat basta:

```text
1. Anexar o TXT
2. Escrever:
   Deck "English"
```

A Skill reconhece automaticamente as linhas `português = inglês`, remove duplicatas dentro do próprio TXT e gera o `.ankiimport` pronto para abrir no Windows.

Veja: [Como instalar a Skill no ChatGPT](docs/INSTALAR-SKILL-CHATGPT.md).

> A disponibilidade de Skills depende do plano e das configurações da conta/workspace do ChatGPT. Atualmente, a documentação oficial da OpenAI lista Skills para usuários elegíveis de Business, Enterprise, Healthcare e Edu.

## Arquitetura

```text
TXT + deck
   ↓
Skill Anki Importer no ChatGPT
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
chatgpt-skill/              Skill para gerar .ankiimport no ChatGPT
installer/                  instalador Windows e associação .ankiimport
docs/GUIA-USUARIO.md        manual do usuário final
.github/workflows/          build e release
```

## Build

```text
Python → PyInstaller → AnkiImporter.exe → Inno Setup → AnkiImporterSetup.exe
```
