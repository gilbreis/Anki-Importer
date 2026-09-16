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

> **Aviso do Windows SmartScreen**
>
> O instalador ainda não possui certificado comercial de assinatura de código e pode aparecer como **Unknown publisher**. Nesse caso o Windows pode exibir **Windows protected your PC**.
>
> Esse aviso, por si só, **não significa que o Windows detectou vírus**; significa que o executável ainda não tem um publicador reconhecido pelo SmartScreen.
>
> Se você baixou o arquivo pelo botão oficial acima, clique em **More info / Mais informações** e depois em **Run anyway / Executar mesmo assim**.
>
> Para detalhes, consulte o [Guia de Instalação](docs/GUIA-USUARIO.md).

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

O AnkiConnect é obrigatório:

- Página: https://ankiweb.net/shared/info/2055492159
- Código: `2055492159`

Resumo:

1. Instale o Anki Desktop.
2. Instale o AnkiConnect no Anki e reinicie o Anki.
3. Instale `AnkiImporterSetup.exe` uma vez.
4. No ChatGPT, envie o TXT e informe o nome do deck.
5. Baixe o `.ankiimport` gerado e dê duplo clique.
6. O importador verifica duplicados, gera o áudio em inglês, adiciona apenas cartões novos, mostra o relatório e fecha.

## Formato `.ankiimport`

```json
{
  "deck": "English",
  "model": "Basic",
  "frontField": "Front",
  "backField": "Back",
  "tts": true,
  "ttsLanguage": "en",
  "cards": [
    { "front": "montar", "back": "assemble" },
    { "front": "dividir", "back": "split" }
  ]
}
```

O deck pode ter qualquer nome. Se não existir, o importador cria automaticamente.

`tts` é opcional e assume `true` por padrão. O idioma padrão do áudio é inglês (`en`).

## Como o cartão fica

```text
Front: montar
Back: assemble + áudio TTS em inglês
```

O áudio é gerado com gTTS, salvo na mídia do Anki via AnkiConnect e referenciado no campo `Back` como `[sound:arquivo.mp3]`.

A geração do TTS requer internet no momento da importação, mas não exige chave de API nem serviço pago. Se o áudio falhar, o cartão textual continua sendo criado.

## Regras

- Português → `Front`
- Inglês → `Back`
- TTS em inglês → `Back`
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
Python + gTTS → PyInstaller → AnkiImporter.exe → Inno Setup → AnkiImporterSetup.exe
```
