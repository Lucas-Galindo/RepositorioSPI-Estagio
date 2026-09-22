# Quickstart: validar Cobrança Automática por Modalidade do Vínculo

Guia de validação ponta a ponta. Modelo em [data-model.md](./data-model.md); ausência de contrato de API em [contracts/README.md](./contracts/README.md).

## Pré-requisitos

- Backend rodando (mesmo fluxo de `run-dev.ps1`), banco já com `vinculo_cobranca` (specs/037).
- Uma professora autenticada.
- Um aluno com `ValorAula` definido, participante de uma turma ativa, e com ao menos uma aula `Agendada` (de turma e/ou individual) disponível para registrar sessão — ou criar novas aulas via `POST /api/aulas`.
- Quatro Vínculos de Cobrança de teste, um por cenário (podem ser os mesmos endpoints de specs/037): sem vínculo (não criar nenhum), Avulsa com Valor diferente de `ValorAula`, Mensalidade, e Pacote com `SaldoAulas` pequeno (ex.: 2).

## 1. Testes automatizados

```powershell
dotnet test "C:\PROJETO - SPI\tests\SPI.Application.Tests"
```

Esperado: suíte inteira verde, incluindo `Aulas/AulaServiceGerarContasAReceberTests.cs` (os 4 ramos de modalidade + os 3 testes de requisito negativo do research.md R6).

## 2. Cenários pela API (requisições diretas)

| # | Passo | Resultado esperado |
|---|---|---|
| 1 | Registrar sessão (`POST /api/aulas/{id}/registrar-sessao`, presença = sim) de um aluno **sem** Vínculo de Cobrança para o contexto da aula | `GET /api/pagamentos?alunoId=` mostra uma nova conta com `valorFinal` igual a `Aluno.ValorAula` (US1) |
| 2 | Cadastrar Vínculo Avulsa com Valor diferente do `ValorAula`, no mesmo contexto de uma aula; registrar a sessão | Nova conta com `valorFinal` igual ao **Valor do vínculo**, não ao `ValorAula` (US2) |
| 3 | Cadastrar Vínculo Mensalidade no contexto de uma aula; registrar a sessão | Nenhuma conta nova em `GET /api/pagamentos?alunoId=` para essa aula (US4) |
| 4 | Cadastrar Vínculo Pacote com `SaldoAulas = 2` no contexto de uma aula; registrar a sessão | Nenhuma conta nova; `GET /api/alunos/{alunoId}/vinculos-cobranca` mostra `saldoAulas = 1` (US3) |
| 5 | Registrar outra sessão no mesmo contexto do passo 4 | Nenhuma conta nova; `saldoAulas = 0` |
| 6 | Registrar uma terceira sessão no mesmo contexto (saldo já em 0) | Nenhuma conta nova; `saldoAulas` permanece `0` (nunca negativo) |
| 7 | Numa aula de turma com 2+ alunos, cada um com uma modalidade diferente (um sem vínculo, um Avulsa); registrar a sessão marcando ambos presentes | Cada aluno recebe o efeito da sua própria modalidade na mesma chamada (FR-007) |
| 8 | Registrar uma sessão com um aluno marcado como ausente (`presente: false`), mesmo que tenha um Vínculo Pacote | Nenhuma conta gerada e `saldoAulas` inalterado — falta nunca decrementa (FR-008) |

## 3. Não-regressão explícita

- Repetir o cenário 1 do quickstart de specs/037 (§4: registrar sessão de aluno com vínculo cadastrado, mas antes desta feature existir) não se aplica mais como "sem interferência" — a interferência agora É o comportamento esperado. Em vez disso, confirmar que:
  - `VinculoCobrancaRequestValidatorTests`, `VinculoCobrancaService*Tests` (specs/037) continuam passando sem alteração — o cadastro do vínculo em si não muda (FR-011).
  - `GET /api/alunos/{alunoId}` continua retornando `valorAula` normalmente, sem nenhum campo novo.

```powershell
git -C "C:\PROJETO - SPI" diff --stat -- src/SPI.Application/VinculosCobranca src/SPI.Api/Controllers/VinculosCobrancaController.cs frontend/components/alunos
```

Esperado: sem alterações (vazio) — nada do cadastro do vínculo (specs/037) nem da UI é tocado por esta feature.
