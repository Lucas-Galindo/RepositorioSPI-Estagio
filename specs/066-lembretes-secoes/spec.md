# Feature Specification: Aba Lembretes dividida em "Lembretes de aula" e "Lembretes de pagamento"

**Feature Branch**: `066-lembretes-secoes`

**Created**: 2026-11-01

**Status**: Draft

**Input**: Card do Trello "Melhorar título dos lembretes de aula" (prioridade 1 do backlog). Pedido do usuário em 2026-11-01: "É só criar uma divisão na aba de lembretes. A primeira parte se refere à mensageria de aulas e a segunda é a mensageria de outra parte. A de mensagens de aula está sem título evidente; quero somente que seja implementado."

## Contexto (estado atual, lido do código)

- A aba Lembretes (`frontend/app/(app)/lembretes/page.tsx`) mostra, de cima para baixo: quatro indicadores (Pendentes, Enviados, Falharam, Cancelados), a barra de filtros, a contagem com o botão "Novo lembrete" e a tabela de lembretes **de aula** por turma. **Nada diz que essa parte é "lembretes de aula"**: a página só tem o cabeçalho geral "Lembretes".
- Logo abaixo da tabela vem o bloco "Pagamento pendente" (`LembretePagamentoSection`, spec 060): regra do e-mail semanal e histórico de envios. Esse bloco **já tem título** ("Pagamento pendente") e uma frase explicativa.
- Os dois tipos de mensagem ficam colados na mesma página, sem separação clara; para quem lê, os indicadores e filtros do topo parecem valer para a página inteira, mas valem **só** para os lembretes de aula.
- Os lembretes de aula são e-mails enviados ao aluno e ao responsável antes de cada aula da turma (`LembreteDispatcherService`). O de pagamento é um e-mail semanal sobre pagamentos vencidos (`LembretePagamentoService`).

## Decisões de negócio (respondidas pelo usuário)

| Tema | Decisão |
|---|---|
| O que fazer | **Somente criar a divisão** da aba em duas partes. Nenhuma outra mudança. |
| Primeira parte | Mensageria de **aulas**, com título evidente (hoje não tem). |
| Segunda parte | Mensageria de **outra parte** (pagamento pendente), que já existe. |

## Decisões menores adotadas (a confirmar na aprovação)

1. Títulos das duas partes: **"Lembretes de aula"** e **"Lembretes de pagamento"**, na mesma ordem em que aparecem hoje (aula primeiro).
2. Cada parte tem uma frase curta explicando o que ela envia, no mesmo estilo da frase do bloco de pagamento.
3. O bloco existente "Pagamento pendente" **fica como está** dentro da segunda parte (não é reescrito).
4. Fica **fora** do pedido: título por linha da tabela, assunto do e-mail, itens do sino de notificações e qualquer regra de envio. O card original mencionava essas dúvidas, mas o usuário restringiu o pedido à divisão.
5. Se no futuro surgir um terceiro tipo de lembrete, ele ganha a própria parte com o mesmo padrão (sem trabalho agora).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver as duas mensagerias separadas e com título (Priority: P1)

A professora abre a aba Lembretes e enxerga duas partes distintas: primeiro "Lembretes de aula", com os indicadores, filtros e a tabela de hoje; depois "Lembretes de pagamento", com a regra e o histórico de hoje. Cada uma tem título e uma frase que explica o que ela é.

**Why this priority**: É o pedido inteiro; sem a separação, filtros e indicadores do topo parecem valer para o pagamento também.

**Independent Test**: Abrir a aba Lembretes e conferir os dois títulos, a ordem, a frase de cada parte e que os indicadores e filtros ficam dentro da primeira parte; repetir em modo claro e escuro.

**Acceptance Scenarios**:

1. **Given** a aba Lembretes, **When** ela abre, **Then** aparece o título "Lembretes de aula" acima dos indicadores, dos filtros, do botão "Novo lembrete" e da tabela.
2. **Given** a mesma página, **When** a professora rola até o fim, **Then** aparece o título "Lembretes de pagamento" acima do bloco de regra e histórico de pagamento pendente, claramente separado da parte de aula.
3. **Given** os filtros e indicadores, **When** a professora os usa, **Then** eles afetam só a parte de aula, como hoje, e agora isso fica evidente pela posição.
4. **Given** os modos claro e escuro, **When** a página é exibida, **Then** os títulos e a separação são legíveis (contraste mínimo de 4.5:1 para o texto).
5. **Given** uma lista de lembretes de aula vazia, **When** a página abre, **Then** o título da parte de aula continua visível junto da mensagem de lista vazia.

---

### Edge Cases

- **Erro ao carregar os lembretes de aula**: hoje a página inteira troca pelo aviso de erro; isso **não muda** neste pedido (a divisão só vale quando a página carrega).
- **Lista de aula vazia ou filtros sem resultado**: o título e o botão "Novo lembrete" permanecem; só a tabela mostra o estado vazio, como hoje.
- **Tela estreita**: o layout geral do app ainda não é responsivo (card próprio); a divisão não piora o que existe.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A aba Lembretes MUST mostrar duas partes distintas, nesta ordem: **"Lembretes de aula"** e **"Lembretes de pagamento"**, cada uma com título visível.
- **FR-002**: Cada parte MUST ter uma frase curta que explica o que ela envia e para quem.
- **FR-003**: A parte "Lembretes de aula" MUST conter tudo que hoje é de aula: indicadores, filtros, contagem, botão "Novo lembrete" e a tabela. A parte "Lembretes de pagamento" MUST conter o bloco atual de regra e histórico de pagamento pendente.
- **FR-004**: A separação MUST ser evidente à primeira vista (espaço e/ou linha divisória entre as partes) e legível nos modos claro e escuro, usando só as cores e classes já existentes do tema.
- **FR-005**: A mudança MUST NOT alterar o comportamento: filtros, indicadores, ordenação, ações (editar, excluir, criar), rotas e regra de envio dos lembretes continuam idênticos.
- **FR-006**: A mudança MUST NOT tocar no backend, no banco nem na API (nenhum arquivo em `src/`, `tests/` ou `database/`).
- **FR-007**: A mudança MUST NOT criar arquivo, componente ou tela nova; só altera arquivos que já existem.
- **FR-008**: A mudança MUST NOT duplicar nem recalcular regra de negócio no frontend; só reorganiza a apresentação.

### Key Entities

Nenhuma entidade nova nem alterada. A mudança é só de apresentação.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das aberturas da aba Lembretes mostram os dois títulos, na ordem definida, em modo claro e em modo escuro.
- **SC-002**: 0 diferenças de comportamento: os mesmos indicadores, filtros e ações funcionam como antes, e as capturas de todas as demais telas ficam idênticas às de antes.
- **SC-003**: 0 pares de cor abaixo de 4.5:1 na verificação de contraste do projeto.
- **SC-004**: 0 arquivos alterados fora de `frontend/app/(app)/lembretes/page.tsx` (código) e dos documentos da spec/relatório.

## Assumptions

- "A outra parte" da mensageria é o lembrete de **pagamento pendente**, único outro tipo de mensagem que a aba tem.
- A página continua sendo uma única rota (`/lembretes`); não há abas internas nem rotas novas.
- Os estilos existentes (`section-title`, `section-sub`, `mini-panel`, tokens `--c-*`) bastam para os títulos e a separação.

## Fora de escopo

- Título por linha da tabela de lembretes e assunto do e-mail de lembrete.
- Mudanças nas notificações do sino.
- Qualquer mudança de regra de envio, de backend, de banco ou de API.
- Layout responsivo para celular (card próprio do backlog).
