# Contrato (delta): e-mail do código de verificação de exclusão

Estende/substitui, só neste ponto, o contrato já existente em [specs/044-excluir-professor-2fa/contracts/email-codigo-verificacao.md](../../044-excluir-professor-2fa/contracts/email-codigo-verificacao.md). Mesma infraestrutura de envio (`IEmailSender.EnviarAsync`), mesmo assunto, mesma validade/uso único do código — não repetidos aqui. Nenhum endpoint muda de rota/assinatura.

## Destinatário (MUDOU)

Antes (specs/044): sempre o e-mail da conta Admin autenticada.

**Agora**: sempre o e-mail cadastrado da **professora-alvo** da exclusão (`professor.Email`, já carregada em `SolicitarExclusaoAsync`) — nunca mais o e-mail do Admin. Se a professora não tiver e-mail cadastrado, a solicitação é recusada antes de qualquer tentativa de envio (ver "Novo erro de negócio" abaixo).

## Assunto (inalterado)

`"SPI - Código de confirmação para excluir professora"`

## Corpo (MUDOU — dirigido à professora, não ao Admin)

```html
<p>Olá, {ProfessorNome}.</p>
<p>O Admin {AdminNome} solicitou a exclusão do seu cadastro no SPI. Se você concorda, informe o código abaixo a ele para confirmar. Ele expira em 15 minutos e só pode ser usado uma vez.</p>
<p style="font-size:20px;font-weight:bold;letter-spacing:1px;">{Codigo}</p>
<p>Se você não esperava este e-mail, não compartilhe o código e entre em contato para entender o que está havendo.</p>
```

## Novo erro de negócio (409, `POST /api/professor/admin/excluir/solicitar-codigo`)

- `A professora nao tem e-mail cadastrado; nao e possivel enviar o codigo de confirmacao.` — novo, cobre FR-004. Nenhum token é criado quando esse erro ocorre.

## O que não muda

- Quem digita o código na tela de confirmação continua sendo o Admin (`ConfirmarExclusaoAsync` intocado).
- `AdminId` continua gravado em `ExclusaoProfessorToken`, para rastreabilidade de quem solicitou (FR-003).
- Falha de envio (ex.: SMTP fora do ar) continua sendo propagada como erro ao Admin, nunca mascarada (specs/044, inalterado).
