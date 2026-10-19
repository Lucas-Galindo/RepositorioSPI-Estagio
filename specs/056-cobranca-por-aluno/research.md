# Research: Cobrança por Aluno e Reposição de Aulas

Nenhum `NEEDS CLARIFICATION` técnico — stack fixa (constituição) e regras de negócio decididas com o usuário em 2026-10-19. Abaixo, as decisões de design tiradas da leitura do código atual.

## R1 — Decisão de cobrança como função pura única

**Decision**: `RegraCobrancaPresenca.Decidir(IReadOnlyList<VinculoCobranca> ativosDoAluno, int? turmaIdDaAula)` retorna uma `DecisaoCobranca` com `Tipo` (`Mensalidade`, `Pacote`, `Avulsa`, `Bloqueada`, `SemVinculo`), o `Vinculo` aplicado e `PacoteEsgotado` (true quando caiu na avulsa porque o pacote acabou). Ordem (FR-012/FR-022): mensalidade ativa mais antiga → pacote ativo mais antigo com `SaldoAulas > 0` → pacote ativo com `SaldoAulas` nulo (ver R2) → avulsa ativa com `TurmaId == turmaIdDaAula` → se existe pacote ativo (esgotado) → `Bloqueada` → `SemVinculo`.
`AulaService.RegistrarSessaoAsync` busca os vínculos ativos do aluno uma vez (`ListarPorAlunoAsync(alunoId, ativo: true)`), decide, trata `Bloqueada` no loop de presença (como specs/041 já faz) e passa as decisões para `GerarContasAReceberAsync`, que só executa o efeito (cobrar/decrementar/nada) e grava `AulaAluno.VinculoCobrancaId`.

**Rationale**: Princípio II — a mesma ordem vale para registro de sessão hoje e para a criação retroativa (spec 055) amanhã; uma função pura é testável sem repositórios e elimina a dupla consulta atual (uma para o bloqueio, outra para a cobrança).

**Alternatives considered**: continuar com `ObterAtivoPorAlunoEContextoAsync` + fallbacks espalhados — rejeitado (lógica em dois métodos, fácil de divergir).

## R2 — Pacote com saldo nunca informado (nulo)

**Decision**: preservar o comportamento atual: pacote ativo com `SaldoAulas` nulo cobre a presença sem decrementar e sem gerar cobrança, e nunca bloqueia (specs/038 FR-010, specs/041 "saldo nulo não bloqueia").

**Rationale**: a spec 056 não muda essa regra; mudar seria efeito colateral não pedido. Fica atrás do pacote com saldo positivo na ordem (um pacote "controlado" com saldo é usado primeiro).

## R3 — Unicidade de vínculos

**Decision**: a chave gerada antiga `chave_ativa` (aluno:turma para todas as modalidades) é removida. Nova chave gerada `chave_avulsa_ativa` (aluno:turma, só Avulsa ativa) com índice único. Pacote e Mensalidade **não** ganham índice único no banco: a regra "um pacote ativo / mensalidade exclusiva" é aplicada só no `VinculoCobrancaService`.

**Rationale**: FR-021 proíbe alterar automaticamente conflitos herdados; um índice único para pacote falharia ao ser criado se já existirem dois pacotes ativos do mesmo aluno. Sistema single-tenant com uma única usuária: o risco de corrida entre duas gravações simultâneas é desprezível. Uma `CHECK (turma_id IS NULL OR modalidade = 'Avulsa')` garante no banco que Mensalidade/Pacote nunca tenham turma.

**Alternatives considered**: índice único condicional para pacote — rejeitado pelo motivo acima.

## R4 — Exclusividade no cadastro/edição/reativação

