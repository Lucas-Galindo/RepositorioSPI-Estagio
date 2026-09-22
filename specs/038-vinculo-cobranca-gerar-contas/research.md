# Research: Cobrança Automática por Modalidade do Vínculo

Todas as decisões abaixo foram tomadas lendo o código atual; nenhum `NEEDS CLARIFICATION` restou após `/speckit-clarify`.

## R1. Como buscar "o vínculo correspondente ao contexto" (FR-001)

- **Decision**: Novo método em `IVinculoCobrancaRepository`: `Task<VinculoCobranca?> ObterAtivoPorAlunoEContextoAsync(int alunoId, int? turmaId, CancellationToken cancellationToken = default)`. Implementação: `_dbContext.VinculosCobranca.FirstOrDefaultAsync(v => v.AlunoId == alunoId && v.TurmaId == turmaId && v.Ativo, ct)` — mesma comparação `TurmaId == turmaId` (incluindo `null == null`) já usada em `ExisteAtivoAsync`.
- **Rationale**: A unicidade "no máximo um vínculo ATIVO por Aluno+Turma (ou Aluno sem turma)" já é garantida por specs/037 (validação de serviço + índice único `uq_vinculocobranca_chave_ativa`), então `FirstOrDefaultAsync` nunca é ambíguo. Reaproveita a mesma forma de comparação de `ExisteAtivoAsync`, sem introduzir uma segunda lógica de "correspondência de contexto".
- **Alternatives considered**: Reaproveitar `ListarPorAlunoAsync(alunoId, true)` e filtrar em memória por `TurmaId` no `AulaService` — descartado por espalhar a regra de "qual vínculo corresponde ao contexto" para fora do repositório (Constituição II: validação/regra de busca deve ter um único dono).

## R2. Onde a modalidade é decidida e onde a exceção do EX-001 é referenciada

- **Decision**: Toda a ramificação fica dentro do método privado `AulaService.GerarContasAReceberAsync` (não em um serviço novo). Para cada `AulaAluno` com `Presente == true`, primeiro busca o vínculo via R1; depois um `switch` sobre `vinculo?.Modalidade` (tratando `null` como "sem vínculo" = FR-002) decide: gerar `Pagamento` com `Aluno.ValorAula` (sem vínculo), gerar `Pagamento` com `VinculoCobranca.Valor` (Avulsa), não gerar nada (Mensalidade), ou não gerar nada + decrementar `SaldoAulas` (Pacote).
- **Rationale**: Único ponto de entrada de geração automática de cobrança no sistema (chamado só por `RegistrarSessaoAsync`) — manter a decisão ali evita duplicar a regra "qual efeito uma presença tem" em dois lugares (Constituição II).
- **Alternatives considered**: Extrair um `IVinculoCobrancaEfeitoStrategy`/Strategy Pattern por modalidade — descartado por desproporcional para 3 ramos simples e sem reuso previsto fora deste método; aumentaria a superfície de código sem necessidade real (evitar over-engineering).

## R3. Como o decremento de `SaldoAulas` é persistido

- **Decision**: `vinculo.SaldoAulas -= 1;` seguido de `await _vinculoCobrancaRepository.SalvarAlteracoesAsync(cancellationToken);`, chamado dentro do mesmo laço do `GerarContasAReceberAsync`, logo após decidir o ramo Pacote. `IVinculoCobrancaRepository.SalvarAlteracoesAsync` já existe (specs/037) e já embrulha `DbUpdateException` de violação de índice único em `ConflitoException` — irrelevante aqui (não há `INSERT` nem mudança de `TurmaId`/`Ativo`, só `UPDATE` de uma coluna numérica, que nunca colide com `uq_vinculocobranca_chave_ativa`).
- **Rationale**: Reaproveita o repositório e o método de persistência já existentes, sem criar um segundo caminho de escrita para `VinculoCobranca` (Constituição II) e sem tocar no schema (specs/037 já modela `saldo_aulas` como `INT NULL`, editável).
- **Alternatives considered**: Método dedicado `DecrementarSaldoAsync(id, ct)` no repositório — descartado: a entidade já está carregada em memória (via R1) dentro da mesma unidade de trabalho do `SpiDbContext`; `SalvarAlteracoesAsync` genérico já basta, e `AulaService` já segue esse padrão para outras entidades (ex.: `vinculo.Aluno.Frequencia += 1` seguido de `_aulaRepository.SalvarAlteracoesAsync`).

