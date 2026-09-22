# Contratos: Cobrança Automática por Modalidade do Vínculo

**Sem contrato de API novo ou alterado nesta feature.**

- Nenhum endpoint HTTP é criado, removido ou muda de assinatura/formato de resposta.
- `POST /api/aulas/{id}/registrar-sessao` (existente, `AulasController`) continua com a mesma request (`RegistrarSessaoRequest`) e a mesma response (`AulaResponse`) — o efeito financeiro passa a variar por modalidade do vínculo, mas isso é observável apenas indiretamente, via `GET /api/pagamentos` e `GET /api/alunos/{alunoId}/vinculos-cobranca` (ambos endpoints já existentes, specs/037), nunca no corpo da resposta de `registrar-sessao` em si.
- O único "contrato" novo desta feature é interno (entre `AulaService` e `IVinculoCobrancaRepository`), documentado em [../data-model.md](../data-model.md) — não é uma interface HTTP, então não há arquivo de contrato de API aqui.

A validação end-to-end desta feature é feita via `GET /api/pagamentos?alunoId=` e `GET /api/alunos/{alunoId}/vinculos-cobranca` antes/depois de `POST /api/aulas/{id}/registrar-sessao`, conforme [../quickstart.md](../quickstart.md).
