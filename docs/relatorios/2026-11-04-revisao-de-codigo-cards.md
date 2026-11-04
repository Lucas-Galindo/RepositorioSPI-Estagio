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
