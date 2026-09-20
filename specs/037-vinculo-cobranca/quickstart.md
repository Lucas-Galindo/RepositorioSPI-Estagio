# Quickstart: validar Vínculo de Cobrança

Guia de validação ponta a ponta. Detalhes de contrato em [contracts/vinculos-cobranca-api.md](./contracts/vinculos-cobranca-api.md); modelo em [data-model.md](./data-model.md).

## Pré-requisitos

- Backend e MySQL configurados como no fluxo de desenvolvimento atual (`run-dev.ps1`); connection string nos .NET User Secrets.
- Script `database/14_vinculo_cobranca.sql` aplicado via `mysql` CLI (tarefa da implementação).
- Uma professora autenticada, um aluno com ao menos uma turma ativa vinculada, e outra turma ativa da qual o aluno **não** participa.

## 1. Testes automatizados

```powershell
dotnet test "C:\PROJETO - SPI\tests\SPI.Application.Tests"
```

Esperado: suíte inteira verde, incluindo `VinculosCobranca/*` (validator, serviço, não-regressão de FR-012).

Frontend:

```powershell
cd "C:\PROJETO - SPI\frontend"; npx tsc --noEmit; npm run lint
```

## 2. Banco: garantia de unicidade

Após aplicar o script, com dois `INSERT` ativos para o mesmo `(aluno_id, turma_id)` (ou ambos com `turma_id NULL`), o segundo MUST falhar com "Duplicate entry"; com o primeiro `ativo = 0`, o segundo MUST passar.

## 3. Cenários pela interface (tela de detalhe do aluno)

| # | Passo | Resultado esperado |
|---|---|---|
| 1 | Abrir aluno sem vínculos | Seção "Vínculos de Cobrança" com estado vazio, botão de adicionar (US1-6) e aviso visível de que o vínculo é só cadastro e ainda não afeta a cobrança automática (EX-001 em [plan.md](./plan.md)) |
| 2 | Adicionar: turma do aluno, Mensalidade, valor 350, aulas incluídas 8 | Aparece na lista com os dados corretos (US1-1) |
| 3 | Adicionar: "Atendimento individual", Pacote, valor 500, saldo 10 | Criado; Aulas Incluídas vazia (US1-2, US1-3) |
| 4 | Adicionar: "Atendimento individual", Avulsa, valor 80 | Rejeitado com mensagem clara (já há individual ativo) (FR-006) |
| 5 | Adicionar na mesma turma do passo 2 | Rejeitado com mensagem clara (FR-005) |
| 6 | Abrir o seletor de turma | Só aparecem turmas ativas do aluno + "Atendimento individual" (FR-013); `POST` direto com turma de fora → 409 |
| 7 | Trocar Modalidade no modal de Mensalidade para Pacote | Aulas Incluídas some, Saldo de Aulas aparece (US2-2); `PUT` com ambos preenchidos → 400 |
| 8 | Editar Valor | Lista atualiza sem recarregar (US2-1, SC-004) |
| 9 | Excluir o vínculo do passo 3 | Some da lista; nenhuma linha removida no banco (`ativo = 0`) (US2-3, SC-003) |
| 10 | Adicionar novo individual (Avulsa) agora | Permitido (US2-4, FR-007) |
| 11 | "Mostrar excluídos" | Vínculo do passo 3 aparece com botão "Reativar" (US2-6, FR-015) |
| 12 | Reativar o vínculo do passo 3 | Rejeitado: já há individual ativo; vínculo ativo permanece inalterado (US2-5, FR-014) |
| 13 | Excluir o individual Avulsa e reativar o do passo 3 | Reativa com sucesso (US2-7) |

## 4. Não-regressão da cobrança automática (FR-012)

1. Registrar a sessão de uma aula do aluno (que tem vínculos cadastrados) e conferir que a conta a receber gerada usa `Aluno.ValorAula`, sem qualquer influência do vínculo, e que o `SaldoAulas` do pacote não muda.
2. Conferir que a alteração não tocou nos pontos protegidos:

```powershell
git -C "C:\PROJETO - SPI" diff --stat -- src/SPI.Application/Aulas src/SPI.Domain/Entities/Aluno.cs
```

Esperado: `AulaService.cs` sem alterações; em `Aluno.cs` apenas a nova coleção de navegação, sem mexer em `ValorAula`.
