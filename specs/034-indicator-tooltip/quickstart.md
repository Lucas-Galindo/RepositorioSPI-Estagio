# Quickstart: Validar o tooltip explicativo dos cards de indicador

## Pré-requisitos

- `npm install` já executado em `frontend/` (sem dependência nova adicionada por esta feature).
- Frontend rodando localmente: `npm run dev` em `frontend/` (Next.js dev server).
- Backend rodando localmente (para os dados reais dos cards aparecerem) e uma credencial válida
  para autenticar — pedir uma credencial temporária no momento do teste, conforme a regra de
  trabalho combinada; não criar/usar credenciais de teste persistentes.

## Validação manual (não há suíte automatizada no frontend — ver [research.md — R4](./research.md#r4--verificação-de-cobertura-de-teste))

Repetir os passos abaixo em cada uma das 4 telas listadas em
[data-model.md](./data-model.md#mapeamento-indicador--tela--arquivo--texto):

1. **Dashboard** (`/dashboard`): passar o mouse sobre o ícone de informação do card "Recebido
   este mês".
   - **Esperado**: balão aparece com o texto definido em data-model.md, sem fórmula técnica.
2. **Financeiro — Visão Geral** (`/financeiro`): repetir para "Saldo realizado (mês)" e "Saldo
   previsto".
3. **Relatório Financeiro — aba Visão Financeiro** (`/relatorios/financeiro`): repetir para
   "Recebido", "Pago", "Saldo realizado", "Receita pendente", "Despesa pendente", "Saldo
   previsto".
4. **Relatório Financeiro — aba Indicadores**: repetir para "Inadimplência", "Prazo médio de
   atraso", e para o título da tabela "Fluxo de caixa (últimos 6 meses)".
5. **Relatório de Turmas** (`/relatorios/turmas`): repetir para "Ocupação média" — confirmar que
   o texto fala em "média de alunos por turma", não em taxa de comparecimento (ver
   Clarifications da spec).

## Verificação de acionamento por toque, teclado e leitor de tela (FR-002, FR-002a, FR-006)

6. Usando as ferramentas de emulação de dispositivo touch do navegador (DevTools), tocar em um
   ícone de informação e confirmar que o balão aparece; tocar fora dele e confirmar que
   desaparece.
7. Sem usar o mouse, pressionar Tab repetidamente até o foco chegar a um ícone de informação.
   - **Esperado**: o balão aparece ao focar e desaparece ao pressionar Tab novamente (ou
     Shift+Tab) para sair do ícone.
8. Inspecionar o HTML do ícone de informação (DevTools → Elements) e confirmar a presença de
   `aria-label` (ou `aria-describedby` apontando para o texto do balão) com o texto explicativo.

## Verificação de reutilização (FR-007, User Story 2)

9. Escolher um card sem tooltip (se algum ficar de fora do escopo original) e adicionar
   `<InfoTooltip text="..."/>` dentro do container de label existente — confirmar que nenhuma
   linha de CSS ou lógica de hover precisa ser escrita além dessa.

## Regressão

Como a mudança só adiciona um elemento dentro de containers de label já existentes (sem alterar
`display`/layout dos cards), abrir cada uma das 4 telas e confirmar visualmente que o layout dos
cards (posição de ícone de categoria, valor, delta) permanece igual ao anterior, só com o novo
ícone de informação visível ao lado do texto do label.
