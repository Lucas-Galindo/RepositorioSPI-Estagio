# Quickstart: validar o link da aula (spec 067)

Contratos em [contracts/aulas-link-api.md](contracts/aulas-link-api.md); modelo em [data-model.md](data-model.md).

## Pré-requisitos

- API local (porta 5099) com `Smtp__SmtpHost=smtp.invalido.localhost` (nenhum e-mail sai de verdade) e frontend na porta 3000.
- Login de teste pela variável de ambiente `SPI_TESTE_SENHA` (nunca em arquivo ou Trello).
- Dados sintéticos com prefixo "TESTE-067" (aulas, turma, aluno), apagados no fim, com backup do banco antes (`database/backup/backup-database.ps1`). Dados do usuário não são tocados.
- **Nenhuma migração** a aplicar.

## 1. Testes automatizados (relógio fixo)

`dotnet test`: `LinkAulaTemporarioTests`, `RegraLinkAulaTests`, `DefinirLinkAulaRequestValidatorTests`, `AulaServiceLinkTests`, `LembreteDispatcherLinkTests` e toda a suíte anterior sem alterar nenhum `Assert`.

## 2. API autenticada

| # | Passo | Esperado |
|---|---|---|
| 1 | `PUT /api/aulas/{futura}/link` com `https://meet.google.com/x` | 200; `linkAula` preenchido; `aceitaLinkAula = true` |
| 2 | `GET /api/aulas/{id}` e `GET /api/aulas` | os dois trazem o `linkAula` |
| 3 | `PUT` com `ftp://x`, `javascript:alert(1)`, `abc`, com espaço, 501 caracteres | 400 cada; nada guardado |
| 4 | `PUT` em aula cancelada / já terminada | 409 |
| 5 | `PUT` em aula excluída / inexistente | 404 |
| 6 | `DELETE` do link, duas vezes | 200 nas duas; `linkAula = null` |
| 7 | Aula que termina em ~2 min: informar link, esperar passar o fim | depois do fim `linkAula = null` e `aceitaLinkAula = false` |
| 8 | Informar link e cancelar a aula (outra) | `linkAula = null` |
| 9 | Informar link e mover a aula para outro horário | link continua; ao mover para o passado, some |
| 10 | Reiniciar a API | `linkAula = null` nas aulas que tinham link |
| 11 | Banco: `SELECT COUNT(*)` de `aula` e `DESCRIBE aula` antes e depois | iguais; nenhuma coluna de link |
| 12 | Logs da API (`logs/`) | nenhuma ocorrência do link |

## 3. E-mail (servidor desviado)

| # | Passo | Esperado |
|---|---|---|
| 1 | Turma com lembrete vencido e próxima aula **com** link; disparar o envio | corpo do e-mail com "Link da aula" clicável |
| 2 | Mesmo cenário **sem** link | corpo idêntico ao atual |
| 3 | Link com `&` e `<` | caracteres protegidos, link correto |
| 4 | Link expirado/perdido no momento do envio | e-mail sai sem link, sem erro; status do lembrete como sempre |

(Sem enviar de verdade: os testes de `LembreteDispatcherLinkTests` capturam o corpo; na validação real, o envio vai para um servidor inexistente e o corpo é conferido pelo teste.)

## 4. Telas (Chrome)

| # | Passo | Esperado |
|---|---|---|
| 1 | Detalhes de aula futura, colar link e salvar | link clicável (abre em nova aba) + aviso de que é temporário |
| 2 | Trocar e remover | atualiza; campo volta |
| 3 | Link inválido | mensagem de erro do servidor |
| 4 | Detalhes de aula já terminada/cancelada | sem campo de link |
| 5 | Popup de detalhes na Home/Agenda de uma aula com link | link clicável, sem editar |
| 6 | Capturas das demais telas, antes e depois | só mudam os detalhes e o popup da aula |
| 7 | Modo claro e escuro, `npm run verificar-contraste` | legível; 0 pares abaixo de 4.5:1 |
