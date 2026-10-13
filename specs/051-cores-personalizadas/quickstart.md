# Quickstart: validar o modo de cores Personalizado

Guia de validação ponta a ponta. Modelo de dados em [data-model.md](./data-model.md); decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Login feito, acesso à tela Configurações.

## 1. Ativar o modo Personalizado e escolher as 4 cores (US1, FR-001 a FR-004, SC-001/SC-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir Configurações → "Modo de cores" | 5 cards: Padrão, Protanopia, Deuteranopia, Tritanopia, **Personalizado** |
| 2 | Clicar em "Personalizado" | Card fica ativo; a seção "Cor de destaque" abaixo passa a mostrar **4 seletores de cor**, cada um com os mesmos valores do modo Padrão (`#4ecdc4`, `#bb9457`, `#ffe66d`, `#c0392b`) |
| 3 | Escolher uma cor diferente em cada um dos 4 seletores | Botões de ação principal, destaques secundários/terciários, e qualquer elemento de erro/perigo na interface (ex.: um alerta) refletem as novas cores imediatamente, sem recarregar a página |
| 4 | Navegar para outra tela do sistema (ex.: Dashboard) | As 4 cores escolhidas continuam aplicadas fora da tela de Configurações |

## 2. Restaurar as cores personalizadas (US2, FR-005, SC-004)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com o modo Personalizado ativo e pelo menos 1 cor alterada | — |
| 2 | Clicar em "Restaurar padrão" (dentro do bloco de cor, modo Personalizado) | As 4 cores voltam aos valores originais numa única ação; o modo continua sendo "Personalizado" (não volta para "Padrão") |

## 3. Trocar de modo não apaga a personalização (Edge Case do spec.md, FR-007, SC-003)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com o modo Personalizado ativo e cores customizadas escolhidas | — |
| 2 | Trocar para "Padrão" ou algum modo de daltonismo | Interface reflete o modo escolhido |
| 3 | Voltar para "Personalizado" | As 4 cores customizadas escolhidas antes continuam exatamente as mesmas (não voltaram ao padrão) |

## 4. Persistência entre sessões (FR-006)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com cores personalizadas escolhidas, fechar e reabrir o navegador (ou recarregar a página com F5) | Modo "Personalizado" e as 4 cores escolhidas continuam aplicados |

## 5. Não-regressão (FR-008/FR-009, SC-002)

- Nenhuma chamada de rede é feita ao trocar de modo de cores ou escolher uma cor (confirmar na aba Network do navegador) — tudo é local.
- Escolher uma combinação de cores deliberadamente ruim (ex.: branco sobre branco) não é bloqueada nem gera nenhum erro — o sistema aceita qualquer valor.
- Os 4 modos de daltonismo já existentes continuam com suas paletas fixas, inalteradas — trocar para "Personalizado" e voltar não altera nenhum deles.
- `git diff --stat` mostra só os 3 arquivos esperados (`ThemeContext.tsx`, `AppShell.tsx`, `configuracoes/page.tsx`) — nenhuma mudança em `src`/`database`.
