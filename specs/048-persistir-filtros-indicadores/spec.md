# Feature Specification: Persistir Filtros da Visão de Indicadores

**Feature Branch**: `048-persistir-filtros-indicadores`

**Created**: 2026-10-08

**Status**: Draft

**Input**: User description: "Persistir os filtros da página de Indicadores (Financeiro) ao navegar para o detalhe de Entradas/Saídas (spec 045) e voltar, e adicionar um botão para limpar os filtros. Problema: hoje, ao clicar em Entradas ou Saídas, ver o detalhe e voltar, os filtros aplicados (período, turma, aluno e quaisquer outros existentes na página) voltam ao padrão e o usuário precisa refazê-los. Requisitos: 1. Ao voltar do detalhe para Indicadores, os filtros devem estar restaurados exatamente como estavam, e os números exibidos devem corresponder a eles. 2. A persistência vale só dentro da sessão de uso (ir e voltar); não precisa sobreviver a fechar o navegador nem ao logout. 3. Botão 'Limpar filtros' na área de filtros de Indicadores, só ativo/visível quando houver algum filtro diferente do padrão. 4. O detalhe de Entradas/Saídas continua recebendo os mesmos filtros de Indicadores, sem mudança no que o detalhe mostra. 5. Sem mudança de backend, nenhuma regra de cálculo alterada. 6. Reaproveitar o estilo de botão e os componentes que já existem, sem cor nem componente novo se houver equivalente. Fora de escopo: persistir filtros de outras páginas, ou entre sessões/dispositivos."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **O problema é real e já tinha sido previsto, mas não resolvido**: a spec 045 (lançamentos clicáveis) já identificou, em sua própria investigação, que os filtros da aba Indicadores são 7 `useState` locais (`inicio`, `fim`, `turmaId`, `materiaId`, `alunoId`, `semestre`, `tipoLancamento`) em `frontend/app/(app)/relatorios/financeiro/page.tsx`, nenhum refletido na URL — e decidiu, na época, não resolver isso de verdade (resolveria só "se viável sem complexidade extra", com fallback aceito, por ser um requisito `SHOULD` de prioridade P3 naquela spec). A tarefa de validação manual que confirmaria o comportamento real nunca foi marcada como concluída em `specs/045-lancamentos-clicaveis/tasks.md` — mas o relato deste pedido ("hoje, ao... voltar, os filtros voltam ao padrão") **é** essa confirmação empírica: o resultado observado foi "resetado". Esta feature promove a correção desse comportamento de um `SHOULD` aceito-como-falha para um `MUST` resolvido de verdade.
- **Achado importante sobre o Requisito 3 ("adicionar" botão Limpar filtros): o botão JÁ EXISTE**, e já atende ao requisito como pedido. `relatorios/financeiro/page.tsx` já tem, dentro da `.filter-bar` (linhas ~159-206, renderizada para as duas abas, não só "Financeiro"): `const temFiltro = Boolean(inicio || fim || formaPagamentoId || alunoId || turmaId || materiaId)` e `{temFiltro && (<button className="filter-clear" onClick={limparFiltros}>Limpar filtros</button>)}`, com `limparFiltros` já resetando todos os 6 filtros ali listados. Esta feature **não precisa criar esse botão** — só garantir que ele continue existindo e que `temFiltro`/`limparFiltros` também considerem o filtro `tipoLancamento` (Entradas/Saídas, específico da aba Indicadores, spec 022/045), que hoje não entra nessa conta.
- **Achado adicional de escopo**: a aba ativa (`aba: "financeiro" | "indicadores"`, também um `useState` local, padrão `"financeiro"`) sofre do mesmo problema — se o Admin está na aba Indicadores, clica num lançamento, e volta, hoje ele é jogado de volta para a aba "Financeiro" (o padrão), não para onde estava. Embora o pedido original fale apenas de "filtros", a mesma causa raiz (estado local perdido no remount) afeta a aba ativa, e deixá-la de fora contrariaria a experiência que o Requisito 1 pede ("restaurados exatamente como estavam").
- **Mecanismo escolhido, e por que não o mesmo já descartado pela spec 045**: a spec 045 descartou refletir os filtros na URL e usar `sessionStorage`, por desproporção a um requisito `SHOULD` P3. Agora que isso é o núcleo de uma feature própria (`MUST`), um mecanismo real é necessário. `sessionStorage` é o que corresponde exatamente ao Requisito 2 ("não precisa sobreviver a fechar o navegador nem ao logout" — é literalmente o ciclo de vida do `sessionStorage`, que o próprio navegador limpa ao fechar a aba/janela). Refletir na URL funcionaria tecnicamente, mas sobreviveria a fechar o navegador e a favoritos/compartilhamento — mais do que o pedido exige, e exigiria sincronizar 8 variáveis de estado com query params bidirecionalmente. `sessionStorage` já tem precedente de padrão seguro no projeto: `ThemeContext.tsx` já lê um storage do navegador **só depois do mount**, dentro de um `useEffect` (nunca como inicializador preguiçoso do `useState`), justamente porque o storage do navegador não existe durante o SSR e usá-lo antes do mount quebraria a hidratação — o mesmo cuidado se aplica aqui, só que com `sessionStorage` em vez de `localStorage` (o projeto só usa `localStorage` hoje, para dados que devem sobreviver ao fechamento do navegador — token de autenticação e preferência de tema — semântica oposta à exigida aqui).
- **Como a restauração dispara os dados corretos "automaticamente"**: os dois `useEffect` que buscam os dados da página (`obterRelatorioFinanceiro`/`obterIndicadoresFinanceiros`) já são reativos aos `useState` dos filtros (`[sessao, inicio, fim, formaPagamentoId, alunoId, turmaId, materiaId]` e equivalente para indicadores) — ou seja, assim que os filtros forem restaurados nesses `useState`, os dados são recarregados pelos mesmos efeitos que já existem hoje, sem precisar de nenhuma chamada de API nova nem de nenhum recálculo no frontend (Requisito 5).
- **Nenhuma mudança nas telas de detalhe**: `contas-a-pagar/[id]`/`contas-a-receber/[id]` não leem nem escrevem esse armazenamento — continuam recebendo só o `id` na rota, como hoje (Requisito 4, spec 045 inalterada).

