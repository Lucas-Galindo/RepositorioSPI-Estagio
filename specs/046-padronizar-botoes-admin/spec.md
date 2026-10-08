# Feature Specification: Padronizar Botões da Zona de Risco

**Feature Branch**: `046-padronizar-botoes-admin`

**Created**: 2026-10-06

**Status**: Draft

**Input**: User description: "Na 'Zona de risco' da tela do Admin (spec 044), os botões 'Excluir Professor' e 'Cancelar' usam uma cor fora do padrão do sistema. Padronizar reaproveitando as classes de botão que já existem (por exemplo as de ação destrutiva e secundária usadas em 'Cancelar aula'/'Excluir' na tela de detalhe da Aula), sem criar cor nova. Antes de planejar, identificar qual classe e cor estão em uso hoje e qual é o equivalente padrão. Só visual, sem mudar comportamento."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar) — **o achado real é diferente do que o pedido original presumiu**:

- **"Excluir Professor" e os outros dois botões destrutivos da Zona de Risco já usam a classe padrão correta**: `frontend/components/admin/CadastrarProfessoraForm.tsx` (linhas 359, 377, 404) usa `className="btn btn-danger"` nos três botões de ação destrutiva ("Excluir Professor", "Entendi, enviar código de confirmação", "Confirmar exclusão") — exatamente a mesma classe `btn-danger` usada em `frontend/app/(app)/aulas/[id]/page.tsx` (linhas 118/123) para "Cancelar aula"/"Excluir". A cor (`var(--c-danger)`/`var(--c-danger-tint)`, definida em `frontend/styles/tokens.css`) é a mesma em ambas as telas — não há nenhuma cor nova nem fora do padrão nesses três botões. A única diferença é que a tela de Aula também usa o modificador de tamanho `btn-sm`, que a Zona de Risco não usa — mas isso é tamanho, não cor, e a própria Zona de Risco já é internamente consistente nesse ponto ("Reativar Professor", linha 346, também usa `btn btn-primary` sem `btn-sm`).
- **O problema real está nos botões "Cancelar"**: as duas ocorrências de "Cancelar" na Zona de Risco (linhas 380 e 409) usam `className="btn"` **sem nenhuma classe de variante** (nem `btn-ghost`, nem `btn-danger`, nem `btn-primary`). A classe base `.btn` (`frontend/styles/dashboard.css`) não define `background` nem `box-shadow` — só cursor, padding, borda-arredondada e tipografia — então, sem uma variante, o botão renderiza com a aparência padrão do navegador para `<button>` (cinza do sistema operacional), em vez de qualquer cor do design system do SPI. **Esse é o botão realmente "fora do padrão"** citado no pedido.
- **O equivalente padrão para "Cancelar"/ação secundária de dispensar já existe e está em uso em todo o sistema**: `frontend/components/shared/ConfirmModal.tsx` (componente reutilizável de confirmação, usado em exclusões/cancelamentos em várias telas) usa `className="btn btn-ghost btn-sm"` para o botão "Cancelar"; o mesmo padrão (`btn btn-ghost`) aparece no botão "Fechar" da Agenda (`frontend/app/(app)/agenda/page.tsx`, linha 324). `btn-ghost` é a classe secundária padrão do sistema — fundo neutro (`var(--c-bg)`), sombra sutil (`var(--shadow-out-sm)`), sem a conotação de perigo do `btn-danger` nem a conotação de ação principal do `btn-primary`.
- **Conclusão da investigação**: esta feature precisa trocar `className="btn"` por `className="btn btn-ghost"` nos dois botões "Cancelar" da Zona de Risco. Os três botões destrutivos ("Excluir Professor" e os dois que o pedido citou junto) **já estão corretos** e **MUST NOT** ser alterados — alterá-los introduziria uma mudança sem necessidade real (e o próprio pedido original pede para não criar cor nova, o que reforça não tocar no que já está certo).

## Clarifications

_Nenhuma pendente — a investigação prévia já identificou com precisão qual classe está em uso hoje e qual é o equivalente padrão (ver nota acima), resolvendo a única pergunta real do pedido original ("qual classe/cor está em uso e qual é o padrão"). Não há ambiguidade de produto a esclarecer com `/speckit-clarify`._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Botões "Cancelar" da Zona de Risco usam a cor padrão do sistema (Priority: P1)

Como Admin usando a tela de gerenciamento da professora (Zona de Risco, spec 044), eu quero que os botões "Cancelar" tenham a mesma aparência visual que o botão "Cancelar" já usado em outras confirmações do sistema (ex.: cancelar uma aula), para que a tela pareça parte do mesmo sistema, não um elemento desalinhado.

**Why this priority**: é o único problema visual real encontrado pela investigação — sem essa correção, a tela continua exibindo um botão com a cor padrão do navegador, destoando de todo o resto do sistema.

**Independent Test**: Abrir a tela de gerenciamento da professora, clicar em "Excluir Professor" para chegar à etapa do aviso (onde aparece o botão "Cancelar"), e comparar visualmente a cor/aparência desse botão com a do botão "Cancelar" de uma confirmação de exclusão em outra tela do sistema (ex.: excluir uma Aula) — devem ser visualmente equivalentes.

