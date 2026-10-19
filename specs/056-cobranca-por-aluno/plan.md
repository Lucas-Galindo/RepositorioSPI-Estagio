# Implementation Plan: Cobrança por Aluno e Reposição de Aulas

**Branch**: `056-cobranca-por-aluno` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/056-cobranca-por-aluno/spec.md`

## Summary

Mensalidade e Pacote deixam de ser vínculos "por contexto" e passam a ser do aluno (sempre sem turma); Avulsa continua por contexto. A decisão de cobrança de cada presença deixa de ser "procure o vínculo exatamente deste contexto" e passa a ser uma regra de prioridade única (mensalidade → pacote com saldo → avulsa do contexto → bloqueio por pacote esgotado → valor da aula), implementada uma vez numa função pura reaproveitada pelo registro de sessão. O cadastro de vínculo ganha as regras de exclusividade (mensalidade exclusiva; um pacote por aluno; uma avulsa por contexto). A aula de turma ganha participantes de reposição (`aula_aluno.reposicao`), adicionáveis/removíveis enquanto a aula está agendada e preservados na edição. Uma migração SQL move pacotes/mensalidades existentes para "do aluno" sem desativar nada; conflitos herdados aparecem como aviso na ficha do aluno.

## Technical Context

**Language/Version**: C# / .NET 10 (backend); TypeScript / Next.js App Router + React (frontend)

**Primary Dependencies**: ASP.NET Core, EF Core (Pomelo MySQL), FluentValidation; Next.js/React — nenhuma dependência nova

**Storage**: MySQL `spi_db` — migração nova `database/21_cobranca_por_aluno.sql` (troca da chave de unicidade de `vinculo_cobranca`, `turma_id` nulo para Mensalidade/Pacote, coluna `aula_aluno.reposicao`)

**Testing**: xUnit com fakes manuais (`tests/SPI.Application.Tests/`); validação manual via `quickstart.md` no frontend (sem suíte de testes de frontend)

**Target Platform**: aplicação web (API REST + Next.js), como o restante do sistema

**Project Type**: Web application

**Performance Goals**: sem meta nova; a decisão de cobrança passa de 1 consulta por aluno presente ("vínculo do contexto") para 1 consulta por aluno ("vínculos ativos do aluno", lista pequena) — mesma ordem de grandeza

**Constraints**: a regra de prioridade de cobrança MUST existir em um único lugar (Princípio II); a migração MUST NOT desativar/somar vínculos (FR-021); backup do banco local antes de aplicar a migração (regra de segurança do usuário)

**Scale/Scope**: backend (Domain, Application, Infrastructure, Api), frontend (ficha do aluno/vínculos, detalhe da aula), 1 migração, correção de 5 specs antigas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Princípio I (Exclusão Lógica)**: ✅ — a migração não exclui nem desativa nenhum vínculo; remover participante de reposição de uma aula ainda agendada apaga só a linha de participação dessa aula (sem histórico de presença/cobrança, já que a aula não aconteceu), mesmo tratamento que a edição de aula já dá hoje à lista de alunos de uma aula agendada.
- **Princípio II (Validação centralizada no backend)**: ✅ — prioridade de cobrança numa função única (`RegraCobrancaPresenca`), exclusividade de vínculos só no `VinculoCobrancaService`, conflito herdado calculado no backend e só exibido pelo frontend. O frontend só esconde o campo turma para Mensalidade/Pacote (conveniência); o backend rejeita turma nessas modalidades de qualquer forma.
- **Princípio III (Nenhuma capacidade sem implementação real)**: ✅ — o formulário deixa de oferecer turma para Mensalidade/Pacote exatamente porque o backend deixa de aceitá-la.
- **Princípio IV (Segredos)**: N/A.
- **Princípio V (Documentação retroativa vinculante)**: ✅ — esta feature muda intencionalmente comportamento descrito em specs/037, 038, 039, 041 e 052; cada uma recebe nota apontando para esta spec (FR-023), em vez de ficar desatualizada em silêncio.

**Gate status**: PASS.

**Re-check pós-Phase 1**: PASS — o design (research.md R1-R8) não introduz duplicação de regra nem opção de UI sem suporte; o único ponto sensível (unicidade de Pacote/Mensalidade só no serviço, sem índice no banco) está justificado em R3.

## Project Structure

### Documentation (this feature)

```text
specs/056-cobranca-por-aluno/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── vinculos-cobranca-api.md
│   └── aulas-participantes-api.md
└── tasks.md
```

### Source Code (repository root)

```text
database/21_cobranca_por_aluno.sql                         # migracao nova

src/SPI.Domain/
├── Entities/AulaAluno.cs                                  # + Reposicao
└── Repositories/IVinculoCobrancaRepository.cs             # - ExisteAtivoAsync, - ObterAtivoPorAlunoEContextoAsync
src/SPI.Application/
├── Aulas/
│   ├── Services/RegraCobrancaPresenca.cs                  # novo: decisao de cobranca (funcao pura)
│   ├── Services/AulaService.cs                            # usa a regra; reposicao; edicao preserva extras
│   ├── Services/IAulaService.cs                           # + AdicionarParticipante/RemoverParticipante
│   └── Dtos/AlunoPresencaResponse.cs                      # + Reposicao
├── VinculosCobranca/
│   ├── Services/VinculoCobrancaService.cs                 # exclusividade; conflito herdado
│   ├── Validators/VinculoCobrancaRequestValidator.cs      # turma so para Avulsa
│   └── Dtos/VinculoCobrancaResponse.cs                    # + Conflito
└── Dashboard/Services/DashboardService.cs                 # rotulo do pacote ("Todas as aulas do aluno")
src/SPI.Infrastructure/
├── Persistence/Configurations/AulaAlunoConfiguration.cs   # + reposicao
├── Repositories/VinculoCobrancaRepository.cs             # remove metodos por contexto; msg de duplicidade
└── BackgroundServices/MensalidadeDispatcherService.cs     # descricao sem turma
src/SPI.Api/Controllers/AulasController.cs                 # + POST/DELETE participantes

frontend/
├── lib/api/aulas.ts, lib/api/vinculosCobranca.ts          # tipos + funcoes novas
├── components/alunos/VinculoCobrancaFormModal.tsx         # turma so para Avulsa
├── components/alunos/VinculosCobrancaSection.tsx          # rotulo + aviso de conflito
└── app/(app)/aulas/[id]/page.tsx                          # adicionar/remover aluno de reposicao

specs/037, 038, 039, 041, 052 /spec.md                     # nota "substituido em parte pela spec 056"
tests/SPI.Application.Tests/                               # testes novos + ajuste dos que assumem o modelo antigo
```

**Structure Decision**: mesma arquitetura em camadas existente; nenhuma camada ou projeto novo. A única classe nova de backend é `RegraCobrancaPresenca` (função pura, testável sem fakes de repositório).

## Complexity Tracking

> Nenhuma violação do Constitution Check a justificar.
