# Tasks: Aba Lembretes dividida em "Lembretes de aula" e "Lembretes de pagamento"

**Input**: `specs/066-lembretes-secoes/` (spec.md, plan.md, research.md, quickstart.md)
**Prerequisites**: nenhum (mudança só de apresentação, em um único arquivo de código).

**Formato**: `- [ ] TNNN [P] [USn] descrição com caminho`. Regras do `CLAUDE.md`: cada "MUST NOT" tem tarefa de verificação (`git status --porcelain` para "sem arquivo novo"; **leitura do diff** para "sem lógica nova").

**Segurança**: a validação só **lê** a tela; nenhum dado é criado ou alterado; nenhum e-mail sai (`Smtp__SmtpHost=smtp.invalido.localhost`); a senha de teste só por variável de ambiente (`SPI_TESTE_SENHA`), nunca em arquivo, relatório ou Trello; nada em produção.

## Phase 1: Linha de base

- [X] T001 Anotar a linha de base: `cd frontend && npx tsc --noEmit` e `npx eslint "app/(app)/lembretes/page.tsx"` limpos; `git status --porcelain` (o que já está sem commit). Subir a API (`--no-build`, porta 5099) e o frontend de produção (porta 3000, `experimental.cpus: 2` temporário se faltar memória, restaurando `next.config.ts`) e rodar as capturas "antes" das 34 telas (scripts do scratchpad da spec 065; se não existirem, recriar: Chrome headless, claro e escuro, hash do PNG).

**Checkpoint**: capturas "antes" guardadas.

## Phase 2: User Story 1 — Ver as duas mensagerias separadas e com título (P1)

**Meta**: a aba mostra "Lembretes de aula" e "Lembretes de pagamento" em seções distintas, cada uma com título e frase.
**Teste independente**: abrir `/lembretes` e conferir os dois títulos, a ordem e a divisória, em claro e escuro.

- [X] T002 [US1] Em `frontend/app/(app)/lembretes/page.tsx`, envolver os indicadores, os filtros, a linha de contagem/"Novo lembrete" e a tabela numa seção com o título **"Lembretes de aula"** (`<div className="section-title">` com `<Icon name="cal" size={15} />`) e a frase de research.md R3 (`<p className="section-sub">`). Envolver `<LembretePagamentoSection />` numa segunda seção com divisória (`borderTop: "1px solid var(--c-bg)"`, `marginTop` e `paddingTop`, como em `CadastrarProfessoraForm.tsx:328`), o título **"Lembretes de pagamento"** (ícone `money`) e a frase de research.md R3. Manter o `ConfirmModal` fora das seções. Remover o `marginTop: 24` duplicado só se ele estiver no `LembretePagamentoSection` e conflitar visualmente (preferir não alterar esse componente). **Não** mexer em estados, efeitos, filtros, handlers nem chamadas de API.
- [X] T003 [US1] **Verificação dos "MUST NOT" FR-005, FR-006, FR-007 e FR-008**: (a) ler o `git diff` de `page.tsx` linha a linha e confirmar que só entraram JSX de títulos/frases/wrappers e a divisória, sem nenhuma linha de lógica removida ou alterada (estados, `useEffect`, `useMemo`, filtros, `handleExcluir`, imports de API); (b) `git status --porcelain -- frontend` mostra **somente** ` M frontend/app/(app)/lembretes/page.tsx` e nenhum `??`; (c) `git status --porcelain -- src tests database` vazio e `git diff --stat -- src tests database` vazio; (d) `git diff -- frontend/components` vazio (o `LembretePagamentoSection` não muda).

**Checkpoint**: US1 implementada e conferida por leitura do diff.

## Phase 3: Validação final

- [X] T004 Rodar `npx tsc --noEmit`, `npx eslint "app/(app)/lembretes/page.tsx"`, `npm run verificar-contraste` (0 pares abaixo de 4.5:1) e `npm run build`; restaurar `next.config.ts` se alterado.
- [X] T005 Validar o `quickstart.md` no Chrome headless com login de teste: títulos e ordem, divisória, filtros só na parte de aula, ações de editar/excluir/criar presentes, lista vazia com o título mantido, modo claro e escuro; olhar as capturas. Rodar as capturas "depois" das 34 telas e comparar com as "antes": **só `/lembretes`** (claro e escuro) pode diferir (SC-002).
- [X] T006 Fechamento: escrever `docs/relatorios/2026-11-01-lembretes-secoes.md` (pedido, o que foi feito, validação, o que ficou fora: título por linha, assunto do e-mail, sino); hook `revisao-codigo` do Trello; atualizar a memória do projeto; parar os servidores iniciados por mim. **Não** commitar nem fazer push (não foi pedido para esta spec).

## Dependências

T001 → T002 → T003 → T004 → T005 → T006 (sequencial: um único arquivo, sem paralelismo útil).

## Estratégia

MVP = T002 (a história inteira). O restante é prova de que nada mais mudou.

## Cobertura dos requisitos

| FR | Tarefas | FR | Tarefas |
|---|---|---|---|
| FR-001 | T002, T005 | FR-005 | T003, T005 |
| FR-002 | T002, T005 | FR-006 | T003 |
| FR-003 | T002, T003 | FR-007 | T003 |
| FR-004 | T002, T004, T005 | FR-008 | T003 |
