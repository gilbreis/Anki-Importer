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

1. Baixe `AnkiImporterSetup.exe` pelo botão **Baixar Anki Importer** na página principal do projeto.
2. Dê duplo clique no instalador.

#### Aviso do Windows SmartScreen

Na primeira instalação, o Windows pode mostrar a mensagem:

```text
Windows protected your PC
Microsoft Defender SmartScreen prevented an unrecognized app from starting.
Publisher: Unknown publisher
```

Isso acontece porque o `AnkiImporterSetup.exe` ainda **não possui um certificado comercial de assinatura de código**. Por esse motivo, o Windows não consegue mostrar um publicador verificado e trata o instalador como um aplicativo ainda sem reputação conhecida.

**Esse aviso, por si só, não significa que o Windows encontrou vírus ou malware no Anki Importer.** Ele informa que o executável não está assinado por um publicador reconhecido pelo SmartScreen.

O projeto é aberto e o código-fonte utilizado para gerar o instalador está disponível neste próprio repositório GitHub.

Se você baixou o arquivo pelo botão oficial deste repositório, prossiga assim:

1. Na janela **Windows protected your PC**, clique em **More info / Mais informações**.
2. Confira que o aplicativo exibido é `AnkiImporterSetup.exe`.
3. Clique em **Run anyway / Executar mesmo assim**.
4. O instalador do Anki Importer será aberto normalmente.

> Evite executar cópias do instalador recebidas por e-mail, mensagens ou sites de terceiros. Prefira sempre o botão de download deste repositório.

#### Continue a instalação

1. Clique em **Next / Avançar**.
2. Clique em **Install / Instalar**.
3. Clique em **Finish / Concluir**.

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
- criação apenas dos cartões realmente novos;
- complemento de áudio em cartões existentes que ainda não possuem áudio.

### Passo 5 — Confira o resultado

Ao terminar aparece uma janela semelhante a:

```text
Deck: English

Encontradas: 20
Novos cartões: 12
Duplicadas ignoradas: 5
Áudio adicionado em existentes: 3
Inválidas: 0
Áudios em novos cartões: 12
Erros de áudio em novos: 0
Erros de áudio em existentes: 0
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

## 5. Duplicados e cartões já existentes

O Anki Importer nunca cria um segundo cartão com o mesmo `Front` dentro do mesmo deck.

A regra é:

```text
Palavra não existe no deck
→ cria o cartão com texto + áudio

Palavra já existe e já possui áudio no Back
→ não cria outro cartão e ignora a entrada

Palavra já existe, mas ainda não possui áudio no Back
→ não cria outro cartão
→ mantém o texto existente
→ adiciona somente o áudio ao Back existente
```

Exemplo:

Se o deck `English` já contém:

```text
Front: dividir
Back: split
```

sem áudio, uma nova importação de:

```text
dividir = split
```

não criará outro cartão. O cartão existente será atualizado para algo equivalente a:

```text
Front: dividir
Back: split + 🔊 áudio em inglês
```

Se o cartão já contiver um marcador de áudio `[sound:...]`, ele será considerado completo e será ignorado.

Nenhum cartão existente é apagado e nenhum cartão duplicado é criado.

---

## 6. Se algo não funcionar

### O Windows mostra "Windows protected your PC"

Isso pode acontecer porque o instalador ainda aparece como **Unknown publisher**. Volte à seção **1.3 Instale o Anki Importer** deste guia e siga **More info / Mais informações → Run anyway / Executar mesmo assim**.

Esse aviso não é, sozinho, uma detecção de vírus. Baixe sempre o instalador diretamente deste repositório.

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
Baixar AnkiImporterSetup.exe do GitHub
   ↓
Se aparecer SmartScreen:
More info → Run anyway
   ↓
Instalar Anki Importer
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
Novos cartões são criados
Cartões existentes sem áudio recebem o áudio
Duplicados completos são ignorados
   ↓
Pronto
```
