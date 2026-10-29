# Implementation Plan: Detalhes da Aula em Pop-up na Agenda Semanal da Home

**Branch**: `061-home-detalhes-aula` | **Date**: 2026-10-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/061-home-detalhes-aula/spec.md`

## Summary

Só frontend. Um componente novo e reutilizável, `AulaDetalhesPopup` (`frontend/components/aulas/`), recebe a aula já carregada e mostra os detalhes e dois botões ("Abrir aula" e "Fechar"). Na Home, cada aula da agenda semanal deixa de ser um `<Link>` e vira um `<button>`; o clique guarda a aula escolhida num estado local e abre o pop-up. Sem endpoint, sem banco e sem regra de negócio nova.

## Technical Context

**Language/Version**: TypeScript / React 19 / Next.js 16

**Primary Dependencies**: nenhuma nova (reaproveita `.modal`, `StatusBadge`, `fmtData`, `fmtHora` e o `useRouter`)

**Storage**: N/A

**Testing**: o projeto não tem suíte de testes de frontend (vale desde a spec 057). A verificação é por `tsc`, `eslint`, `npm run build`, `npm run verificar-contraste`, `grep` e leitura do diff. A conferência visual é manual, no navegador.

**Project Type**: web application (só `frontend/`)

**Constraints**: usar só tokens de tema (spec 057); sem rolagem horizontal; acessível por teclado

## Constitution Check

- **I. Exclusão lógica**: não se aplica (sem dados).
- **II. Regra de negócio no backend**: o pop-up só exibe o que a API já devolve (status, presença, reposição). Não calcula nada nem decide regra.
- **III. Nada configurável sem efeito**: não cria configuração.
- **IV. Segredos**: não se aplica.
- **V. Documentação retroativa**: nenhuma spec anterior descreve o clique na agenda semanal; não há nota a acrescentar.

**Resultado**: PASS.

## Project Structure

```text
frontend/components/aulas/AulaDetalhesPopup.tsx   # NOVO: pop-up de detalhes (somente leitura)
frontend/app/(app)/dashboard/page.tsx             # aula da agenda vira botão que abre o pop-up
frontend/styles/dashboard.css                     # reset de botão em .week-event; estilos do pop-up (tokens)
specs/061-home-detalhes-aula/                     # documentos desta feature
```

**Structure Decision**: o pop-up fica em `components/aulas/` (e não dentro da Home) para a Agenda e outras telas poderem reutilizá-lo (FR-009).

## Complexity Tracking

Sem violações.
