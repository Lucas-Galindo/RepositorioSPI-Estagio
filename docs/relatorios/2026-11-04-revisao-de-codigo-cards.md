# Relatório: revisão de código e de conceito dos cards em "Revisão de Código/Conceito" (2026-11-04)

**Pedido**: para cada card em revisão, procurar código morto, complexidade desnecessária, redundância e lógica ruim ou errada, **só dentro do escopo do card**, sem quebrar o programa. Decisões do usuário: bug real → corrijo com teste e reporto; o card 065 entra na revisão; commit e push por card.

**Ordem**: 066 (menor), 064, 065, 063. Cada seção abaixo é fechada no commit do respectivo card.

---

## Card 066 — Aba Lembretes dividida em "Lembretes de aula" e "Lembretes de pagamento"

**Escopo revisado**: o que a spec 066 mudou em `frontend/app/(app)/lembretes/page.tsx` (os títulos, as frases e a divisória). Nada além disso.

**Achado e ação**
| # | Tipo | Achado | Ação |
|---|---|---|---|
| 1 | Redundância | O mesmo objeto de estilo inline (`display:flex; alignItems:center; gap:8`) estava repetido nos dois títulos de seção | Extraído para a constante `TITULO_SECAO` (mesmo CSS; nenhuma mudança visual) |

**Sem achado**: nenhum código morto, nenhuma lógica nova (a mudança é só de apresentação), nenhum estado ou handler tocado. A divisória e os textos já usam só estilos e tokens existentes.

**Fora do escopo (visto, não alterado)**: o bloco "Pagamento pendente" (`LembretePagamentoSection`, spec 060) repete o ícone e fica com um título aninhado sob "Lembretes de pagamento"; é da spec 060, não do 066.

**Validação**: `tsc` e `eslint` limpos; o CSS aplicado é o mesmo de antes (a captura visual da spec 066 continua válida).

---

## Card 064 — Chamada só nos detalhes da aula e aviso de aula sem frequência

**Escopo revisado**: o que a spec 064 criou ou mudou: `RegraChamada`, `RelogioExtensions`, `AulaService` (criar, registrar chamada, `Mapear`), notificações sem corte, `aulas/[id]/page.tsx`, `AulaForm` e os testes novos.

| # | Tipo | Achado | Ação |
|---|---|---|---|
| 1 | Redundância / lógica frágil | `Mapear` lia o relógio **duas vezes** por aula (podia sair com dois instantes diferentes na mesma resposta) e, na lista de aulas, uma vez por aula | `MapearEm(aula, agora)`: o relógio é lido **uma vez** por resposta; a lista inteira usa o mesmo instante |
| 2 | Redundância | `RegraChamada.Liberada` e `SegundosParaLiberar` repetiam a checagem "ativa e Agendada" | Extraída para `AguardaChamada` (um lugar só) |
| 3 | Redundância na tela | A tela repetia `aula.status === "Agendada"` onde `chamadaLiberada` já implica isso | Removido; o checkbox e o botão dependem só de `chamadaLiberada` |
| 4 | Lógica errada (borda) | A mensagem "A chamada ficará disponível…" aparecia para qualquer aula Agendada sem chamada liberada, inclusive uma aula **excluída** aberta pelo endereço | A mensagem agora aparece só quando o servidor informa `segundosParaChamada` (o caso "ainda não começou"); para aula excluída nada é mostrado |
| 5 | Nome obsoleto | Dois testes ainda se chamavam `Aula_retroativa_…` | Renomeados para `Aula_antiga_…` (o comportamento retroativo não existe mais) |

**Sem achado**: nenhum código morto (nada sobrou do desenho antigo: `retroativ…`, corte de vigência e `AulasPendentesDesde` só aparecem em comentários históricos de testes e nas notas das specs 055/059); a migração 26, a consulta de auditoria e o fuso estão como especificados.

**Validação**: 507 testes (com build), `tsc` e `eslint` limpos. As mudanças 1 a 3 e 5 preservam o comportamento; a 4 só muda o caso de aula excluída (coberto por `RegraChamadaTests` e `AulaServiceChamadaTests`, que já fixam `segundosParaChamada` nulo para aula excluída).

> Adendo ao 064: o analisador IDE0005 achou um `using SPI.Domain.Enums` sem uso em `AulaService.cs`; removido (commit à parte).

---

## Card 065 — Melhoria do código sem mudar o comportamento (segunda passada)

**Escopo revisado**: `ErrosDeNegocioFilter`, a remoção dos `try/catch` dos 17 controllers, o CSS removido, os exports de `jwt.ts`/`paleta.ts`, o `PackageReference`, `ValidarTurmaDoVinculoAsync` e os 43 comentários de regra.

