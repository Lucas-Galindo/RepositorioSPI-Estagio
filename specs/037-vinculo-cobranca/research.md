# Research: Vínculo de Cobrança do Aluno

Todas as decisões abaixo foram tomadas lendo o código atual; nenhum `NEEDS CLARIFICATION` restou após `/speckit-clarify`.

## R1. Como garantir "no máximo um vínculo ativo por Aluno+Turma e por Aluno sem turma"

- **Decision**: Duas camadas. (1) `VinculoCobrancaService` consulta `IVinculoCobrancaRepository.ExisteAtivoAsync(alunoId, turmaId, ignorarId)` antes de criar, editar (quando a chave Aluno+Turma muda) e reativar, lançando `ConflitoException` (HTTP 409, padrão já usado em `TurmaService.VincularAlunoAsync`). (2) No MySQL, coluna gerada `chave_ativa` (`IF(ativo, CONCAT(aluno_id, ':', IFNULL(turma_id, 0)), NULL)`) com `UNIQUE INDEX`. `NULL` não colide em índice único do MySQL, então vínculos inativos ficam de fora (FR-007) e a chave `aluno:0` cobre o caso "sem turma" (ids de turma começam em 1). O `VinculoCobrancaRepository` traduz a violação de chave duplicada (erro MySQL 1062, via `DbUpdateException`) em `ConflitoException`, para corridas entre duas requisições simultâneas.
- **Rationale**: Um `UNIQUE (aluno_id, turma_id)` simples não serve: `NULL` em `turma_id` permitiria vários vínculos individuais e ele contaria vínculos inativos. A coluna gerada resolve os dois pontos no próprio banco. A verificação no serviço dá a mensagem clara exigida por SC-002; o índice fecha a janela de corrida.
- **Alternatives considered**: (a) só verificação no serviço — deixa corrida possível; (b) trigger — o projeto evita triggers (comentário em `02_criacao_tabelas.sql` sobre `aula`); (c) `UNIQUE` funcional/índice parcial — MySQL não tem índice parcial; a coluna gerada é o equivalente idiomático.

## R2. Onde vive a regra "turma deve ser ativa e do aluno" (FR-013) e quando é reaplicada

- **Decision**: No `VinculoCobrancaService`, usando `ITurmaRepository.ObterPorIdAsync` (que já carrega `AlunosTurma`): a turma existe, `Ativo == true` e `AlunosTurma` contém o aluno. Reaplicada no cadastro e, na edição, **somente quando `TurmaId` muda**. Falha → `ConflitoException` com mensagem clara (o projeto só tem `NaoEncontrado`/`Conflito`; não criar um tipo de exceção novo para uma regra só). Turma inexistente → `NaoEncontradoException`.
- **Rationale**: Reaproveita repositório existente (nenhum método novo). Não revalidar turma inalterada evita bloquear a edição de Valor num vínculo cuja turma foi desativada depois — consistente com o edge case da spec ("vínculos órfãos ficam para fatia futura"). Reativação reaplica apenas a unicidade (FR-014), como a spec define.
- **Alternatives considered**: reaplicar sempre — bloquearia edições legítimas de vínculos órfãos; criar `RegraNegocioException` (400) — mais correto semanticamente, mas amplia o escopo do projeto para uma única regra.

## R3. Representação da Modalidade

- **Decision**: `enum ModalidadeCobranca { Avulsa = 1, Mensalidade = 2, Pacote = 3 }` em `SPI.Domain/Enums`. No banco, `VARCHAR(20)` com `CHECK (modalidade IN ('Avulsa','Mensalidade','Pacote'))` e `HasConversion<string>()` no EF (mesmo padrão de `aula.status`, legível em consultas SQL). Na API, o DTO usa o enum diretamente; `Program.cs` já registra `JsonStringEnumConverter` globalmente, então o JSON trafega `"Avulsa" | "Mensalidade" | "Pacote"` e valor inválido vira 400 do model binding.
- **Rationale**: Pedido do usuário é "enum"; o padrão de texto no banco já existe. Nenhum código de tradução manual é necessário.
- **Alternatives considered**: `TINYINT` com valor numérico — menos legível no banco e diverge de `aula.status`; string livre validada por lista (padrão de `Lembrete.Canal`) — perde tipagem no domínio.

## R4. Obrigatoriedade de `AulasIncluidas` e `SaldoAulas`

- **Decision**: Ambos **opcionais** (nullable), exatamente como definido no pedido. `AulasIncluidas` só é aceito com Modalidade = Mensalidade e, se informado, deve ser ≥ 1; `SaldoAulas` só é aceito com Pacote e, se informado, deve ser ≥ 0 (um pacote pode zerar). Campo não aplicável à modalidade com valor informado → erro de validação. Regras no `VinculoCobrancaRequestValidator` (FluentValidation, Constituição Stack); `CHECK` no banco repete a aplicabilidade como defesa em profundidade (mesmo padrão de `chk_aula_status`).
- **Rationale**: FR-003/FR-004 falam em "permitir informar apenas quando…", nunca em "exigir". A redação "exigido" no cenário 2 de US2 era imprecisa e foi corrigida na spec para "aplicável".
- **Alternatives considered**: tornar `SaldoAulas` obrigatório para Pacote — contradiz o pedido ("opcional") e a spec.

## R5. Valor

- **Decision**: `decimal(10,2)`, mesmo tipo de `Aluno.ValorAula`; validação `> 0` (FR-002) e teto `<= 99999999.99` (limite do tipo, para devolver mensagem clara em vez de erro do banco).
- **Rationale**: Consistência com o campo monetário existente; a regra `> 0` difere de `ValorAula` (`>= 0`) porque a spec pede vínculo nunca gratuito nesta fatia (Assumptions).