**Decision**: `VinculoCobrancaService` troca `GarantirSemVinculoAtivoAsync` (contexto exato) por `GarantirRegrasDeCombinacaoAsync(alunoId, modalidade, turmaId, ignorarId)`, que lê os vínculos ativos do aluno (`ListarPorAlunoAsync(alunoId, true)`, ignorando o próprio vínculo) e recusa com `ConflitoException`: Mensalidade com qualquer outro ativo; Pacote/Avulsa com mensalidade ativa; Pacote com outro pacote ativo; Avulsa com outra avulsa ativa no mesmo contexto. Na edição, roda sempre (não só quando a turma muda), porque a modalidade também pode mudar. `ExisteAtivoAsync` e `ObterAtivoPorAlunoEContextoAsync` deixam de ter uso e são removidos da interface/repositório.

**Rationale**: mensagens específicas por caso (FR-002/FR-003/FR-008); remover os métodos órfãos evita código morto.

## R5 — Turma só para Avulsa

**Decision**: `VinculoCobrancaRequestValidator` rejeita `TurmaId` preenchido quando a modalidade é Mensalidade ou Pacote ("Mensalidade e Pacote valem para todas as aulas do aluno e não têm turma."). `ValidarTurmaAsync` (turma ativa com o aluno matriculado) continua valendo para Avulsa com turma. O formulário do frontend esconde o campo turma para essas modalidades e envia `turmaId: null`.

## R6 — Conflito herdado exibido na ficha do aluno

**Decision**: `VinculoCobrancaResponse` ganha `Conflito: string?`. `ListarPorAlunoAsync` calcula, sobre os vínculos **ativos** do aluno: se há mensalidade e mais de um vínculo ativo, todos os ativos recebem "Mensalidade não pode coexistir com outros vínculos ativos."; senão, se há mais de um pacote ativo, os pacotes recebem "Mais de um pacote ativo.". Os demais endpoints devolvem `Conflito` nulo. O frontend mostra um aviso no topo da seção de vínculos quando algum item tem `conflito`.

**Rationale**: cálculo no backend (Princípio II); sem tela nova (Assumption da spec).

## R7 — Participantes de reposição

**Decision**: coluna `aula_aluno.reposicao` (bool, default false). Novos métodos em `AulaService`: `AdicionarParticipanteAsync(aulaId, alunoId)` (aula ativa, agendada, de turma; aluno existente e ativo; ainda não participante) e `RemoverParticipanteAsync(aulaId, alunoId)` (aula agendada; participante com `Reposicao == true`). Endpoints `POST /api/aulas/{id}/participantes` e `DELETE /api/aulas/{id}/participantes/{alunoId}`. `AtualizarAsync` deixa de apagar todos os participantes: remove só os não-reposição, recria os matriculados ativos da turma (nova ou mesma) e, se um aluno de reposição agora é matriculado, marca `Reposicao = false` (sem duplicar). Se a aula vira individual, os participantes de reposição são removidos (aula individual não aceita extras — FR-017). Adicionar/remover participante dispara o mesmo recálculo de lembretes que a edição de aula? **Não** — lembretes dependem da data da próxima aula da turma, que não muda.

**Rationale**: menor mudança que atende FR-015 a FR-019; reaproveita a navegação `Aula.AulaAlunos` já usada na edição.

## R8 — Migração de dados e rótulos

**Decision**: `database/21_cobranca_por_aluno.sql`: (1) remove `uq_vinculocobranca_chave_ativa` e a coluna `chave_ativa`; (2) `UPDATE ... SET turma_id = NULL WHERE modalidade IN ('Mensalidade','Pacote')`; (3) cria `chave_avulsa_ativa` + índice único + `CHECK`; (4) `ALTER TABLE aula_aluno ADD reposicao`. A ordem (1 antes de 2) evita colisão da chave antiga quando um aluno tinha Avulsa individual e Pacote numa turma. Antes de aplicar no banco local, roda-se `database/backup/backup-database.ps1`. Rótulos: `MensalidadeDispatcherService` passa a descrever "Mensalidade - MM/aaaa"; `DashboardService` (Pacotes em atenção) passa a mostrar "Todas as aulas do aluno"; a ficha do aluno mostra "Todas as aulas do aluno" para Mensalidade/Pacote e mantém turma/"Atendimento individual" para Avulsa.
