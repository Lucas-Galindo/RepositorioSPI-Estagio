# Research: Auditoria de Aulas Dadas por Vínculo de Cobrança

Decisões técnicas da Phase 0. O grounding de negócio já veio fechado da conversa que precedeu a spec (ver spec.md, "Nota de investigação prévia"); esta fase resolve os detalhes de implementação que a spec deixou para `/speckit-plan`.

## R1. Schema: 1 arquivo SQL novo, 2 colunas aditivas, sem alterar as existentes

- **Decision**: `database/18_auditoria_vinculo_cobranca.sql` (próximo número livre após `17_exclusao_professor_token.sql`). Duas alterações:
  1. `ALTER TABLE vinculo_cobranca ADD COLUMN total_aulas_pacote INT NULL COMMENT 'Total original de aulas do pacote, imutavel por consumo (specs/052); so se aplica a modalidade Pacote' , ADD CONSTRAINT chk_vinculocobranca_total_aulas_pacote CHECK (total_aulas_pacote IS NULL OR (modalidade = 'Pacote' AND total_aulas_pacote >= 1));` — mesmo padrão de `chk_vinculocobranca_aulas_incluidas`/`chk_vinculocobranca_saldo_aulas` já existentes em `database/14_vinculo_cobranca.sql`.
  2. `ALTER TABLE aula_aluno ADD COLUMN vinculo_cobranca_id INT NULL COMMENT 'Vinculo de cobranca ativo no momento desta presenca, para auditoria historica (specs/052); NULL quando nao havia vinculo correspondente' , ADD CONSTRAINT fk_aulaaluno_vinculo_cobranca FOREIGN KEY (vinculo_cobranca_id) REFERENCES vinculo_cobranca(id);` — sem índice único (ao contrário de `pagamento.vinculo_cobranca_id` em specs/039, aqui múltiplas presenças apontam legitimamente para o mesmo vínculo).
- **Rationale**: segue exatamente o padrão já estabelecido pelo projeto (`database/NN_*.sql` numerado, CHECK por modalidade, FK nomeada) — specs/037 e specs/039 são o precedente direto para cada uma das duas mudanças.
- **Alternatives considered**: criar uma tabela de histórico separada (`vinculo_cobranca_consumo`) em vez de uma coluna em `aula_aluno` — descartado: `AulaAluno` já é o registro de presença por aluno/aula; uma tabela paralela duplicaria a granularidade sem necessidade, já que a relação é 1 presença → no máximo 1 vínculo afetado.

## R2. Backfill dos Pacotes já existentes: `total_aulas_pacote = saldo_aulas` atual

- **Decision**: no mesmo arquivo SQL, após o `ALTER TABLE`: `UPDATE vinculo_cobranca SET total_aulas_pacote = saldo_aulas WHERE modalidade = 'Pacote' AND saldo_aulas IS NOT NULL;`
- **Rationale**: spec.md (Assumptions) já decidiu esse default — assume consumo zero prévio para vínculos existentes, sabendo que pode subestimar o total real se já havia consumo; a professora corrige manualmente depois (editar já fica sujeito à regra de bloqueio de FR-007 a partir da correção).
- **Alternatives considered**: deixar `total_aulas_pacote` NULL para vínculos antigos — descartado: "aulas dadas" ficaria indefinido para todo o histórico existente até a professora editar manualmente cada vínculo, uma pior experiência que um valor de partida (mesmo que subestimado) já exibido.

## R3. Onde popular `AulaAluno.VinculoCobrancaId`: reaproveitar a resolução já existente em `GerarContasAReceberAsync`

- **Decision**: em `AulaService.GerarContasAReceberAsync` (`AulaService.cs:219-274`), dentro do `foreach (var aulaAluno in aula.AulaAlunos.Where(v => v.Presente == true))`, logo após `var vinculoCobranca = await _vinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(...)` (linha ~230) e antes do `switch`, adicionar `aulaAluno.VinculoCobrancaId = vinculoCobranca?.Id;` — incondicional (vale para Avulsa/Mensalidade/Pacote, e fica `null` quando `vinculoCobranca` é `null`, cobrindo FR-004/FR-005 automaticamente pela própria expressão).
- **Rationale**: o vínculo correto para o contexto já é resolvido exatamente ali, uma vez por presença — reaproveitar essa consulta evita uma segunda chamada ao repositório e garante que a mesma lógica de correspondência (specs/038) decide os dois efeitos (financeiro e auditoria) de forma consistente, sem risco de divergência entre eles.
- **Persistência**: `aulaAluno` é uma entidade seguida pelo mesmo `DbContext` já usado por `_aulaRepository`/`_vinculoCobrancaRepository`/`_pagamentoRepository` (injeção scoped padrão do projeto) — a chamada já existente a `_pagamentoRepository.SalvarAlteracoesAsync(cancellationToken)` ao final do método persiste a mudança em `aulaAluno` junto com o resto, sem precisar de um `SaveChanges` adicional (mesmo princípio já em uso: `aulaAluno.MotivoNaoRegistro`, setado em `RegistrarSessaoAsync`, é persistido pela chamada a `_aulaRepository.SalvarAlteracoesAsync` que vem depois).
- **Alternatives considered**: popular em `RegistrarSessaoAsync` (onde o "pacote esgotado" já faz uma consulta similar, linha ~188) — descartado: aquela consulta decide só se a presença é barrada por saldo zero, acontece ANTES da aula virar `Realizada`, e para alunos sem Pacote nem é chamada; reaproveitar a de `GerarContasAReceberAsync` é a única que roda para toda presença confirmada, de qualquer modalidade.

## R4. Contagem mensal da Mensalidade: novo método em `IAulaRepository`, não em `IVinculoCobrancaRepository`

