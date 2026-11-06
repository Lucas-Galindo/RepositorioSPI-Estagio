# Quickstart: validar a spec 073

## Pré-requisitos

API local em `http://localhost:5099` e Next com `cmd /c "set NEXT_PUBLIC_API_URL=http://localhost:5099&& set NODE_OPTIONS=--max-old-space-size=1536&& npx next dev --webpack"` (conferir antes se já há API do usuário na 7002 e não encerrar processos que não subi); login só por `SPI_TESTE_SENHA`; `ignoreHTTPSErrors` se o front apontar para a 7002. Sem migração e sem dados novos (o banco local tem 4 turmas: 3 ativas e "Turma teste" inativa).

## 1. Estático

`cd frontend`: `npx tsc --noEmit`, `npx eslint` (0 problemas), `npm run build`.

## 2. Chrome (login real, claro, escuro e 360 px, capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Turmas sem filtro | seletor "Status — todos"; 4 turmas na mesma ordem de antes |
| 2 | "Ativo" | 3 turmas ativas; contagem "3 turma(s) encontrada(s)"; requisição com `ativo=true` |
| 3 | "Inativo" | só "Turma teste"; "1 turma(s)"; `ativo=false` |
| 4 | Status + busca por nome | os dois filtros juntos |
| 5 | "Limpar filtros" | aparece só com filtro; volta a todos e busca vazia |
| 6 | Sem resultado | "Nenhuma turma encontrada" |
| 7 | Clique no card | abre a turma |
| 8 | 360 px | sem rolagem horizontal da página |
| 9 | `git status --porcelain -- frontend src database` | só `turmas/page.tsx` modificado, nada novo |
