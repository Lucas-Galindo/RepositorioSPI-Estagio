# Research: divisão da aba Lembretes (spec 066)

## R1 — Onde fica a divisão

**Decisão**: seções dentro da mesma rota `/lembretes`, em `page.tsx`. Não se criam abas internas, rotas nem componentes novos.

**Por quê**: o pedido é "criar uma divisão na aba"; as duas partes já convivem na página. Abas internas esconderiam uma das partes (mais cliques) e mudariam a navegação; um componente novo contraria o pedido de mexer o mínimo. O `LembretePagamentoSection` já é um bloco autocontido.

**Alternativas descartadas**: (a) abas "Aulas | Pagamentos" — mais intrusivo, esconde conteúdo; (b) mover a parte de aula para um componente novo — arquivo novo sem necessidade; (c) renomear só o bloco de pagamento — não resolve o título que falta na parte de aula.

## R2 — Estilo

**Decisão**: usar apenas o que já existe em `dashboard.css`: `.section-title` (13.5px, 700) e `.section-sub` (11.5px, cor `--c-text-muted`), o componente `Icon` (ícones já existentes como `cal`, `money`) e a divisória `borderTop: "1px solid var(--c-bg)"` com margens, o mesmo recurso já usado em `CadastrarProfessoraForm.tsx`. Nenhuma classe CSS nova, nenhuma cor nova.

**Contraste**: `section-sub` já é usado em outras telas e passa na verificação do projeto (`npm run verificar-contraste`); o título usa a cor de texto padrão. A tarefa de validação confirma em modo claro e escuro.

## R3 — Texto

- Aula: **"Lembretes de aula"** — "E-mails enviados aos alunos e responsáveis antes de cada aula da turma. Os indicadores e filtros abaixo valem só para estes lembretes."
- Pagamento: **"Lembretes de pagamento"** — "E-mail semanal para alunos e responsáveis com os pagamentos vencidos sem baixa."

A segunda frase de aula deixa explícito o escopo dos filtros (o problema que a separação resolve). O texto do bloco "Pagamento pendente" não é alterado.

## R4 — O que não muda

Estados, `useEffect`, filtros, KPIs, `handleExcluir`, `ConfirmModal`, rotas de edição e criação, `LembretePagamentoSection`. O `ConfirmModal` continua fora das seções (é um modal).

## R5 — Validação

Sem suíte de testes de frontend, a validação é: `tsc`, `eslint`, contraste, `next build` e Chrome headless com captura da tela `/lembretes` (claro e escuro) mais comparação por hash das demais telas (as 34 telas da validação das specs 063 e 065). Esperado: só `/lembretes` difere.