## Clarifications

_Nenhuma pendente — a investigação prévia resolveu o único ponto técnico não trivial (mecanismo de persistência: `sessionStorage`, não URL/query params) com base na própria redação do Requisito 2 e num precedente de segurança de hidratação já estabelecido no projeto (`ThemeContext.tsx`). O restante do pedido já é preciso o suficiente para especificar sem necessidade de `/speckit-clarify`._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Filtros restaurados ao voltar do detalhe (Priority: P1)

Como Admin ou professora olhando a Visão de Indicadores com um período e outros filtros aplicados, eu quero que, ao clicar num lançamento, ver o detalhe, e voltar, os mesmos filtros continuem aplicados e os números exibidos correspondam a eles, para não precisar refazer a filtragem a cada lançamento que eu for investigar.

**Why this priority**: é o núcleo do pedido — sem isso, o botão "Limpar filtros" e qualquer outro ajuste seriam irrelevantes, já que o problema relatado é especificamente perder os filtros ao voltar.

**Independent Test**: Aplicar um período e um filtro de turma/aluno na Visão de Indicadores, clicar num lançamento (entrada ou saída), voltar pelo botão do navegador (ou da tela de detalhe), e confirmar que os mesmos filtros continuam selecionados e os valores exibidos (tabela de lançamentos, KPIs, fluxo de caixa) correspondem a eles.

**Acceptance Scenarios**:

1. **Given** a Visão de Indicadores com período e turma filtrados, **When** o Admin clica num lançamento e depois volta, **Then** o período e a turma continuam exatamente como estavam, e os dados exibidos refletem esses filtros (sem precisar de nenhuma ação adicional).
2. **Given** a tabela de lançamentos filtrada por "Entradas" ou "Saídas" (spec 022), **When** o Admin clica num lançamento e volta, **Then** esse filtro de tipo também continua aplicado.
3. **Given** o Admin estava na aba "Visão de Indicadores" (não a aba padrão "Visão Financeiro"), **When** ele clica num lançamento e volta, **Then** a página volta a mostrar a aba "Visão de Indicadores", não a aba padrão.
4. **Given** nenhum filtro havia sido aplicado antes de clicar num lançamento, **When** o Admin volta, **Then** a página continua sem filtros, exatamente como estava (nenhum comportamento novo quando não há nada a restaurar).