**Como**: leitura do filtro e dos controllers, varredura de sobras (campos `_logger` e `using`) e o analisador IDE0005 (`using` sem uso) ligado temporariamente por um `.editorconfig` descartável.

| # | Tipo | Resultado |
|---|---|---|
| 1 | Sobras nos controllers | Nenhum `_logger` ficou só declarado: todos os controllers que o têm ainda o usam (login, refresh, logout e demais registros). Nenhum campo morto |
| 2 | `using` sem uso | O analisador não acusou nada nos arquivos que o 065 tocou (os `using SPI.Domain.Exceptions` sem uso já tinham saído) |
| 3 | Filtro de erros | A lógica confere com o contrato: 404/409 só quando a ação declara o status, senão 500 com o mesmo corpo de `ControllerBase.Problem`; o teste compara os dois. Nada a simplificar |
| 4 | Comentários | Os comentários continuam explicando o porquê e nenhum repete o código |

**Nada alterado neste card** (a segunda passada não achou o que corrigir).

**Vistos e não alterados**
- `Program.cs` tem um `using System.Threading.RateLimiting` sem uso (anterior ao 065, não é do escopo).
- **Sugestão (muda comportamento, por isso não feita)**: o 500 devolve a mensagem da exceção em `detail` (igual a antes do 065). Em produção isso pode vazar texto interno (por exemplo de banco). Vale um card: mostrar o detalhe só em Development e registrar a exceção em log.

---

## Card 063 — Alunos de pacote fora do cadastro fixo das turmas (turma de pacote e pacote por grupo)

**Escopo revisado**: o que a spec 063 criou ou mudou: `Turma` (tipo, datas, `EstaIniciada`/`EstaEmAndamento`), `TurmaService`, validador de turma, `EncerramentoTurmaPacoteService` e a rotina `TurmaEncerramentoDispatcherService`, as regras de pacote em `VinculoCobrancaService`, as consultas de pacote do repositório, `RegraCobrancaPresenca` (pacote só no seu grupo) e as telas de turma, vínculo e "Outros alunos". Lidos os arquivos principais; o comportamento foi preservado.

| # | Tipo | Achado | Ação |
|---|---|---|---|
| 1 | Redundância | `TurmaService` repetia duas vezes o par "ler a turma de novo e mapear" (vincular e desvincular aluno) | Extraído para `RecarregarEMapearAsync` |
| 2 | Complexidade | `MapearComPacotesAsync` fazia `turma.AlunosTurma.Any(...)` dentro de um `Where` (varredura por pacote) | Usa um `HashSet` das matrículas (uma passada) |
| 3 | Redundância | `EncerramentoTurmaPacoteService` tinha o mesmo laço "inativar pacotes e contar" nos dois métodos | Extraído para `Inativar` (só a coluna `Ativo` muda, como na spec) |

**Conferido e sem achado**: o critério de "turma encerrada" da rotina (`DataFim < hoje` ou turma inativa) coincide com `Turma.EstaEmAndamento`; a rotina é idempotente e só grava `Ativo`; as consultas de pacote são em lote e filtradas por ativo; a regra "pacote só no seu grupo" está em um lugar só (`RegraCobrancaPresenca.Decidir`); nenhum código morto do desenho anterior ao 063.

**Vistos e não alterados (sugestões)**
- A validação de turma existe no validador **e** no serviço (datas por tipo). É deliberado (o serviço é chamado direto pelos testes e precisa mesclar com as datas já gravadas na edição), mas é o ponto mais próximo de duplicar regra; manter até haver um motivo para unificar.
- A página da turma busca **todos os alunos ativos** mesmo em turma de pacote, onde o resultado não é usado (a lista vem de `alunosComPacoteForaDaTurma`). Evitar a chamada exigiria carregar a turma antes ou devolver "alunos disponíveis" pelo servidor (que também tiraria o filtro da tela): vale como card pequeno.
- `DateTime.Now` na rotina de fim de turma, em `Turma`/`TurmaService` e na validação do pacote (fuso do servidor, não o de São Paulo): é o mesmo ponto já listado como sugestão de card na spec 064.

**Validação**: 507 testes (com build), cobrindo vincular/desvincular, pacote fora da turma e encerramento.

---

## Resumo geral

| Card | Mudanças de código | Resultado |
|---|---|---|
| 066 Lembretes em duas seções | 1 (estilo repetido) | `tsc`/`eslint` limpos |
| 064 Chamada nos detalhes | 5 + 1 `using` | 507 testes; corrigida a mensagem de bloqueio para aula excluída |
| 065 Melhoria de código | 0 | sem sobra; sugestão de não devolver a mensagem da exceção em produção |
| 063 Pacote fora da turma | 3 | 507 testes |

Nenhum comportamento foi quebrado; o único ajuste de comportamento é o do item 4 do 064 (aula excluída).
