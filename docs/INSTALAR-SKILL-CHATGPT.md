# Instalar a Skill do Anki Importer no ChatGPT

A Skill do **Anki Importer** permite usar o fluxo em novos chats sem repetir instruções longas.

Depois de instalada, o uso esperado é:

```text
1. Anexe o TXT
2. Escreva apenas:
   Deck "English"
```

O ChatGPT identifica o vocabulário no formato `português = inglês` e gera o arquivo `.ankiimport` automaticamente.

## Disponibilidade

Skills são um recurso do ChatGPT cuja disponibilidade depende do plano e das configurações da conta ou workspace.

Se sua conta tiver a aba **Skills / Habilidades** no Diretório de Plugins, siga o processo abaixo.

## Instalação

1. Baixe o arquivo `anki-importer-skill.zip` na Release mais recente deste repositório.
2. No ChatGPT, abra **Plugins** na barra lateral.
3. Abra a aba **Skills / Habilidades**.
4. Clique em **Create / Criar**.
5. Escolha **Upload from your computer / Carregar do computador**.
6. Selecione `anki-importer-skill.zip`.
7. Aguarde a verificação do ChatGPT.
8. Instale/ative a Skill.

## Como usar em qualquer novo chat

Anexe um TXT de vocabulário e escreva somente:

```text
Deck "English"
```

Outros exemplos:

```text
Deck "Business English"
Deck "Aula 20"
Deck "English::Vocabulary"
```

A Skill deve gerar um arquivo `.ankiimport` contendo:

- Português no `Front`;
- Inglês no `Back`;
- `Basic` como modelo padrão;
- TTS ativado;
- duplicatas do próprio TXT removidas.

A verificação de duplicatas que já existem no Anki é feita apenas no computador do usuário, pelo **Anki Importer** local.

## Pré-requisitos no Windows

Para abrir o `.ankiimport`, o usuário precisa ter:

- Anki Desktop;
- AnkiConnect (`2055492159`);
- AwesomeTTS (`1436550454`);
- Anki Importer instalado.

Consulte `GUIA-USUARIO.md` para o passo a passo completo.