## R6. Contrato REST

- **Decision**: Rota aninhada em aluno: `api/alunos/{alunoId}/vinculos-cobranca` no novo `VinculosCobrancaController` (`[Authorize(Roles = Professor)]`, try/catch com `NaoEncontrado`→404, `Conflito`→409, validação→400, como `TurmasController`). Verbos: `GET` (lista, filtro `ativo`), `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` (exclusão lógica), `PATCH /{id}/reativar`. Detalhes em [contracts/vinculos-cobranca-api.md](./contracts/vinculos-cobranca-api.md).
- **Rationale**: Um vínculo não existe sem aluno; a rota aninhada evita passar `alunoId` no corpo e permite que `id` de outro aluno devolva 404. Os verbos espelham `TurmasController`/`AlunosController` (`DELETE` lógico + `PATCH reativar`).
- **Alternatives considered**: rota plana `api/vinculos-cobranca?alunoId=` — funciona, mas espalha a checagem "vínculo pertence ao aluno"; `GET /{id}` é incluído só para dar destino ao `CreatedAtAction` (padrão do projeto).

## R6b. Filtro da listagem e "Mostrar excluídos" (FR-015)

- **Decision**: O parâmetro `ativo` (bool?) segue o padrão de `TurmasController.Listar`. O frontend chama com `ativo=true` por padrão e sem filtro (todos) ao ligar "Mostrar excluídos"; inativos vêm marcados visualmente e com botão "Reativar".
- **Rationale**: Nenhuma lógica nova no backend além do filtro já padrão do projeto.

## R7. Turmas oferecidas no formulário (FR-013 na UI)

- **Decision**: Adicionar `Ativo` (bool) a `TurmaResumoResponse` (aditivo; já usado dentro de `AlunoResponse.Turmas`) e ao tipo `TurmaResumo` do frontend. O modal lista `aluno.turmas.filter(t => t.ativo)`. A rejeição de turmas inválidas continua sendo do backend (R2).
- **Rationale**: `AlunoResponse.Turmas` já traz as turmas do aluno; falta só o indicador de ativa. Evita nova chamada/endpoint. O filtro é apresentação, não regra de negócio (Constituição II).
- **Alternatives considered**: endpoint `.../turmas-elegiveis` — mais uma rota para o mesmo dado; chamar `listarTurmas({ativo:true})` e cruzar no cliente — duplica a noção de elegibilidade no frontend.

## R8. Padrão de UI para adicionar/editar na própria seção

- **Decision**: Modal com formulário aberto a partir da seção, reaproveitando as classes `modal-overlay`/`modal` e o padrão do popup de aula da Agenda (`modal.modal-aula-popup` envolvendo `AulaForm`, em `agenda/page.tsx`), com uma classe própria de largura em `dashboard.css` se necessário. Exclusão usa `ConfirmModal` existente; reativação é botão direto (como `reativarAluno`). Ao trocar a Modalidade, o formulário limpa Aulas Incluídas/Saldo de Aulas locais (só conveniência; o backend rejeita de qualquer forma).
- **Rationale**: A investigação prévia da spec dizia não haver padrão de "adicionar na lista", mas há precedente de **formulário dentro de modal** na Agenda; o modal é o caminho de menor novidade e satisfaz o esclarecimento (sem navegar para outra página).
- **Alternatives considered**: formulário inline expansível — introduz um padrão visual sem precedente.

## R9. Escopo de FR-012 e como protegê-lo

- **Decision**: (1) `AulaService`, `AulaService.GerarContasAReceberAsync` e `Aluno.ValorAula` não são editados nem ganham dependência de `VinculoCobranca`; (2) teste de não-regressão `VinculoCobrancaNaoAfetaCobrancaAutomaticaTests` que verifica por reflexão que o construtor de `AulaService` não recebe nenhum tipo de vínculo de cobrança (`IVinculoCobrancaRepository`/`IVinculoCobrancaService`) e que `Aluno.ValorAula` continua `decimal`; (3) verificação no `quickstart.md` de que `git diff` não toca `AulaService.cs` nem a propriedade `ValorAula`.
- **Rationale**: Não existe hoje nenhuma suíte de `AulaService`; montar todos os fakes (7 dependências) só para provar comportamento inalterado seria desproporcional para uma feature de cadastro puro. O teste por reflexão + revisão de diff cobre exatamente o risco real (alguém ligar o vínculo à cobrança sem querer). Um teste comportamental completo pode ser adicionado em `/speckit-tasks` se se julgar necessário.
- **Alternatives considered**: teste comportamental de `RegistrarSessaoAsync` com fakes — mais forte, porém caro e frágil para o benefício.

## R10. Migração e aplicação

- **Decision**: `database/14_vinculo_cobranca.sql` (padrão de `13_endereco_aluno_responsavel.sql`: cabeçalho de descrição, `USE spi_db;`, comentários por coluna). Aplicar via `mysql` CLI durante a implementação, obtendo a connection string dos .NET User Secrets (regra do usuário registrada em memória: migrações são aplicadas na implementação, não deixadas manuais). Nenhuma stored procedure é necessária (sem geração de identificador especial).
- **Rationale**: Cumpre a Constituição (schema versionado em `database/*.sql`) e a preferência do usuário.

## R11. Fora do escopo confirmado

- Efeito automático do vínculo sobre `GerarContasAReceberAsync`/`ValorAula`/saldo de pacote (Regra 4 do pedido).
- Comportamento de vínculos cuja turma foi desativada depois, ou aluno removido da turma (edge case da spec).
- Bloquear cadastro/edição de vínculo para aluno inativo: não especificado; a API só exige que o aluno exista (404 caso contrário).
