# Quickstart: progresso de pacote (spec 058)

Contrato em [contracts/api.md](./contracts/api.md).

## Pré-requisitos

API local com o SMTP desligado; token de Professor; dados sintéticos "TESTE-058", sem e-mail; backup do banco antes.

## Cenários (API)

| # | Passo | Esperado |
|---|---|---|
| 1 | Cadastrar Pacote com `totalAulasPacote = 4` e sem saldo | `saldoAulas = 4`; `progresso` = 0 dadas, 4 restantes, `EmDia` |
| 2 | Em aulas futuras da turma, registrar presença em duas, falta em uma, e cancelar uma | `progresso` = 2 dadas, 2 restantes, `Atencao` |
| 3 | `GET .../{id}/aulas` | Exatamente 2 aulas, as das presenças |
| 4 | Presença numa aula individual | 3 dadas, 1 restante, `Atencao` |
| 5 | `PUT` com `totalAulasPacote = 2` | 409, "aulas ja dadas (3)" |
| 6 | `GET /api/dashboard?alunoId={aluno}` | `pacotes.proximosDoFim = 1`; `renovacao` com o pacote e os mesmos números da ficha |
| 7 | Mais uma presença | 4 dadas, 0 restantes, `Esgotado`; no dashboard, `concluidos = 1` e o pacote em `renovacao` |
| 8 | Período sem as aulas (`periodoInicio`/`periodoFim` de outro mês) | `aulasDadasNoPeriodo = 0`; os demais números não mudam |

## Frontend (manual)

Formulário com "Total de aulas"; ficha com a barra "N de M" e o estado; "Ver aulas" abre a lista; Home com indicadores, renovação e filtros.
