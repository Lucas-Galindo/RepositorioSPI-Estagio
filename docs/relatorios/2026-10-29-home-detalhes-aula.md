# Relatório: Detalhes da aula em pop-up na agenda semanal da Home (spec 061)

**Card Trello**: "Home: abrir detalhes da aula ao clicar na agenda semanal" (Backlog → Design → A Fazer → Em andamento → Revisão)
**Spec**: `specs/061-home-detalhes-aula/` (specify, plan, tasks, analyze e implement em 2026-10-29)
**Status final**: ✅ Implementado. Falta a conferência visual no navegador (T009, parte manual).

## O que foi pedido

Ao clicar numa aula da agenda semanal da Home, mostrar os detalhes da forma mais prática, pop-up ou nova guia. O card pedia para estudar a melhor opção e implementar a escolhida.

## Decisão do estudo: pop-up

| Opção | Resultado |
|---|---|
| **Pop-up** (escolhida) | A professora vê o resumo sem sair da Home, em 1 clique, e fecha com 1 clique. Não precisa de nova consulta: os dados já estão carregados. |
| Nova guia | Acumula guias, vira um caminho confuso no celular e mostra a página inteira quando o pedido é só dar uma olhada. |

Para agir (presença, cancelar, editar, reposição), o pop-up tem o botão **"Abrir aula"**, que abre a página completa.

## Ponto que o card tinha errado

O card sugeria reaproveitar o pop-up da página Agenda. Esse pop-up, porém, é o **formulário de criar e editar aula** (`AulaForm`), e não uma visão de detalhes. Colocá-lo na Home poria um formulário de edição onde o pedido é só ver detalhes. Por isso criei um pop-up de detalhes novo, pequeno e reutilizável.

## O que foi feito

- **`frontend/components/aulas/AulaDetalhesPopup.tsx` (novo)**, somente leitura:
  - mostra a matéria e o status;
  - mostra data e horário, turma ou "Atendimento individual", e a descrição, quando existir;
  - lista os alunos com RA. Numa aula **realizada** mostra "Presente", "Faltou" ou "Pacote esgotado"; numa aula agendada, só os nomes; o aluno de reposição ganha a marca "Reposição";
  - tem os botões "Fechar" e "Abrir aula".
- **Teclado e acessibilidade**: Esc fecha, o foco inicial vai para "Abrir aula" e volta para a aula clicada ao fechar. O pop-up tem `role="dialog"`, `aria-modal` e `aria-labelledby`.
- **Home (`dashboard/page.tsx`)**: cada aula da agenda deixa de ser um link e vira um botão que abre o pop-up. O botão aceita Enter e Espaço.
- **Estilos**: reset de botão na aula da agenda e visual do pop-up (480px, lista de alunos com rolagem interna). Só tokens de tema.
- **Sem backend, sem banco, sem endpoint novo.**

## Efeito para você saber

Como a aula agora é um botão, **Ctrl+clique ou clique do meio** na própria agenda deixam de abrir a aula em nova guia. O caminho para a página completa é o botão "Abrir aula" do pop-up (registrado como caso de borda na spec).

## Como foi verificado

- `tsc`, `eslint` e `npm run build` limpos; `npm run verificar-contraste`: 48 combinações, nenhum par abaixo de 4.5:1.
- Nenhuma cor literal no CSS nem no componente.
- **Verificação dos "MUST NOT" (leitura do diff e `grep`)**:
  - FR-006: os únicos controles do pop-up são "Fechar" e "Abrir aula". O único trecho com "cancelar/editar" é um comentário.
  - FR-007: o pop-up importa de `lib/api` só tipos (`import type`), não usa `fetch` e a Home não ganhou chamada nova.
- `git status`: só `AulaDetalhesPopup.tsx` é arquivo novo no código; nada de backend ou banco foi alterado.
- O projeto não tem testes de frontend, então não há teste automatizado desta tela.

## Pendência

- **Navegador (T009)**: percorrer o `quickstart.md`: aula realizada, agendada e de reposição, Esc, foco, teclado, modo escuro e 360px.
