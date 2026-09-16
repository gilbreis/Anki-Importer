# Guia do Usuário — Anki Importer

O Anki Importer adiciona palavras do ChatGPT ao Anki Desktop com poucos passos e sem configuração técnica.

## 1. Preparação — apenas na primeira vez

### 1.1 Instale o Anki Desktop

Tenha o **Anki Desktop** instalado no Windows.

### 1.2 Instale o AnkiConnect — obrigatório

O Anki Importer usa o **AnkiConnect** para conversar com o Anki Desktop. Sem esse complemento, a importação não funciona.

Página oficial do complemento:

https://ankiweb.net/shared/info/2055492159

Código do complemento:

```text
2055492159
```

#### Passo a passo

1. Abra o **Anki Desktop**.
2. No menu superior, clique em **Ferramentas**.
3. Clique em **Complementos / Add-ons**.
4. Na janela de complementos, clique em **Obter Complementos / Get Add-ons**.
5. No campo exibido, digite exatamente:

```text
2055492159
```

6. Clique em **OK** para instalar.
7. Aguarde a confirmação da instalação.
8. Feche completamente o Anki Desktop.
9. Abra o Anki Desktop novamente.

Pronto. O AnkiConnect fica instalado e o Anki Importer poderá acessar o Anki localmente.

> Importante: o Anki Desktop precisa estar aberto quando você importar um arquivo `.ankiimport`.

### 1.3 Instale o Anki Importer

1. Baixe `AnkiImporterSetup.exe`.
2. Dê duplo clique no instalador.
3. Clique em **Next / Avançar**.
4. Clique em **Install / Instalar**.
5. Clique em **Finish / Concluir**.

Essa instalação é feita uma única vez. Você não precisa instalar Python.

---

## 2. Uso no dia a dia

### Passo 1 — Abra o Anki

Abra o **Anki Desktop** e deixe-o aberto.

### Passo 2 — Envie o TXT ao ChatGPT

Anexe o TXT recebido do professor e informe o deck desejado.

Exemplo:

```text
Deck "English"
```

O nome do deck pode ser qualquer um, por exemplo:

```text
Deck "Business English"
Deck "Aula 15"
Deck "Inglês::Vocabulário"
```

### Passo 3 — Baixe o arquivo gerado

O ChatGPT devolverá um arquivo com extensão:

```text
.ankiimport
```

Exemplo:

```text
English.ankiimport
```

### Passo 4 — Dê duplo clique

Abra o `.ankiimport` com duplo clique.

O Anki Importer fará automaticamente:

- leitura das palavras;
- criação do deck, caso ainda não exista;
- verificação de palavras duplicadas no próprio arquivo;
- verificação de palavras que já existem no deck;
- geração do áudio em inglês;
- inclusão somente das palavras novas.

### Passo 5 — Confira o resultado

Ao terminar aparece uma janela semelhante a:

```text
Deck: English

Encontradas: 20
Adicionadas: 17
Duplicadas: 3
Inválidas: 0
Áudios TTS: 17
Erros de áudio: 0
Erros: 0
```

Depois disso o Anki Importer fecha. Não existe programa que precise ficar rodando em segundo plano.

---

## 3. Como cada cartão é criado

Para uma linha recebida assim:

```text
montar = assemble
```

o cartão será criado assim:

```text
Front:
montar

Back:
assemble
🔊 áudio em inglês
```

O padrão utilizado é:

```text
Modelo: Basic
Português: Front
Inglês: Back
Áudio TTS em inglês: Back
```

O áudio é salvo na mídia do próprio Anki e associado ao verso do cartão.

---

## 4. Áudio TTS em inglês

O Anki Importer usa **Google Translate TTS por meio da biblioteca gTTS** para gerar a pronúncia da palavra ou expressão em inglês.

Não é necessária chave de API, cadastro adicional ou serviço pago.

Para gerar o áudio, o computador precisa estar conectado à internet no momento da importação.

Se a internet estiver indisponível ou o serviço de TTS falhar:

- o cartão de texto ainda é criado normalmente;
- o campo `Back` continua contendo a tradução em inglês;
- o relatório informa quantos áudios falharam.

Isso evita perder a importação por causa de um problema temporário de áudio.

---

## 5. Duplicados

O importador não adiciona novamente uma palavra que já exista no mesmo deck.

Exemplo: se `montar` já estiver no deck `English`, ela será contabilizada como **Duplicada** e não será criada novamente.

Nenhum cartão existente é alterado ou apagado.

---

## 6. Se algo não funcionar

### Mensagem: não foi possível acessar o Anki

Confira:

1. O **Anki Desktop está aberto**.
2. O **AnkiConnect está instalado**.
3. Você reiniciou o Anki depois de instalar o AnkiConnect.

Se tiver dúvida sobre o AnkiConnect, abra:

https://ankiweb.net/shared/info/2055492159

Código:

```text
2055492159
```

Depois dê duplo clique novamente no `.ankiimport`.

### O áudio não foi criado

Confira se o computador está conectado à internet. O cartão textual pode ser importado mesmo sem o áudio.

### O Windows não abre o `.ankiimport`

Execute novamente `AnkiImporterSetup.exe`. O instalador registra automaticamente esse tipo de arquivo no Windows.

---

## Resumo

### Primeira vez

```text
Instalar Anki
   ↓
Instalar AnkiConnect (2055492159)
   ↓
Reiniciar Anki
   ↓
Instalar AnkiImporterSetup.exe
```

### Uso normal

```text
Abrir Anki
   ↓
Enviar TXT + nome do deck ao ChatGPT
   ↓
Baixar .ankiimport
   ↓
Dar duplo clique
   ↓
Texto + áudio adicionados ao Anki
   ↓
Pronto
```
