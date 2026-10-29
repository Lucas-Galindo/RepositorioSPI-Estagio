---

description: "Task list for Detalhes da Aula em Pop-up na Agenda Semanal da Home"
---

# Tasks: Detalhes da Aula em Pop-up na Agenda Semanal da Home

**Input**: Design documents from `specs/061-home-detalhes-aula/`

**Tests**: o projeto não tem suíte de testes de frontend. A verificação é por `tsc`, `eslint`, build, `verificar-contraste` e leitura do diff. Os dois "MUST NOT" (FR-006 sem ações de edição, FR-007 sem consulta nova) têm tarefa de verificação por leitura do diff, como pede o `CLAUDE.md`.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Pop-up reutilizável (US1 + US2, P1) 🎯 MVP

- [X] T001 [US1] Criar `frontend/components/aulas/AulaDetalhesPopup.tsx` conforme [contracts/aula-detalhes-popup.md](./contracts/aula-detalhes-popup.md):
  - props `aula` e `onFechar`;
  - título com a matéria e o status em `StatusBadge`;
  - detalhes de data e horário, turma ou "Atendimento individual", alunos (RA; "Presente"/"Faltou"/"Pacote esgotado" só em aula realizada; marca "Reposição") e descrição quando existir;
  - rodapé com "Fechar" e "Abrir aula" (`router.push("/aulas/{id}")`);
  - `role="dialog"`, `aria-modal`, `aria-labelledby`;
  - Esc fecha, foco inicial em "Abrir aula", foco devolvido ao elemento que abriu, clique fora fecha;
  - lista de alunos com rolagem interna (`max-height`).
  Comentar no código as decisões de teclado e foco.
- [X] T002 [P] [US1] `frontend/styles/dashboard.css`: variante `.aula-detalhes-popup` (largura de 480px, `max-width` em % para o celular), lista de alunos com rolagem e reaproveitamento do bloco `.notif-detalhes`; reset de `<button>` em `.week-event` (largura total, texto à esquerda, sem borda extra, `font: inherit`). Só tokens de tema.

## Phase 2: Home

- [X] T003 [US1] `frontend/app/(app)/dashboard/page.tsx`: cada aula da agenda semanal vira `<button type="button" className="week-event ...">` que guarda a aula em `aulaAberta`; renderizar `<AulaDetalhesPopup>` quando houver aula aberta e zerar o estado em `onFechar`. Remover o `Link` que ficar sem uso (o botão "Nova aula" ainda usa `Link`). A aula usada no pop-up é a que já está em `aulas` (nenhuma chamada nova).

## Phase 3: Verificação

- [X] T004 FR-006 ("MUST NOT" ações de edição): ler o diff de `AulaDetalhesPopup.tsx` e confirmar que não há botão nem chamada de editar, cancelar, excluir ou registrar presença; os únicos controles são "Fechar" e "Abrir aula".
- [X] T005 FR-007 ("MUST NOT" consulta nova): ler o diff de `AulaDetalhesPopup.tsx` e de `dashboard/page.tsx` e confirmar que o pop-up importa de `lib/api` apenas tipos (`import type`), não usa `fetch` e não tem `useEffect` de carga; a Home não ganhou chamada nova.
- [X] T006 FR-008: `grep -nE "#[0-9a-fA-F]{3,8}\b"` em `styles/dashboard.css` e no componente, sem cor literal; `npm run verificar-contraste` passa.
- [X] T007 `git status --porcelain -- frontend src database tests`: os arquivos novos são só `frontend/components/aulas/AulaDetalhesPopup.tsx` e os documentos da spec; nenhuma mudança de backend ou banco.
- [X] T008 `npx tsc --noEmit`, `npx eslint` nos arquivos tocados e `npm run build`.
- [ ] T009 (parte manual) Percorrer o [quickstart.md](./quickstart.md) no navegador: aula realizada, agendada, reposição, Esc, foco, teclado, modo escuro e 360px.

## Dependencies & Execution Order

T001 e T002 podem andar juntos; T003 depende de T001; a Fase 3 vem por último.
