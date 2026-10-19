# Research: Notificações de Status de Aula Pendente (spec 059)

## R1 — Uma notificação por aula

**Decision**: a tabela `notificacao` tem `UNIQUE (tipo, aula_id)`. A sincronização:

1. Lê as aulas pendentes.
2. Lê as notificações já existentes dessas aulas, em qualquer estado, mais as ativas do tipo.
3. Aplica a regra.
   - Se duas sincronizações rodarem ao mesmo tempo, uma pode tentar inserir uma notificação que a outra já criou. O `SalvarAlteracoesAsync` do repositório então lança `ConflitoException`, e o serviço ignora: a notificação já existe.

**Rationale**: FR-003 com garantia no banco, não só no código; o mesmo padrão de chave única já é usado em `vinculo_cobranca`.

## R2 — O que é "pendente"

**Decision**: `IAulaRepository.ListarPendentesDeStatusAsync(DateTime agora, DateTime desde, int? professorId)` devolve as aulas que cumprem tudo isto:
- `Ativo`;
- `Status == "Agendada"`;
- término (`DataInicio` + `HoraFim`) `<= agora`;
- término `>= desde` (FR-004).

A query traz Turma e AulaAlunos.Aluno, para montar a mensagem. A comparação data + hora é feita em duas partes: data anterior, ou mesma data com hora menor ou igual, porque o EF não traduz `DateOnly.ToDateTime` em todas as versões.

## R3 — Regra da sincronização

**Decision**: `NotificacaoService.SincronizarAulasPendentesAsync(agora, professorId?)`, com P = aulas pendentes:

| Situação | Ação |
|---|---|
| Aula em P sem notificação | cria uma (`criada_em = agora`, não lida, ativa) |
| Aula em P com notificação resolvida | reativa: `resolvida_em = null`, `lida_em = null`, `criada_em = agora`, para subir ao topo como nova |
| Notificação ativa cuja aula não está em P | resolve: `resolvida_em = agora` |

**Rationale**: cobre FR-011 e FR-012 sem gancho no `AulaService`. Registrar, cancelar, excluir ou editar a aula fazem a notificação sair na próxima sincronização, e o `GET` sincroniza antes de listar, então a professora nunca vê um aviso já resolvido. Um gancho no `AulaService` duplicaria a regra.

**Alternatives considered**: resolver dentro de `RegistrarSessaoAsync`/`CancelarAsync` — duplica a definição de "pendente" em vários lugares.

## R4 — Configuração

**Decision**: a seção `Notificacoes` do appsettings tem duas chaves:
- `AulasPendentesDesde`, padrão `2026-10-19T00:00:00`;
- `IntervaloVerificacaoSegundos`, padrão `300`.

Elas viram `NotificacoesOptions`, uma classe da Application registrada como singleton em `AddInfrastructure` (que já recebe `IConfiguration`).

## R5 — API

**Decision**: contrato em [contracts/notificacoes-api.md](./contracts/notificacoes-api.md).
- `GET /api/notificacoes`: sincroniza a professora do token e devolve as ativas, das mais recentes para as mais antigas.
- `PATCH /api/notificacoes/{id}/lida`: marca como lida. Se a notificação for de outra professora, responde 404.
- A mensagem e o "contexto" (turma, ou "Atendimento individual · aluno") são montados no backend.

## R6 — Frontend

**Decision**: `NotificacoesSino`, no `Header`, substitui o sino decorativo.
- **Quando busca**: `GET` ao montar, a cada troca de rota (`usePathname`) e a cada 60 s.
- **Painel**: suspenso, com a lista. Itens não lidos ficam em negrito, com um ponto.
- **Clique num item**: abre um popup (estilo `modal` existente) e chama `PATCH lida`.
- **"Ir para a aula"**: `router.push("/aulas/{id}")`.
- **Estilos**: só tokens da spec 057.