---

### User Story 2 - Limpar todos os filtros de uma vez (Priority: P2)

Como Admin que aplicou vários filtros na Visão de Indicadores, eu quero um botão para limpar todos de uma vez e ver os dados sem filtro, para não precisar desfazer cada filtro manualmente um por um.

**Why this priority**: é um complemento de conveniência ao redor da User Story 1 — a persistência (US1) é o que resolve o problema relatado; o botão de limpar já existe hoje para a maioria dos filtros e só precisa ser estendido para cobrir o filtro de tipo (Entradas/Saídas), por isso tem prioridade um nível abaixo.

**Independent Test**: Com algum filtro aplicado (incluindo possivelmente só o filtro Entradas/Saídas), clicar em "Limpar filtros" e confirmar que todos os filtros voltam ao padrão, os dados são recarregados sem filtro, e o botão desaparece (ou fica inativo) em seguida.

**Acceptance Scenarios**:

1. **Given** um ou mais filtros aplicados (período, turma, matéria, aluno, forma de pagamento, ou o filtro Entradas/Saídas da aba Indicadores), **When** o Admin clica em "Limpar filtros", **Then** todos esses filtros voltam ao valor padrão e os dados exibidos são recarregados sem nenhum filtro.
2. **Given** nenhum filtro aplicado (estado padrão), **When** o Admin olha a área de filtros, **Then** o botão "Limpar filtros" não aparece (ou fica inativo) — mesmo comportamento já existente hoje para os demais filtros.
3. **Given** o Admin acabou de limpar os filtros, **When** ele clica num lançamento e volta, **Then** a página continua sem filtro (o estado "limpo" também é o que fica persistido).

---

### Edge Cases

- **`sessionStorage` indisponível** (navegação privada restrita, configuração do navegador): a página MUST continuar funcionando normalmente, só sem persistir os filtros entre a ida e a volta — mesmo padrão de falha silenciosa já usado em `ThemeContext.tsx` para `localStorage`.
- **Admin faz logout e outra pessoa loga no mesmo navegador/aba, sem fechar o navegador**: os filtros persistidos podem continuar no `sessionStorage` dessa aba — aceitável, já que o Requisito 2 diz apenas que a persistência "não precisa" sobreviver ao logout (não exige que seja limpa nele).
- **Admin abre a página de Indicadores em duas abas diferentes do mesmo navegador, com filtros diferentes em cada uma**: fora do controle desta feature — `sessionStorage` é compartilhado entre abas da mesma origem; a aba aberta por último "vence" ao salvar. Não é um cenário de uso típico desta tela, e não é agravado por esta feature (o problema já existiria hoje de qualquer forma, sem persistência, abas diferentes já mostram a mesma tela recarregada do zero cada uma).
- **Filtros aplicados antes desta feature existir** (nenhum valor salvo ainda): a primeira visita após a implementação simplesmente não encontra nada salvo e usa os valores padrão — comportamento idêntico ao de hoje.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Ao voltar da tela de detalhe de Entradas/Saídas para a Visão de Indicadores (spec 045), o sistema MUST restaurar todos os filtros (período, turma, matéria, aluno, semestre, Entradas/Saídas) exatamente como estavam antes de navegar para o detalhe.
- **FR-002**: O sistema MUST restaurar também qual aba estava ativa ("Visão Financeiro" ou "Visão de Indicadores") ao voltar — não apenas os filtros.
- **FR-003**: Os dados exibidos (tabela de lançamentos, KPIs, fluxo de caixa, demais indicadores) MUST corresponder aos filtros restaurados, sem exigir nenhuma ação adicional do usuário.
- **FR-004**: A persistência MUST valer somente durante a sessão de uso atual (ida e volta dentro da mesma aba/janela aberta) — o sistema MUST NOT persistir os filtros de forma que sobrevivam ao fechamento do navegador ou a um novo login.
- **FR-005**: O sistema MUST exibir um botão "Limpar filtros" na área de filtros da Visão de Indicadores, que restaura todos os filtros (incluindo o filtro Entradas/Saídas) ao valor padrão e recarrega os dados sem nenhum filtro aplicado.
- **FR-006**: O botão "Limpar filtros" MUST só aparecer (ou ficar ativo) quando pelo menos um filtro estiver diferente do seu valor padrão — mesmo comportamento já aplicado hoje aos demais filtros da página.
- **FR-007**: O sistema MUST NOT alterar o que a tela de detalhe de Entradas/Saídas exibe, nem os filtros que ela recebe — o contrato estabelecido pela spec 045 permanece o mesmo.
- **FR-008**: O sistema MUST NOT alterar nenhuma regra de cálculo dos indicadores, do relatório financeiro, ou da tabela de lançamentos — e MUST NOT introduzir no frontend nenhum recálculo que hoje só existe no backend.
- **FR-009**: O sistema MUST NOT exigir nenhuma mudança de API/backend — a persistência e a restauração dos filtros MUST ser resolvidas inteiramente no frontend.
- **FR-010**: O sistema MUST NOT introduzir nenhuma cor ou componente visual novo — o botão "Limpar filtros" MUST reaproveitar o mesmo elemento/classe já usado hoje na mesma página (e em outras telas do sistema) para essa mesma finalidade.
- **FR-011**: O sistema MUST NOT persistir os filtros desta página de forma que afete ou seja afetada pelos filtros de nenhuma outra página do sistema (cada tela com filtros persistidos, se houver, é independente).

