# Anki Importer

Importador local e gratuito de vocabulário do ChatGPT para o Anki Desktop.

## Como funciona

```text
TXT + nome do deck
       ↓
ChatGPT gera .ankiimport
       ↓
duplo clique no arquivo
       ↓
Anki Importer local
       ↓
AnkiConnect
       ↓
Anki Desktop
```

Não há servidor, login adicional, conta, token, mensalidade ou processo permanente em segundo plano.

## Para o usuário final

Leia o passo a passo em [docs/GUIA-USUARIO.md](docs/GUIA-USUARIO.md).

Resumo:

1. Instale o Anki Desktop.
2. Instale o AnkiConnect no Anki.
3. Instale `AnkiImporterSetup.exe` uma vez.
4. No ChatGPT, envie o TXT e informe o nome do deck.
5. Baixe o `.ankiimport` gerado e dê duplo clique.
6. O importador verifica duplicados, adiciona apenas cartões novos, mostra o relatório e fecha.

## Formato `.ankiimport`

```json
{
  "deck": "English",
  "model": "Basic",
  "frontField": "Front",
  "backField": "Back",
  "cards": [
    { "front": "montar", "back": "assemble" },
    { "front": "dividir", "back": "split" }
  ]
}
```

O deck pode ter qualquer nome. Se não existir, o importador cria automaticamente.

## Regras

- Português → `Front`
- Inglês → `Back`
- Modelo padrão → `Basic`
- Duplicados no arquivo são ignorados
- Duplicados já existentes no deck são ignorados
- Cartões existentes não são alterados ou apagados
- UTF-8 e acentos são preservados

## Estrutura do repositório

```text
local-importer/             aplicativo local em Python
installer/                  instalador Windows e associação .ankiimport
docs/GUIA-USUARIO.md        manual do usuário final
.github/workflows/          build e release do Setup.exe
```

## Build

O GitHub Actions gera o instalador gratuitamente:

```text
Python → PyInstaller → AnkiImporter.exe → Inno Setup → AnkiImporterSetup.exe
```
