# Contrato: tokens de tema (saída de `tokensDoTema`)

Variáveis CSS que os estilos podem usar como cor. Nenhum estilo usa código de cor literal. Exceções: raios, fontes e sombras decorativas em `tokens.css`.

## Neutros

| Token | Uso |
|---|---|
| `--c-bg`, `--c-surface`, `--c-surface-2` | fundos |
| `--c-text` | texto principal |
| `--c-text-muted` | texto secundário |
| `--c-sidebar`, `--c-sidebar-2` | barra lateral, aviso flutuante, botão escuro, painel do login (escuros nos dois modos) |
| `--c-on-sidebar` | texto sobre a barra lateral |
| `--c-on-sidebar-muted` | texto secundário sobre a barra lateral |

## Por papel de cor

Os papéis são `accent`, `accent2`, `accent3` e `danger`. Cada um gera os tokens abaixo (`X` = nome do papel):

| Token | Uso |
|---|---|
| `--c-X` | cor base (marcadores, barras; no `accent`, também fundo sólido com texto) |
| `--c-X-deep` | fim de gradiente que começa em `--c-X` |
| `--c-X-strong` | fundo de avatar (texto branco por cima) |
| `--c-X-text` | cor X usada como texto/ícone sobre fundos do modo e sobre `--c-X-tint` |
| `--c-X-tint` | fundo translúcido de selos/chips |

Só do `accent`, que é o único papel usado como fundo sólido com texto e como cor sobre a barra lateral:

| Token | Uso |
|---|---|
| `--c-on-accent` | texto sobre `--c-accent` / `--c-accent-deep` |
| `--c-accent-on-sidebar` | destaque usado como texto/ícone sobre a barra lateral |

Também existe `--c-on-strong` (`#ffffff`), o texto sobre qualquer `--c-X-strong`. Gerar `on`/`on-sidebar` para os outros papéis criaria tokens sem uso: nenhum estilo põe texto sobre eles.

## Pares conferidos pelo script (4.5:1)

| Texto | Fundos |
|---|---|
| `--c-text` | `--c-bg`, `--c-surface`, `--c-surface-2` |
| `--c-text-muted` | `--c-bg`, `--c-surface`, `--c-surface-2` |
| `--c-on-sidebar` | `--c-sidebar`, `--c-sidebar-2` |
| `--c-on-sidebar-muted` | `--c-sidebar`, `--c-sidebar-2` |
| `--c-accent-on-sidebar` | `--c-sidebar`, `--c-sidebar-2` |
| `--c-on-accent` | `--c-accent`, `--c-accent-deep` |
| `--c-X-text` | `--c-bg`, `--c-surface`, `--c-surface-2` e `--c-X-tint` composta sobre `--c-surface` |
| `--c-on-strong` | `--c-X-strong` |

O script percorre 2 modos (claro/escuro) × 4 modos de cores × 6 amostras de destaque.