### Key Entities

- **Filtros da Visão de Indicadores** (estado de UI existente, sem nenhuma entidade de domínio nova): período (início/fim), semestre, turma, matéria, aluno, forma de pagamento (Visão Financeiro), filtro de tipo Entradas/Saídas (Visão de Indicadores, spec 022/045), e a aba ativa — todos já existentes como estado local da página; esta feature só adiciona a persistência deles entre a navegação de ida e volta ao detalhe.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das vezes que o usuário clica num lançamento e volta, todos os filtros (incluindo a aba ativa) estão exatamente como estavam antes, sem nenhuma ação manual de refiltragem.
- **SC-002**: 100% das vezes que o usuário clica em "Limpar filtros", todos os filtros (incluindo Entradas/Saídas) voltam ao padrão numa única ação.
- **SC-003**: 0 regressões no que a tela de detalhe de Entradas/Saídas exibe ou recebe (spec 045 inalterada).
- **SC-004**: 0 mudanças de backend e 0 cálculos duplicados no frontend (confirmável por inspeção do código após a implementação).
- **SC-005**: Fechar o navegador (ou abrir a página pela primeira vez numa nova aba/sessão) e abrir a Visão de Indicadores de novo mostra os filtros no estado padrão, não os últimos usados antes de fechar.

## Assumptions

- A aba ativa ("Visão Financeiro"/"Visão de Indicadores") é tratada como parte do que precisa ser restaurado, mesmo o pedido original falando só em "filtros" — é o mesmo problema de raiz (estado local perdido ao remontar a página) e deixá-la de fora contrariaria a experiência pedida pelo Requisito 1.
- O mecanismo de persistência é `sessionStorage` do navegador — não reflexo na URL (query params), nem `localStorage` (teria vida mais longa que o Requisito 2 pede), nem um contexto React compartilhado (não sobreviveria ao unmount completo da página causado pela navegação para o detalhe).
- O botão "Limpar filtros" já existe hoje (`filter-clear`) e cobre 6 dos filtros da página; esta feature só estende o que ele já limpa para incluir também o filtro Entradas/Saídas — não cria um botão novo.
- Falha ao acessar `sessionStorage` (navegação privada restrita) é tratada silenciosamente — a página funciona normalmente, só sem persistir os filtros, mesmo padrão já aceito pelo `ThemeContext.tsx` para `localStorage`.
- Fora de escopo: persistir filtros de qualquer outra página do sistema; persistir entre sessões/dispositivos/logout; qualquer mudança na tela de detalhe de Entradas/Saídas; qualquer mudança de backend ou de regra de cálculo.
