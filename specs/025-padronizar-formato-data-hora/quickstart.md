# Quickstart: Validar Padronização de Data/Hora Brasileira

Guia de validação manual (não há framework de teste automatizado no frontend — ver plan.md
Technical Context). Rodar após a implementação (tasks.md), antes de considerar a feature
concluída.

## Pré-requisitos

- `frontend` rodando localmente (`npm run dev` dentro de `frontend/`).
- Login como Professora/Admin com dados de exemplo (turmas, aulas, contas a pagar/receber,
  lembretes) já cadastrados — reaproveitar dados de desenvolvimento existentes.
- Dois navegadores (ou um navegador + uma janela anônima) configurados com idiomas diferentes,
  para o Cenário 5 (ex.: Chrome em `en-US`, Firefox/Edge em `pt-BR`).

## Cenário 1 — Datas em dd/mm/aaaa em toda tela de exibição

1. Abrir, em sequência: Dashboard, Agenda, Turmas, Alunos (detalhe de um aluno), Financeiro
   (Contas a Pagar, Contas a Receber, Pagamentos), todos os Relatórios (Financeiro, Agenda,
   Histórico do Aluno, Pendências, Alunos, Turmas, Pagamentos).
2. Em cada tela, conferir visualmente: toda data exibida (cards, tabelas, listas) está no
   formato dd/mm/aaaa (ex.: "17/09/2026").
3. **Esperado**: nenhuma ocorrência de mm/dd/aaaa em nenhuma tela (FR-001, SC-001).

## Cenário 2 — Horários em 24h em toda tela de exibição

1. Nas mesmas telas do Cenário 1 (com foco em Agenda, cards de aula, Relatório de Agenda),
   conferir todo horário exibido.
2. **Esperado**: todo horário aparece como HH:mm (ex.: "16:00"), nunca com "AM"/"PM" (FR-002,
   SC-002). Onde data e hora aparecem juntas, a ordem é sempre "dd/mm/aaaa HH:mm" (FR-003).

## Cenário 3 — Novos componentes de data/hora em formulários

1. Abrir cada formulário que tem campo de data/hora: cadastro/edição de Aula (`AulaForm`),
   Contas a Pagar (novo/editar), Contas a Receber (novo/editar), e os filtros de período em
   Financeiro, Lembretes e nos Relatórios.
2. Em cada campo, digitar e/ou selecionar uma data/hora usando o novo componente.
3. **Esperado**: o campo sempre exibe e aceita dd/mm/aaaa (datas) ou HH:mm (horários) — não abre
   mais o seletor nativo do navegador (calendário/relógio do SO) (FR-008).
4. Salvar o formulário e confirmar que o valor persistido/exibido depois de recarregar a tela
   está correto (mesma data/hora escolhida) — confirma que o contrato de valor não mudou
   (FR-007).

## Cenário 4 — Nenhuma mudança de dado, cálculo ou comportamento

1. Antes da implementação, anotar 3–5 valores de data/hora exibidos em telas variadas (Dashboard,
   um card de aula, uma conta a pagar).
2. Depois da implementação, comparar os mesmos registros.
3. **Esperado**: os valores em si (dia, mês, ano, hora) são idênticos — só a formatação/exibição
   mudou onde havia divergência; nenhum cálculo, ordenação ou dado enviado à API mudou (FR-007,
   SC-004).

## Cenário 5 — Independência de idioma/SO do navegador

1. Configurar um navegador com idioma `en-US` e outro com `pt-BR` (ou trocar o idioma do SO/
   navegador entre duas execuções).
2. Abrir o mesmo formulário com campo de data/hora (ex.: cadastro de Aula) nos dois navegadores.
3. **Esperado**: em ambos, o componente exibe e aceita dd/mm/aaaa e 24h de forma idêntica — sem
   nenhuma diferença de formato causada pelo idioma do navegador (FR-008, SC-005).

## Cenário 6 — Nenhuma tela nova reimplementa formatação própria

1. Revisar o diff da implementação (`git diff`) e confirmar que toda formatação de data/hora
   nova ou alterada usa `frontend/lib/format.ts` (ou os novos `DateInput`/`TimeInput`), sem
   nenhuma chamada direta a `toLocaleDateString`/`toLocaleTimeString`/parsing manual novo fora
   desses pontos centrais.
2. **Esperado**: única fonte de formatação confirmada (FR-004, SC-003).
