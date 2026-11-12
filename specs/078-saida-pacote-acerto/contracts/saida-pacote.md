# Contrato: Registrar saída de turma de pacote (spec 078)

Rotas sob a turma, autenticadas (professora). **O `DELETE /api/turmas/{id}/alunos/{alunoId}` (desvincular) não muda.**

## `POST /api/turmas/{id}/alunos/{alunoId}/saida/previa` — resumo do acerto (não grava nada)

Corpo:

```json
{ "valorPago": 30.00, "vencimento": "2026-11-20" }
```

`vencimento` é opcional na prévia (só é exigido ao gravar, quando há diferença).

Resposta **200**:

```json
{
  "alunoId": 5, "alunoNome": "Ana", "turmaId": 12, "turmaNome": "Pacote Novembro",
  "valorPacote": 100.00, "totalAulas": 10, "totalEstimado": false,
  "aulasConsumidas": 7, "valorPorAula": 10.00,
  "valorDevido": 70.00, "valorPago": 30.00,
  "diferenca": 40.00, "excedente": 0.00,
  "consequencia": "GerarConta", "precisaVencimento": true,
  "aulasAgendadasQueSairao": 2
}
```

- `consequencia`: `GerarConta` (pago < devido), `Fechado` (pago = devido), `ExcedenteNaoDevolvido` (pago > devido; `excedente` = o quanto passou, **sem lançamento**).
- Todo número vem do servidor; a tela só formata e exibe.

## `POST /api/turmas/{id}/alunos/{alunoId}/saida` — registra a saída (atômico)

Corpo (os dois últimos campos são os ecos da prévia que a professora confirmou):

```json
{ "valorPago": 30.00, "vencimento": "2026-11-20", "consumidasEsperadas": 7, "valorDevidoEsperado": 70.00, "aulasQueSairaoEsperadas": 2 }
```

Resposta **200**:

```json
{ "acerto": { "...": "o mesmo da prévia" }, "pagamentoId": 431 }
```

`pagamentoId` é `null` quando nada foi gerado. Efeitos, **todos ou nenhum**: aluno desvinculado da turma; aluno retirado das aulas agendadas da turma que ainda não começaram (`aulasAgendadasQueSairao`; reposição, realizadas, canceladas e já iniciadas ficam como estão); pacote `ativo = false` com `saidaRegistradaEm` preenchida; se `diferenca > 0`, **uma** conta a receber `Pendente` (categoria "Acerto de pacote", vencimento informado, ligada ao pacote).

## Erros (ambas as rotas)

| Status | Quando |
|---|---|
| 400 | `valorPago` negativo, com mais de 2 casas ou acima do teto (lista de mensagens, como nos demais cadastros) |
| 404 | turma ou aluno inexistente |
| 409 | aluno não matriculado; turma não é de pacote ou já terminou; pacote do aluno nessa turma inativo (inclusive saída já registrada); pacote sem total de aulas; **gravação**: há diferença e o vencimento falta ou é anterior a hoje; o consumo, o valor devido ou o número de aulas de que o aluno sairia mudou desde a prévia ("confira o novo resumo"); categoria "Acerto de pacote" ausente ("aplique a migração 28") |
| 500 | erro inesperado (nada gravado) |

Todas as ações declaram `[ProducesResponseType]` 200/400/404/409/500 (o `ErrosDeNegocioFilter` só mapeia 404/409 pelo que está declarado).

## Campos novos em respostas existentes

- `GET /api/turmas` e `/api/turmas/{id}`: cada item de `alunos` ganha `podeRegistrarSaida` (bool, calculado no servidor).
- `GET /api/alunos/{id}/vinculos-cobranca`: cada vínculo ganha `saidaRegistradaEm` (datetime ou null).
- `POST /api/alunos/{id}/vinculos-cobranca/{vid}/reativar` em pacote encerrado pela saída: **409** "Este pacote foi encerrado pela saída do aluno e não pode ser reativado."
