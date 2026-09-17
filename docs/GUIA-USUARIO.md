# Guia do Usuário — Anki Importer

O Anki Importer adiciona vocabulário do ChatGPT ao Anki Desktop e usa o próprio AwesomeTTS do Anki para gerar a pronúncia.

## 1. Preparação — apenas na primeira vez

### 1.1 Instale o Anki Desktop

Tenha o **Anki Desktop** instalado no Windows.

### 1.2 Instale o AnkiConnect — obrigatório

1. Abra **Ferramentas → Complementos → Obter Complementos**.
2. Informe o código:

```text
2055492159
```

Página oficial:
https://ankiweb.net/shared/info/2055492159

3. Confirme e reinicie o Anki.

### 1.3 Instale o AwesomeTTS — obrigatório para o áudio

1. Abra **Ferramentas → Complementos → Obter Complementos**.
2. Informe o código:

```text
1436550454
```

Página oficial:
https://ankiweb.net/shared/info/1436550454

3. Confirme e reinicie o Anki.

O Anki Importer usa automaticamente esta configuração do AwesomeTTS:

```text
Serviço: Google Translate
Voz: English, American (en-US)
Velocidade: 1.0
```

Não é necessário abrir a janela **Add TTS Audio to Note** nem clicar em **Record** durante uma importação.

### 1.4 Instale o Anki Importer

1. Baixe `AnkiImporterSetup.exe` pelo botão **Baixar Anki Importer** do repositório.
2. Execute o instalador.
3. Se o Windows mostrar **Windows protected your PC**, use **More info / Mais informações → Run anyway / Executar mesmo assim**. O aviso ocorre porque o instalador ainda não possui certificado comercial de assinatura; por si só, isso não significa uma detecção de vírus.
4. Clique em **Next → Install → Finish**.
5. **Feche completamente o Anki e abra novamente.**

O Setup instala automaticamente um pequeno complemento local chamado **Anki Importer AwesomeTTS Bridge**. Esse bridge permite que o programa chame o AwesomeTTS sem abrir janelas manuais.

---

## 2. Uso no dia a dia

1. Abra o **Anki Desktop** e deixe-o aberto.
2. No ChatGPT, anexe o TXT e informe o deck, por exemplo:

```text
Deck "English"
```

3. Baixe o `.ankiimport` gerado.
4. Dê duplo clique no arquivo.
5. Aguarde o relatório final.

O nome do deck pode ser qualquer um. Se ele não existir, será criado automaticamente.

---

## 3. Como cada cartão fica

Entrada:

```text
montar = assemble
```

Resultado:

```text
Front:
montar

Back:
assemble
[sound:arquivo-gerado-pelo-AwesomeTTS.mp3]
```

O áudio é gerado pelo **AwesomeTTS → Google Translate → English, American (en-US) → Speed 1.0** e salvo na mídia da própria coleção do Anki.

---

## 4. Regra de duplicados e áudio

```text
Não existe no deck
→ cria o cartão
→ AwesomeTTS adiciona o áudio ao Back

Já existe e já possui [sound:...] no Back
→ ignora
→ não duplica

Já existe, mas não possui áudio
→ NÃO cria outro cartão
→ mantém o conteúdo atual
→ AwesomeTTS adiciona somente o áudio ao Back existente
```

Nenhum cartão existente é apagado.

---

## 5. Relatório

Exemplo:

```text
Deck: English

Encontradas: 20
Novos cartões: 12
Duplicadas ignoradas: 5
Áudio AwesomeTTS em existentes: 3
Áudio AwesomeTTS em novos: 12
Inválidas: 0
Erros de áudio em existentes: 0
Erros de áudio em novos: 0
Erros: 0
```

---

## 6. Se o áudio não funcionar

Confira nesta ordem:

1. O Anki está aberto.
2. O AnkiConnect `2055492159` está instalado.
3. O AwesomeTTS `1436550454` está instalado.
4. O Anki foi reiniciado depois da instalação ou atualização do Anki Importer.
5. O computador possui internet para o Google Translate gerar o áudio.

Se aparecer a mensagem **Integração AwesomeTTS não está disponível**, reinstale `AnkiImporterSetup.exe` e reinicie o Anki.

---

## Resumo da primeira instalação

```text
Instalar Anki
   ↓
Instalar AnkiConnect (2055492159)
   ↓
Instalar AwesomeTTS (1436550454)
   ↓
Reiniciar Anki
   ↓
Instalar AnkiImporterSetup.exe
   ↓
Reiniciar Anki novamente
   ↓
Pronto
```

## Uso normal

```text
Abrir Anki
   ↓
Enviar TXT + Deck ao ChatGPT
   ↓
Baixar .ankiimport
   ↓
Duplo clique
   ↓
Texto + áudio AwesomeTTS
   ↓
Pronto
```
