# Análise de consistência: Calendário próprio no padrão do sistema (spec 070)

**Data**: 2026-11-05
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `contracts/calendario.md`, `quickstart.md`, `tasks.md`, a constituição, as regras do `CLAUDE.md` e o código atual (`DateInput.tsx`, `lib/format.ts`, `lib/paleta.ts`, `Icon.tsx`, `verificar-contraste.mjs`, `dashboard.css`, as 4 telas de competência).
**Natureza**: análise sem alteração de código. Os achados A1 a A3 foram aplicados nos documentos.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais / critérios de sucesso | 17 (FR-001 a FR-017) / 7 (SC-001 a SC-007) |
| Histórias | 4 (US1 a US3 P1, US4 P2) |
| Tarefas | 23 (T001 a T023) |
| Cobertura de requisitos por tarefas | 100% |
| Achados | 1 médio, 2 baixos (todos aplicados), 3 observações |
| Críticos | 0 |
| Violações da constituição | 0 |

## Achados

| ID | Categoria | Severidade | Onde | Achado | Resolução |
|---|---|---|---|---|---|
| A1 | Inconsistência | Média | spec (SC-002, US2), quickstart, T011 | A conta de cliques estava errada: no ano atual são **4** cliques (abrir, título, mês, dia), não 3, e "chegar a março de 2024 em 4 cliques" só vale **depois de abrir** (título, ano, 2024, março) | SC-002 reescrito (≤ 4 no ano atual, 3 com a seta de um mês vizinho, ≤ 6 em ano a até 12 anos); texto de US2, quickstart e T011 ajustados |
| A2 | Lacuna | Baixa | T005 | O `Icon` só tem o chevron "voltar" (`back`); não existe o de "próximo" | A seta de próximo é o mesmo ícone girado por CSS (sem ícone novo, sem componente novo) |
| A3 | Lacuna | Baixa | spec, research, T003 | A máscara do `DateInput` aceita qualquer ano de 4 dígitos, mas a faixa navegável é 1900 a 2100 | Regra declarada: ano fora da faixa abre o calendário em hoje, sem erro e sem alterar o texto; caso coberto no script de borda (1899 e 2101 devolvem `null`) |
| A4 | Observação | — | plano | Todos os pares de contraste do calendário já estão em `verificar-contraste.mjs` (`--c-text` e `--c-text-muted` sobre as superfícies; `--c-on-accent` sobre `--c-accent`; `--c-accent-text` sobre as superfícies e sobre a tinta de destaque) e os tokens `--c-accent`, `--c-accent-tint`, `--c-accent-text` e `--c-on-accent` existem em `paleta.ts` | T019 só confirma; nenhum token novo |
| A5 | Observação | — | R2 | O "hoje" já existe em `lib/format.ts` (`dataIsoLocal`, campos locais, sem `toISOString`), então não há conversão por fuso | Reaproveitado |
| A6 | Observação | — | R7 | Os modais com Esc (`AulaDetalhesPopup`, `ConfirmModal`) escutam `keydown` no documento (fase de bolha); a escuta do calendário em captura com `stopPropagation` impede o fechamento duplo | Coberto por T012 e T014 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Tarefa de verificação |
|---|---|
| FR-002 (sem cor literal) | T020(d): `grep` nas linhas adicionadas |
| FR-008 (sem conversão de data pelo fuso) | T020(a): leitura do diff e `grep` de `new Date(`, `Date.parse`, `toISOString`, `getTimezoneOffset`; T003: dia da semana de datas conhecidas |
| FR-014 (nada nativo restante) | T020(b): `grep` de `type="date"`, `type="month"`, `date-nativo` e `color-scheme` |
| FR-015 (sem dependência nova; backend intocado) | T020(c): `git diff` do `package.json` e do lock e `git status`/`git diff --stat` de `src`, `tests` e `database` |
| FR-016 (contrato do `DateInput` intacto) | T020(e): leitura do diff e `git status` das 14 telas |
| Arquivos novos | T020(f): `git status --porcelain -- frontend` só com os 4 previstos |

## Constituição

Sem violações: nenhuma regra de negócio no frontend (o servidor continua validando as datas e o mínimo só bloqueia a escolha), sem opção configurável nova, sem segredos, documentação (nota na spec 069 e relatório).

## Riscos conferidos no código

- Os modais usam camadas até 110 e rolam (`overflow-y: auto`); as tabelas rolam na horizontal: o portal na raiz com camada 120 resolve (R3).
- O ESLint do projeto proíbe `setState` direto em efeito: posição aplicada no estilo do elemento e `matchMedia` por `useSyncExternalStore` (R3/R4).
- Memória baixa nesta máquina (~700 MB livres): comandos do Node com `--max-old-space-size=1536` e saída dos roteiros longos em arquivo (registrado nas tarefas).
- Sem framework de teste de interface: a lógica pura ganha script próprio (T003) e a interface é validada no Chrome **com capturas**, já que o calendário agora faz parte da página.

## Próximos passos

1. `/speckit-implement` seguindo `tasks.md` (aguarda a sua aprovação), com relatório em `docs/relatorios/` e card movido para Revisão de Código/Conceito pelo hook.
