# Relatório — Selo de cobrança do aluno (spec 068)

**Data:** 2026-11-04 · **Spec:** [specs/068-badge-cobranca-aluno](../../specs/068-badge-cobranca-aluno/spec.md) · **Card Trello:** "Exibir o vínculo de cobrança do aluno para o professor"

## Pedido

A professora precisa ver, sem abrir a ficha, **qual é a cobrança de cada aluno**: na lista de alunos, na página da turma e na chamada. Só exibição: nenhuma regra de cobrança muda.

## O que foi feito

**Backend (somente leitura, sem migração)**
- `ResumoCobrancaService` (novo): calcula o selo **em lote** (no máximo duas consultas por tela, qualquer que seja o número de alunos).
  - **Lista de alunos:** vínculo principal na ordem Mensalidade → Pacote → Avulsa (mais recente em empate), `+N` outros vínculos e a dica com todos.
  - **Turma e chamada:** o vínculo que **vale naquele grupo**, decidido por `RegraCobrancaPresenca.Decidir` (a mesma função que cobra a presença), com o progresso do pacote de `ProgressoPacote.Calcular` (spec 058). Pacote de outra turma não vale; pacote esgotado com avulsa vira "Avulsa" com aviso; esgotado sem avulsa vira "Pacote esgotado" (a presença seria barrada).
- `IVinculoCobrancaRepository.ListarAtivosDosAlunosAsync` (uma consulta `IN` com a turma).
- `AlunoService.ListarAsync(..., incluirCobranca)`: o resumo só é calculado quando a tela pede (`GET /api/alunos?incluirCobranca=true`); só a tela de Alunos pede. As outras 11 telas que usam a lista só para escolher um aluno ficam como antes.
- `TurmaService` preenche o selo dos alunos matriculados; `AulaService` preenche o selo nos retornos de **uma aula só** (obter, editar, participantes, registrar chamada, cancelar, link). A lista de turmas e a lista de aulas continuam sem selo.
- Parâmetros novos opcionais nos construtores (`IResumoCobrancaService? cobranca = null`), então nenhum teste antigo precisou mudar.

**Frontend**
- **Um único componente novo:** `components/alunos/SeloCobranca.tsx` (pílula `status-pill` com cores que já existiam; "restam N" no pacote, valor, turma e "+N" na lista, dica com todos os vínculos, aviso "pacote esgotado", traço quando não veio). Sem ação de clique e sem lógica.
- Coluna "Cobrança" em `/alunos`, selo por aluno em `/turmas/[id]` e na chamada `/aulas/[id]` (inclusive "Outros alunos").
- Tipos em `lib/api/vinculosCobranca.ts` (junto do `ProgressoPacote`), usados por `alunos.ts`, `turmas.ts` e `aulas.ts`.

**Documentação:** notas de Princípio V em `specs/056-cobranca-por-aluno/spec.md` e `specs/058-progresso-pacote/spec.md`.

## Validação

| Verificação | Resultado |
|---|---|
| `dotnet test tests/SPI.Application.Tests` | **603 aprovados**, 0 falhas (eram 575; 28 novos; nenhum `Assert` existente alterado) |
| Testes novos | `ResumoCobrancaServiceTests` (12: ordem do principal, +N, sem vínculo, pacote/esgotado/sem total, **consistência com `Decidir` numa matriz de vínculos**, pacote de outra turma, guarda de N+1 com 30 alunos), `AlunoServiceCobrancaListaTests` (4), `TurmaServiceCobrancaTests` (5), `AulaServiceCobrancaSeloTests` (7) |
| `dotnet build` | 0 erros, 12 avisos (os mesmos de antes: pacotes NuGet com vulnerabilidade conhecida) |
| `tsc`, `eslint`, `verificar-contraste`, `npm run build` | limpos; 48 combinações, 0 pares abaixo de 4,5:1; `next.config.ts` restaurado |
| API real, cenário "TESTE-068" (7 alunos, 3 turmas, chamadas reais) | **21 de 21** (lista, turma de pacote, turma de mensalidade, chamada com reposição, listas sem selo, parâmetro ausente = `cobranca` nulo) |
| Selo = cobrança real | aluno com selo "Pacote esgotado" teve a presença **barrada**; aluno com selo "Avulsa" teve a presença **gerando conta a receber** |
| Chrome, claro e escuro | 10 verificações ok (coluna, selos, "+1" e dica, "pacote esgotado", "Outros alunos", 360 px sem rolagem horizontal); capturas conferidas |
| Requisições de cada tela | as mesmas de antes (nenhuma chamada nova de API no frontend; `grep` de `listarVinculos`/`vinculos-cobranca` nas telas sem resultado) |
| "MUST NOT" (CLAUDE.md) | `git status --porcelain` mostra só os arquivos novos previstos; diff do frontend sem `.sort`/`.find`/`.filter` de vínculo nem `onClick` (o único `.filter(Boolean)` só junta textos da dica); sem cores literais; `frontend/styles` e `paleta.ts` sem diff; o controller só ganhou o parâmetro `incluirCobranca`; `RegraCobrancaPresenca.cs` e `ProgressoPacote.cs` sem diff; nenhuma escrita no serviço novo; nada em `database/` |
| Banco local | contagens idênticas às de antes depois da limpeza (aula 16, aula_aluno 14, pagamento 8, alunos 1, turma 4, vínculos 4); backup `database/backup/spi_db_20261104_114407.sql` |

## Decisões e observações

- **Ajuste durante a validação:** a pílula "Pacote · restam 7" quebrava em duas linhas na coluna estreita da lista; passou a não quebrar.
- **Pacote esgotado na lista:** o principal segue a ordem fixa (Pacote antes de Avulsa), então um aluno com pacote esgotado e avulsa aparece como "Pacote · restam 0" com "+1"; a dica mostra os dois. Na turma e na chamada aparece o que de fato vale.
- **Aula já realizada:** o selo mostra o vínculo **vigente hoje**, que pode ser diferente do aplicado na época; o histórico exato continua na ficha e nos relatórios (spec 052).
- **Limite da validação:** a contagem de consultas ao banco por resposta foi garantida pelo teste com fakes (uma consulta de vínculos por lista); não medi o SQL real da API.

## O que ficou de fora

Semestral/Anual (spec 062): entram sozinhas no selo quando existirem (a categoria vem do nome da modalidade). Nada foi commitado nem enviado (push).
