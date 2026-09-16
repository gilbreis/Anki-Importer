# Guia do Usuário — Anki Importer

O Anki Importer foi feito para adicionar palavras do ChatGPT ao Anki Desktop sem configuração técnica.

## 1. Preparação — apenas na primeira vez

### Instale o Anki Desktop

Tenha o Anki Desktop instalado no Windows.

### Instale o AnkiConnect

1. Abra o Anki.
2. Entre em **Ferramentas → Complementos (Add-ons)**.
3. Escolha **Obter Complementos / Get Add-ons**.
4. Informe o código:

```text
2055492159
```

5. Confirme a instalação.
6. Feche e abra o Anki novamente.

### Instale o Anki Importer

1. Baixe `AnkiImporterSetup.exe`.
2. Abra o instalador.
3. Clique em **Next / Avançar**.
4. Clique em **Install / Instalar**.
5. Clique em **Finish / Concluir**.

Pronto. Essa instalação é feita uma única vez.

---

## 2. Uso no dia a dia

### Passo 1 — Abra o Anki

Deixe o **Anki Desktop aberto**.

### Passo 2 — Envie o arquivo ao ChatGPT

Anexe o TXT recebido do professor e informe o deck desejado.

Exemplo:

```text
Deck "English"
```

O nome pode ser qualquer um, por exemplo:

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
- inclusão somente das palavras novas.

### Passo 5 — Confira o resultado

Ao terminar aparece uma janela semelhante a:

```text
Deck: English

Encontradas: 20
Adicionadas: 17
Duplicadas: 3
Inválidas: 0
Erros: 0
```

Depois disso o Anki Importer fecha. Não existe programa que precise ficar rodando em segundo plano.

---

## 3. Como as palavras são criadas

Para uma linha recebida assim:

```text
montar = assemble
```

o cartão será:

```text
Front: montar
Back: assemble
```

O padrão utilizado é:

```text
Modelo: Basic
Português: Front
Inglês: Back
```

---

## 4. Duplicados

O importador não adiciona novamente uma palavra que já exista no mesmo deck.

Exemplo: se `montar` já estiver no deck `English`, ela será contabilizada como **Duplicada** e não será criada novamente.

Nenhum cartão existente é alterado ou apagado.

---

## 5. Se algo não funcionar

### Mensagem: não foi possível acessar o Anki

Confira somente duas coisas:

1. O **Anki Desktop está aberto**.
2. O **AnkiConnect está instalado**.

Depois dê duplo clique novamente no `.ankiimport`.

### O Windows não abre o `.ankiimport`

Execute novamente `AnkiImporterSetup.exe`. O instalador registra automaticamente esse tipo de arquivo no Windows.

---

## Resumo

Depois da instalação inicial, o uso normal é apenas:

```text
Abrir Anki
   ↓
Enviar TXT + nome do deck ao ChatGPT
   ↓
Baixar .ankiimport
   ↓
Dar duplo clique
   ↓
Pronto
```
