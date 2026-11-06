# Quickstart: validar a spec 072

Decisões em [research.md](research.md).

## Pré-requisitos

- API local em `http://localhost:5099` (SMTP inválido) e Next com a variável definida dentro do comando: `cmd /c "set NEXT_PUBLIC_API_URL=http://localhost:5099&& set NODE_OPTIONS=--max-old-space-size=1536&& npx next dev --webpack"`; login só por `SPI_TESTE_SENHA`; aquecer as rotas antes do teste.
- Somente frontend: sem migração e **sem gravar dados** (os tipos extras são simulados interceptando `GET /api/alunos`).

## 1. Estático

`cd frontend`: `npx tsc --noEmit`, `npx eslint` (0 problemas), `npm run build` (com `.next/dev` removido).

## 2. Chrome (login real, claro, escuro e 360 px, com capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Alunos, dados reais | cabeçalho "Aluno, RA, Turma(s), Telefone, Valor da aula, Cobrança, Tipo, Status"; João da Silva: "Pacote" na coluna Tipo e "R$ 100,00 · Matematica basica 1" na Cobrança |
| 2 | Resposta simulada com todos os tipos | Mensalidade, Semestral, Anual, Avulsa e "Sem vínculo" na coluna Tipo; nenhuma repete o tipo na Cobrança |
| 3 | Pacote com "restam N" e pacote esgotado | pílula com "· restam N" (cor por estado) na coluna Tipo; "pacote esgotado" na Cobrança |
| 4 | Aluno com "+N" | "+N" ao lado da pílula, dica com todos os vínculos ao passar o mouse |
| 5 | Aluno sem selo | "—" nas duas colunas |
| 6 | Clique na linha | abre o aluno |
| 7 | Página da turma e detalhes da aula | selo igual ao de antes (modo "grupo") |
| 8 | 360 px | sem rolagem horizontal da página; tabela rola dentro do contêiner |
| 9 | `git status --porcelain -- frontend` | só `SeloCobranca.tsx` e `alunos/page.tsx` modificados, nenhum arquivo novo |
