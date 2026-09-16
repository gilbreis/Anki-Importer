# Anki Importer

Anki Importer importa vocabulário do ChatGPT para o Anki Desktop com o mínimo de passos possível.

## Experiência do usuário

1. Instale `AnkiImporterSetup.exe` uma única vez.
2. Abra o Anki Desktop com o AnkiConnect instalado.
3. No ChatGPT, anexe o `.txt` e informe o deck desejado, por exemplo:

```text
Deck "English"
```

4. O ChatGPT gera um arquivo `.ankiimport`.
5. Dê duplo clique nesse arquivo.
6. O Anki Importer verifica duplicados, adiciona somente os cartões novos, mostra o relatório e fecha.

Não há servidor, login adicional, conta, token, URL, terminal, mensalidade ou processo permanente em segundo plano.

## Arquitetura

```text
TXT + nome do deck
       |
       v
ChatGPT gera .ankiimport
       |
       v
duplo clique no Windows
       |
       v
Anki Importer local (.exe)
       |
       v
AnkiConnect 127.0.0.1:8765
       |
       v
Anki Desktop
```

## Formato `.ankiimport`

O arquivo é JSON UTF-8 com esta estrutura:

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

O nome do deck pode ser qualquer um. Se o deck não existir, o importador cria automaticamente.

## Regras de importação

- Português vai para `Front`.
- Inglês vai para `Back`.
- Modelo padrão: `Basic`.
- Campos padrão: `Front` e `Back`.
- Duplicados dentro do próprio pacote são ignorados.
- Duplicados já existentes no deck são ignorados.
- Cartões existentes nunca são sobrescritos ou apagados.
- Acentos são preservados em UTF-8.

## Relatório

Ao terminar, o aplicativo mostra algo como:

```text
Deck: English

Encontradas: 20
Adicionadas: 17
Duplicadas: 3
Inválidas: 0
Erros: 0
```

## Requisitos

- Windows x64.
- Anki Desktop.
- AnkiConnect instalado no Anki.

O usuário não precisa instalar Python. O aplicativo Python é empacotado como `.exe` com PyInstaller e distribuído dentro do `AnkiImporterSetup.exe`.

## Build

O GitHub Actions gera gratuitamente:

```text
AnkiImporterSetup.exe
```

Fluxo de build:

```text
Python
  -> PyInstaller
  -> AnkiImporter.exe
  -> Inno Setup
  -> AnkiImporterSetup.exe
```

## Estrutura principal

```text
local-importer/       Importador local em Python
installer/            Instalador Windows e associação .ankiimport
.github/workflows/    Build e release do Setup.exe
```

Os diretórios antigos de experimentos remotos podem permanecer temporariamente no repositório, mas não fazem parte da arquitetura do produto local.
