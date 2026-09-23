# Contratos: Job de Cobrança Automática de Mensalidade

**Sem contrato de API novo ou alterado nesta feature.**

- Nenhum endpoint HTTP é criado, removido ou muda de assinatura/formato de resposta. `MensalidadeDispatcherService` é um `IHostedService` interno, sem superfície HTTP.
- O único efeito observável externamente é indireto, via endpoints **já existentes**:
  - `GET /api/pagamentos?alunoId=` (specs existentes) — passa a poder retornar contas com `categoriaReceitaNome == "Mensalidade"` e sem nenhuma `aulaIds` associada (`aulaIds: []`).
  - `GET /api/alunos/{alunoId}/vinculos-cobranca` (specs/037) — inalterado; continua sem nenhum campo novo relacionado a "mensalidade já cobrada este mês" (fora de escopo, Assumptions do spec.md).
- A validação end-to-end desta feature é feita observando `GET /api/pagamentos?alunoId=` antes/depois do disparo do job (via manipulação do relógio do teste, não do sistema em produção — ver [../quickstart.md](../quickstart.md)).
