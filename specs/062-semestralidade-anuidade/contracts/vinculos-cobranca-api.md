# Contrato: vínculos de cobrança com Semestral e Anual (spec 062)

Base: `/api/alunos/{alunoId}/vinculos-cobranca`. Todos exigem o perfil Professor. A modalidade viaja como texto: `"Avulsa" | "Mensalidade" | "Pacote" | "Semestral" | "Anual"`.

## `POST` — cadastrar (alterado)

Para `Semestral` e `Anual`:

```json
{
  "modalidade": "Semestral",
  "turmaId": null,
  "valor": 1800.00,
  "dataInicio": "2026-11-01",
  "parcelas": [
    { "vencimento": "2026-11-05", "valor": 300.00 },
    { "vencimento": "2026-12-05", "valor": 300.00 }
  ]
}
```

- `valor` é o valor total do período (referência). **Pode diferir da soma das parcelas.**
- `dataInicio` e `parcelas` são **obrigatórios** nessas modalidades e **proibidos** nas outras (400).
- **201**: o vínculo, com `dataInicio`, `dataFim` (= início + 6 ou 12 meses − 1 dia) e `parcelas[]` (cada uma com `pagamentoId`, `descricao`, `vencimento`, `valor`, `status`).
- **400**:
  - nenhuma parcela, ou mais de 24;
  - valor da parcela ≤ 0;
  - duas parcelas no mesmo mês;
  - turma informada;
  - início ausente.
- **409**:
  - o aluno tem outro vínculo ativo (Mensalidade, Pacote ou Avulsa);
  - o período se sobrepõe ao de outro Semestral/Anual ativo.
- **Atômico**: em qualquer falha, nem o vínculo nem as parcelas são gravados.

Cadastrar `Mensalidade`, `Pacote` ou `Avulsa` com um Semestral/Anual ativo no aluno responde **409** ("O aluno tem um plano ... que já cobre todas as aulas.").

## `GET` — listar e obter (alterado)

Cada item de Semestral/Anual traz `dataInicio`, `dataFim` e `parcelas[]`, ordenadas por vencimento. O `status` da parcela segue o de Contas a Receber: `Pendente`, `Pago`, `Cancelado` ou `Atrasado` (pendente e vencida). Nos outros vínculos, `dataInicio`, `dataFim` e `parcelas` vêm nulos.

## `PUT {id}` — editar (alterado)

- **409** quando o vínculo existente ou a modalidade pedida é Semestral/Anual: "Semestralidade e anuidade não podem ser editadas: exclua e cadastre outro vínculo."

## `DELETE {id}` — excluir (alterado)

- Para Semestral/Anual, na mesma transação: `Ativo = false` e as parcelas do vínculo com `Status = Pendente` e `vencimento > hoje` passam a `Cancelado`. As pagas, as já vencidas e as demais ficam intactas. Nenhum pagamento é apagado.
- **204**, também quando o vínculo já estava excluído (idempotente).

## `PATCH {id}/reativar` — reativar (alterado)

- **409** para Semestral/Anual: "Este plano foi encerrado e suas parcelas canceladas. Cadastre um novo vínculo."

## Efeito em `POST /api/aulas/{id}/registrar-sessao` (contrato igual)

Para o aluno com plano vigente na data da aula, a presença não gera conta a receber e fica ligada ao plano. Fora do período, a decisão segue pacote, avulsa ou valor da aula.

## `GET /api/notificacoes` (alterado)

Novo `tipo`: `"RenovacaoPlano"`, com `renovacao` preenchido e `aula`/`pagamento` nulos:

```json
{ "alunoId": 3, "alunoNome": "Ana", "modalidade": "Semestral", "dataFim": "2027-04-30" }
```
