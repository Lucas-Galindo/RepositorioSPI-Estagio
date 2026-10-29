# Quickstart: validar os detalhes da aula na Home (spec 061)

## Verificação automática (em `frontend/`)

| # | Comando | Esperado |
|---|---|---|
| 1 | `npx tsc --noEmit`, `npx eslint` nos arquivos tocados e `npm run build` | limpos |
| 2 | `npm run verificar-contraste` | 0 pares abaixo de 4.5:1 |
| 3 | `grep -nE "#[0-9a-fA-F]{3,8}\b" styles/dashboard.css components/aulas/AulaDetalhesPopup.tsx` | nenhuma cor literal |
| 4 | Ler o diff do pop-up | só tipos de `lib/api` (sem `fetch`/chamadas) e sem botão de editar, cancelar, excluir ou presença |

## Navegador (manual)

| # | Passo | Esperado |
|---|---|---|
| 1 | Home → clicar numa aula da agenda semanal | Pop-up abre; a Home não navega |
| 2 | Conferir os dados | Matéria, data, horário, turma (ou "Atendimento individual"), status e descrição (se houver) |
| 3 | Aula realizada | Alunos com "Presente"/"Faltou"; aula agendada: só os nomes |
| 4 | Aula de reposição | Aluno marcado como "Reposição" |
| 5 | Fechar por "Fechar", clique fora e Esc | Fecha e o foco volta para a aula clicada |
| 6 | Clicar em "Abrir aula" | Abre `/aulas/{id}` da mesma aula |
| 7 | Teclado: Tab até uma aula, Enter | Abre o pop-up |
| 8 | Modo escuro e largura de 360px | Legível, sem rolagem horizontal |
