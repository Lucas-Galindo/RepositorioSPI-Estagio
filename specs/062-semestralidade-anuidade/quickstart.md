# Quickstart: validar Semestralidade e Anuidade (spec 062)

Contrato em [contracts/vinculos-cobranca-api.md](./contracts/vinculos-cobranca-api.md); regras em [research.md](./research.md).

## Pré-requisitos

- Backup do banco local e `database/24_semestralidade_anuidade.sql` aplicado.
- API local com o servidor de e-mail apontado para um endereço inexistente (nenhum e-mail pode sair) e token de Professor.
- Dados sintéticos "TESTE-062", sem e-mail real.

## Cenários (API)

| # | Passo | Esperado |
|---|---|---|
| 1 | Semestral, início 01/11/2026, valor 1800, 6 parcelas | 201; `dataFim` = 30/04/2027; 6 contas a receber "Pendente", ligadas ao vínculo, categoria "Semestralidade", descrição "Semestralidade - parcela X/6" |
| 2 | Anual à vista (1 parcela) | 201; `dataFim` = início + 12 meses − 1 dia; 1 conta |
| 3 | Duas parcelas no mesmo mês | 400, nada gravado (nem vínculo) |
| 4 | Soma das parcelas diferente do valor total | 201 (sem bloqueio) |
| 5 | Aluno com Mensalidade/Pacote/Avulsa ativa → cadastrar Semestral | 409 |
| 6 | Aluno com Semestral ativo → cadastrar Mensalidade, Pacote ou Avulsa | 409 |
| 7 | Novo Semestral com período sobreposto | 409; com início depois do fim do atual: 201 |
| 8 | `PUT` no Semestral | 409 |
| 9 | Presença em aula dentro do período (turma e individual) | nenhuma conta nova; presença ligada ao plano |
| 10 | Presença em aula depois do fim do período | cobra o valor da aula |
| 11 | Excluir o plano com 1 parcela paga, 1 vencida pendente e 4 futuras pendentes | 204; só as 4 futuras ficam "Cancelado"; a paga e a vencida intactas; nenhum pagamento apagado |
| 12 | `PATCH reativar` no plano excluído | 409 |
| 13 | Plano que termina em 10 dias | `GET /api/notificacoes` traz 1 `RenovacaoPlano`; repetir o `GET` mantém 1 só |
| 14 | Cadastrar a renovação (início depois do fim) | o aviso some |
| 15 | Lembrete de vencimento 5 dias antes | inclui a parcela, com texto "pagamento"; e-mail só ao endereço de teste |
| 16 | Plano com uma parcela vencida e pendente | a parcela aparece em Contas a Receber e no `GET /api/notificacoes` como `PagamentoPendente` do aluno (FR-018) |
| 17 | Apagar os dados de teste e conferir os vínculos reais | totais iguais aos de antes |

## Frontend (manual, navegador)

- Ficha do aluno → "Adicionar vínculo" → Semestral: início, nº de parcelas, valor total e 1º vencimento geram parcelas editáveis; aviso quando a soma difere do total.
- A listagem mostra o período e as parcelas com seus status; o plano não tem botão "Editar".
- Excluir mostra a confirmação sobre o cancelamento das parcelas futuras.
- O sino mostra o aviso de renovação (filtro "Planos"), com atalho para a ficha do aluno.
- Claro/escuro e 360px, sem rolagem horizontal.
