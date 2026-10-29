# Research: Detalhes da Aula em Pop-up na Home (spec 061)

## R1 — Pop-up ou nova guia

**Decision**: pop-up. Comparação completa na [spec](./spec.md), seção "Decisão sobre o estudo pedido no card".

**Rationale**: o pedido é dar uma olhada sem perder a Home. O pop-up faz isso em 1 clique e não depende de rede, porque os dados já estão carregados. As ações continuam na página completa, que o pop-up abre pelo botão "Abrir aula".

## R2 — Não reaproveitar o pop-up da Agenda

**Decision**: criar um pop-up de detalhes novo, em vez de reaproveitar o da Agenda.

**Rationale**: o pop-up da Agenda (`modal-aula-popup`, em `agenda/page.tsx`) envolve o `AulaForm`, um formulário de criar e editar aula. Usá-lo na Home colocaria um formulário de edição onde o pedido é ver detalhes, e contraria o FR-006. O card sugeria o reaproveitamento por engano. O componente novo é pequeno e reutilizável.

## R3 — Aula da agenda como botão, não como link

**Decision**: trocar o `<Link>` por `<button type="button">`, com o mesmo visual (`.week-event`).

**Rationale**: o clique agora abre um pop-up (não navega), então o elemento correto é um botão, que também é focável e acionável por teclado (Enter/Espaço). O custo é perder "abrir em nova guia" com Ctrl/clique do meio na própria agenda; o botão "Abrir aula" do pop-up cobre isso (documentado como Edge Case na spec).

## R4 — Dados e acessibilidade

**Decision**:
- **Dados**: o pop-up recebe o objeto `Aula` que a Home já tem em memória (`listarAulas`). Nenhuma chamada nova (FR-007).
- **Teclado**: Esc fecha (listener enquanto aberto); o foco inicial vai para "Abrir aula"; ao fechar, o foco volta para o elemento que abriu o pop-up.
- **Semântica**: `role="dialog"`, `aria-modal="true"` e `aria-labelledby` apontando para o título.
- **Presença**: lista de alunos com `StatusPill`/classes `status-ativo` e `status-atrasado`, como a página da aula. Em aula realizada mostra "Presente"/"Faltou" (ou "Pacote esgotado" quando houver o motivo); em aula agendada, só os nomes. Reposição ganha a marca "Reposição".
- **Altura**: a lista de alunos rola dentro do pop-up (`max-height`), sem passar a altura da tela.

**Rationale**: reaproveita o que a página da aula já faz; o pop-up só deixa o resumo visível.

## R5 — Estilo

**Decision**: classe `.modal` existente, com variante `.aula-detalhes-popup` (largura de 480px, como o pop-up de aula das notificações) e o mesmo bloco de detalhes (`dl`) das notificações. O botão da agenda ganha o reset de `<button>` (largura total, alinhado à esquerda, sem borda extra). Só tokens de tema.
