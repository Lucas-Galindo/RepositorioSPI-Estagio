# Contrato: convenção da linha `STATUS:`

Formato exato da primeira linha de toda descrição de card escrita por esta integração. Aplica-se a cards de feature (que também têm o marcador `Feature: <id>` da spec 040) e a cards manuais (US4).

## Formato

```text
STATUS: <detalhe>
```

- Sempre a **primeira linha** da descrição — nunca no meio ou no fim.
- `<detalhe>` é uma única linha de texto livre, em português, sem quebras de linha.
- Nunca há mais de uma linha `STATUS:` na descrição, mesmo após várias reescritas (FR-006).
- Uma linha `STATUS:` que apareça no **meio** do texto (não na primeira posição) não conta como o padrão — é só texto (edge case do spec.md).

## Regra de reconstrução (idempotente)

Toda escrita segue o mesmo algoritmo, não importa a fase:

1. `Corpo = Extrair-CorpoSemStatus(DescricaoAtual)` — remove uma eventual linha `STATUS:` inicial (e a linha em branco logo depois, se houver); se não havia `STATUS:`, `Corpo` = a descrição inteira, inalterada.
2. `NovaDescricao = Montar-DescricaoComStatus(Detalhe, Corpo)` — `"STATUS: {Detalhe}"` sozinho se `Corpo` for vazio, senão `"STATUS: {Detalhe}\n\n{Corpo}"`.

Nada além da primeira linha muda (FR-005) — inclusive o marcador `Feature: <id>` de um card de feature, que é só a última linha de `Corpo` e nunca é tocado por esta lógica.

## Origem do `<detalhe>`

Ver [sync-card-cli-delta.md](./sync-card-cli-delta.md) (`-Detalhe`) e [../research.md](../research.md) R3/R4.

## Exemplos

| Descrição de entrada | Fase | Detalhe | Descrição de saída |
|---|---|---|---|
| `"Resumo do spec...\n\nFeature: 037-vinculo-cobranca"` | `design` | (derivado) `"/speckit-plan concluído"` | `"STATUS: /speckit-plan concluído\n\nResumo do spec...\n\nFeature: 037-vinculo-cobranca"` |
| `"STATUS: /speckit-tasks concluído, 12 tarefas geradas\n\nResumo...\n\nFeature: 038-x"` | `em-andamento` | (derivado) `"/speckit-implement rodando, 3/12 tarefas concluídas"` | `"STATUS: /speckit-implement rodando, 3/12 tarefas concluídas\n\nResumo...\n\nFeature: 038-x"` |
| `"Preciso rever os botões de pagamento"` (card manual, sem marcador) | `retroativo` | (fixo) `"card criado manualmente, sem spec formal ainda"` | `"STATUS: card criado manualmente, sem spec formal ainda\n\nPreciso rever os botões de pagamento"` |
