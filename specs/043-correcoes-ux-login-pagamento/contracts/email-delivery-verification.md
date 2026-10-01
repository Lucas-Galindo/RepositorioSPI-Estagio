# Contrato: Verificação de envio real do e-mail de recuperação de senha

Sem mudança de endpoint — `POST /api/auth/esqueci-senha` continua com o mesmo contrato de request/response (sempre `200 OK` com corpo vazio/idêntico, exista ou não o e-mail — FR-003). O "contrato" aqui é o critério de aceite da **verificação de configuração**.

## Pré-requisito

Três chaves em User Secrets do projeto `src/SPI.Api` (nunca em `appsettings.json`):

```powershell
dotnet user-secrets set "Brevo:SmtpUsuario" "<login SMTP da Brevo>" --project src/SPI.Api
dotnet user-secrets set "Brevo:SmtpChave" "<SMTP key da Brevo>" --project src/SPI.Api
dotnet user-secrets set "Brevo:RemetenteEmail" "<e-mail remetente validado na Brevo>" --project src/SPI.Api
```

## Critério de aceite (SC-001)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com as 3 chaves configuradas, subir o backend | Nenhum erro de inicialização |
| 2 | `POST /api/auth/esqueci-senha` com o e-mail de uma conta de teste cadastrada e ativa | `200 OK`, corpo vazio/idêntico ao de um e-mail inexistente |
| 3 | Checar a caixa de entrada real do e-mail de teste | E-mail "SPI - Recuperação de senha" chega em poucos minutos, com o código de 15 minutos de validade |
| 4 | `POST /api/auth/esqueci-senha` com um e-mail que não existe no sistema | Mesma resposta do passo 2 (FR-003) — nenhuma diferença observável |
| 5 | (Opcional, se o passo 1-3 falhar) Verificar o log do backend | Mensagem de erro de envio presente e identificável (FR-004), sem mudar a resposta do passo 2/4 |

## Fora de escopo

- Qualquer mudança no texto/HTML do e-mail além do que já existe.
- Qualquer novo canal de recuperação (SMS, pergunta de segurança).
- Qualquer alteração na janela de validade do token (15 minutos, já fixada em `RecuperacaoSenhaService`).