## R4. Atomicidade entre alunos da mesma aula

- **Decision**: Manter o padrão já existente — nenhuma transação explícita envolvendo todos os alunos da aula. Cada iteração do laço persiste seu próprio efeito (gera `Pagamento` OU decrementa `SaldoAulas`) antes de passar para o próximo aluno, exatamente como o código atual já faz (`SalvarAlteracoesAsync` por iteração antes do `SalvarAlteracoesAsync` final).
- **Rationale**: É o comportamento já aceito no código-base hoje (`GerarContasAReceberAsync` já salva por iteração, não em lote único) — esta feature não piora nem melhora a atomicidade existente, e introduzir uma transação nova aqui seria uma mudança de escopo não pedida pelo usuário.
- **Alternatives considered**: Envolver o laço inteiro em uma transação EF Core (`_dbContext.Database.BeginTransactionAsync`) — descartado: mudança de comportamento transacional não solicitada, fora do escopo desta spec (nenhum FR pede atomicidade entre alunos), e o projeto não usa transações explícitas em nenhum outro serviço hoje.

## R5. `SaldoAulas` nunca informado (`null`) tratado como já esgotado (FR-006)

- **Decision**: `null` e `0` seguem o mesmo ramo: nenhuma cobrança, nenhum decremento, nenhuma exceção. Implementado como `if (vinculo.SaldoAulas is > 0) { decrementa } ` — qualquer outro caso (`null` ou `<= 0`) cai no "não faz nada".
- **Rationale**: Já documentado como Assumption no spec.md; evita introduzir um estado de erro para um cenário legítimo (professora cadastrou o Pacote mas ainda não preencheu o saldo).
- **Alternatives considered**: Tratar `null` como erro/lançar exceção — descartado: quebraria a aula (uma presença não pode falhar por causa de um cadastro incompleto de cobrança, que é responsabilidade de tela separada).

## R6. Cobertura de teste dos requisitos negativos (FR-009, FR-010, FR-011)

- **Decision**: Três testes dedicados em `AulaServiceGerarContasAReceberTests.cs`:
  1. Modalidade Mensalidade: nenhuma `Pagamento` é adicionada ao fake de `IPagamentoRepository`, e nenhum campo do `VinculoCobranca` (Valor, AulasIncluidas) muda — prova de FR-009 (nenhum job/efeito além de "não cobrar").
  2. Modalidade Pacote com `SaldoAulas = 0`: nenhuma `Pagamento` é adicionada, `SaldoAulas` permanece `0`, e nenhuma exceção/estado de "aviso" é lançado ou sinalizado — prova de FR-010 (nenhum aviso/bloqueio implementado).
  3. Para todos os 4 cenários (sem vínculo, Avulsa, Mensalidade, Pacote): o fake de `IVinculoCobrancaRepository` nunca recebe chamada a `AdicionarAsync`/`ExisteAtivoAsync` (métodos de cadastro) — só `ObterAtivoPorAlunoEContextoAsync` e, no caso Pacote, `SalvarAlteracoesAsync` — prova de FR-011 (o cadastro do vínculo em si não é tocado por esta feature) — mesmo padrão de guarda estrutural já usado em `VinculoCobrancaNaoAfetaCobrancaAutomaticaTests.cs` (specs/037), só que na direção oposta (agora `AulaService` PODE depender do vínculo, mas não pode escrever nada além de `SaldoAulas`).
- **Rationale**: Segue a regra já estabelecida no projeto de que todo FR "MUST NOT" precisa de tarefa de teste própria, não só os cenários positivos.

## R7. Escopo confirmado fora desta feature

- Job de geração automática de cobrança mensal para vínculos Mensalidade (FR-009).
- Qualquer aviso, bloqueio de agendamento ou renovação automática quando `SaldoAulas` chega a zero (FR-010).
- Qualquer mudança em `VinculoCobrancaController`, `VinculoCobrancaService`, ou nas telas de `frontend/components/alunos/VinculosCobrancaSection.tsx`/`VinculoCobrancaFormModal.tsx` (specs/037) — incluindo o aviso "Este vínculo é apenas um cadastro..." (FR-011; ver nota de EX-001 em plan.md).
- Qualquer mudança em `Aluno.ValorAula` ou nos demais métodos de `AulaService` (`CadastrarAsync`, `AtualizarAsync`, `CancelarAsync`, etc.) além do próprio `GerarContasAReceberAsync`.