**Acceptance Scenarios**:

1. **Given** a etapa de aviso da exclusão (depois de clicar em "Excluir Professor"), **When** o Admin observa o botão "Cancelar", **Then** ele tem a mesma classe/aparência visual (fundo, sombra, cor de texto) do padrão secundário já usado em outras confirmações do sistema — não a aparência padrão de botão do navegador.
2. **Given** a etapa de inserir o código de confirmação, **When** o Admin observa o botão "Cancelar" dessa etapa, **Then** ele tem a mesma aparência visual do cenário 1 — ambos os "Cancelar" da Zona de Risco ficam visualmente idênticos entre si.
3. **Given** qualquer uma das duas telas de "Cancelar" corrigidas, **When** o Admin clica no botão, **Then** o comportamento é exatamente o mesmo de antes da correção (volta para a etapa anterior, sem enviar nenhuma requisição) — nenhuma mudança de comportamento, só de aparência.

---

### Edge Cases

- **Os três botões destrutivos da Zona de Risco** ("Excluir Professor", "Entendi, enviar código de confirmação", "Confirmar exclusão") **já usam a cor/classe correta** (`btn-danger`, confirmado na investigação prévia) — esta feature MUST NOT alterá-los. Se uma implementação futura decidir que eles também precisam de ajuste (ex.: adicionar `btn-sm` por consistência de tamanho com a tela de Aula), isso é uma decisão nova, fora do escopo desta feature, que trata apenas da cor incorreta já identificada.
- **Tamanho do botão** (`btn-sm` vs. tamanho padrão): a tela de Aula usa `btn-sm` em seus botões de confirmação; a Zona de Risco não usa `btn-sm` em nenhum dos seus botões (nem nos corretos, nem nos a corrigir). Esta feature não altera tamanho — só a classe de cor/variante que faltava nos botões "Cancelar", preservando o tamanho padrão já usado em toda a Zona de Risco.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exibir os dois botões "Cancelar" da Zona de Risco (etapa de aviso e etapa de código) com a mesma classe/variante visual secundária já usada em outras confirmações do sistema (ex.: botão "Cancelar" do componente de confirmação reutilizável, botão "Fechar" da Agenda).
- **FR-002**: O sistema MUST NOT introduzir nenhuma cor, classe CSS, ou variante de botão nova — a correção MUST reaproveitar exclusivamente uma classe de botão já existente no sistema.
- **FR-003**: O sistema MUST NOT alterar a classe, cor, ou aparência dos três botões de ação destrutiva da Zona de Risco ("Excluir Professor", "Entendi, enviar código de confirmação", "Confirmar exclusão") — eles já usam a classe destrutiva padrão correta e não fazem parte do problema a corrigir.
- **FR-004**: O sistema MUST NOT alterar o comportamento de nenhum botão da Zona de Risco (nem os corrigidos, nem os já corretos) — nenhuma mudança em qual ação cada botão dispara, em validação, ou em navegação entre etapas.
- **FR-005**: O sistema MUST NOT alterar nenhuma outra tela ou componente do sistema além da Zona de Risco — em particular, MUST NOT alterar a definição da classe `.btn` base nem de nenhuma classe de variante existente (`btn-ghost`, `btn-danger`, `btn-primary`), já que isso afetaria outras telas que as reaproveitam.

### Key Entities

Não aplicável — feature puramente visual, sem entidade de dados envolvida.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos botões "Cancelar" da Zona de Risco (2 ocorrências) passam a usar a classe visual secundária padrão do sistema, visualmente indistinguível do botão "Cancelar" de outras confirmações (ex.: cancelar uma Aula).
- **SC-002**: 0 mudanças de comportamento em qualquer botão da Zona de Risco, confirmável comparando o comportamento antes/depois desta feature.
- **SC-003**: 0 classes ou cores CSS novas introduzidas — confirmável conferindo que nenhuma regra nova foi adicionada a nenhum arquivo `.css` do projeto.
- **SC-004**: 0 regressões visuais em qualquer outra tela do sistema que reaproveita as classes `.btn`, `.btn-ghost`, `.btn-danger`, ou `.btn-primary` (nenhuma delas é redefinida por esta feature).

## Assumptions

- A classe secundária padrão a reaproveitar para "Cancelar" é `btn-ghost`, confirmada em uso real no componente `ConfirmModal.tsx` e na página de Agenda — não uma classe nova.
- Os três botões destrutivos da Zona de Risco já estão corretos (`btn-danger`) e ficam fora do escopo desta feature, apesar de o pedido original os ter citado junto com "Cancelar" — a investigação prévia prevalece sobre a suposição inicial do pedido.
- O tamanho (`btn-sm` ou não) não é alterado por esta feature — só a classe de cor/variante que faltava.
- Fora de escopo: qualquer mudança na classe `.btn` base, em qualquer outra classe de variante compartilhada, em qualquer outra tela do sistema, ou em qualquer comportamento (clique, navegação, requisição) dos botões da Zona de Risco.
