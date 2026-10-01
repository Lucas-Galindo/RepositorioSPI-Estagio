# Contrato: e-mail do código de verificação de exclusão

Reaproveita `IEmailSender.EnviarAsync(destinatario, assunto, corpoHtml)` (spec 043, `BrevoEmailSender`) — nenhuma mudança na infraestrutura de envio em si, só um texto novo.

## Destinatário

Sempre o e-mail da conta Admin autenticada que solicitou a exclusão (`IAdminRepository.ObterPorIdAtivoAsync(adminId)`, `adminId` = `User.ObterUsuarioId()`) — **nunca** o e-mail da professora-alvo, e nunca um valor vindo do corpo da requisição (R3).

## Assunto

`"SPI - Código de confirmação para excluir professora"`

## Corpo (mesmo estilo HTML de `RecuperacaoSenhaService.EsqueciSenhaAsync`)

```html
<p>Olá, {AdminNome}.</p>
<p>Você solicitou a exclusão do cadastro da professora {ProfessorNome}. Use o código abaixo para confirmar. Ele expira em 15 minutos e só pode ser usado uma vez.</p>
<p style="font-size:20px;font-weight:bold;letter-spacing:1px;">{Codigo}</p>
<p>Se você não solicitou essa ação, ignore este e-mail e considere revisar o acesso à sua conta de Admin.</p>
```

## Diferença deliberada em relação ao e-mail de recuperação de senha

A recuperação de senha (spec 001/043) **nunca** revela ao cliente se o envio falhou (política de não-enumeração de e-mail). Este fluxo **é** diferente: o Admin já está autenticado, a ação é dele mesmo, e ele precisa saber se o e-mail não foi enviado para poder tentar de novo — por isso uma falha de envio aqui **é** reportada como erro (ver contracts/excluir-professor-api.md, `500`), não mascarada.