- **Decision**: `IAulaRepository` ganha `Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default)`, implementado como `_dbContext.AulaAlunos.Where(aa => aa.VinculoCobrancaId == vinculoCobrancaId && aa.Aula.DataInicio.Year == competencia.Year && aa.Aula.DataInicio.Month == competencia.Month).CountAsync(...)`.
- **Rationale**: `AulaAluno` é parte do agregado `Aula` (sem repositório próprio, mesmo padrão já usado em todo o projeto) — a consulta é sobre presenças, não sobre o cadastro do vínculo; mesmo raciocínio de fronteira de agregado já usado para `IPagamentoRepository.ExisteMensalidadeGeradaAsync` viver no repositório de `Pagamento` (specs/039), não no de `VinculoCobranca`.
- **Alternatives considered**: adicionar em `IVinculoCobrancaRepository` — descartado: esse repositório nunca consultou `AulaAluno`/`Aula` até hoje, misturaria fronteiras de agregado sem necessidade.

## R5. Cálculo de "aulas dadas" do Pacote: sem consulta nova, direto na entidade já carregada

- **Decision**: "aulas dadas" de um vínculo Pacote é `(vinculo.TotalAulasPacote ?? 0) - (vinculo.SaldoAulas ?? 0)`, calculável a partir dos 2 campos já presentes na entidade carregada — nenhum método de repositório novo necessário para este caso (ao contrário da Mensalidade, que depende de uma contagem de presenças).
- **Rationale**: `TotalAulasPacote` e `SaldoAulas` já vivem na mesma linha de `vinculo_cobranca`; não há necessidade de tocar `AulaAluno` para responder "quanto do Pacote já foi consumido" — só para listar/auditar quais presenças específicas foram (US2), que é uma necessidade diferente, não coberta por esta spec (fica para a feature de visualização).

## R6. Bloqueio de edição (FR-007/FR-010): dentro de `VinculoCobrancaService.AtualizarAsync`, antes de aplicar a mudança

- **Decision**: em `VinculoCobrancaService.AtualizarAsync`, depois de carregar o `vinculo` existente e antes de atribuir os novos valores do `request`:
  - Se `vinculo.Modalidade == Pacote` e `request.TotalAulasPacote.HasValue`: calcular `dado = (vinculo.TotalAulasPacote ?? 0) - (vinculo.SaldoAulas ?? 0)`; se `request.TotalAulasPacote.Value < dado`, lançar `ConflitoException` (mesmo tipo já usado pelas demais validações stateful deste serviço) com mensagem `"O novo total de aulas do pacote nao pode ser menor que as aulas ja dadas ({dado})."`.
  - Se `vinculo.Modalidade == Mensalidade` e `request.AulasIncluidas.HasValue`: obter `dadoNoMes` via `IAulaRepository.ContarPresencasPorVinculoECompetenciaAsync(vinculo.Id, competenciaAtual, ct)` (competência atual = `DateOnly` do primeiro dia do mês corrente, mesmo padrão de `MensalidadeDispatcherService`); se `request.AulasIncluidas.Value < dadoNoMes`, lançar `ConflitoException` com mensagem equivalente, citando "neste mês".
- **Rationale**: é o único ponto por onde toda edição de vínculo já passa (Princípio II da constituição) — `CadastrarAsync` não precisa dessa checagem porque um vínculo recém-criado nunca tem presenças associadas ainda (impossível `dado > 0` na criação).
- **Alternatives considered**: mover a checagem para o `FluentValidation` (`VinculoCobrancaRequestValidator`) — descartado: o validator é stateless por padrão em todo o projeto (só valida a forma do request), enquanto esta checagem depende do estado atual do vínculo no banco e, para Mensalidade, de uma consulta — mesma divisão de responsabilidade já usada para `GarantirSemVinculoAtivoAsync` (unicidade), que também vive no `Service`, não no `Validator`.

## R7. Contrato de API: 2 campos aditivos no request/response já existentes, sem endpoint novo

- **Decision**: `VinculoCobrancaRequest` ganha `TotalAulasPacote` (`int?`, mesma posição/estilo de `AulasIncluidas`/`SaldoAulas`); `VinculoCobrancaResponse` ganha `TotalAulasPacote` e dois campos calculados e somente-leitura — `AulasDadas` (`int?`, só Pacote, `TotalAulasPacote - SaldoAulas` quando ambos informados) e `AulasDadasNoMes` (`int?`, só Mensalidade, contagem da competência atual) — ambos calculados no `Mapear` do `VinculoCobrancaService`, nunca aceitos em request.
- **Rationale**: evita qualquer endpoint novo (a feature de visualização futura já pode consumir `GET /api/alunos/{id}/vinculos-cobranca` existente); mantém o padrão já usado pelo projeto de DTOs espelhando a entidade 1:1, com campos calculados resolvidos no mapeamento do service (nenhum precedente de campo calculado em `VinculoCobrancaResponse` hoje, mas é o mesmo padrão já usado em outros DTOs do projeto, ex.: `TurmaNome` resolvido a partir da navegação).
- **Alternatives considered**: deixar o cálculo de "aulas dadas"/"aulas dadas no mês" inteiramente para o frontend da feature de visualização futura (expondo só os campos brutos) — descartado: violaria o Princípio II se a mesma conta precisasse ser refeita em mais de uma tela futura (dashboard individual e dashboard de negócio); calcular uma vez no backend evita duplicação.

## R8. Sem testes de frontend — mesma decisão já aceita no projeto

- **Decision**: testes só em `tests/SPI.Application.Tests` (xUnit, fakes manuais). Sem suíte de frontend (não existe no projeto) e sem mudança de frontend nesta feature (FR-013).
- **Rationale**: mesma decisão já aceita para toda mudança de backend anterior nesta sessão (specs/038, specs/039).
