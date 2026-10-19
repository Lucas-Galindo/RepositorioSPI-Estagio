# Implementation Plan: Cores de Texto (Contraste Automático e Paleta Restrita)

**Branch**: `057-cores-de-texto` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/057-cores-de-texto/spec.md`

## Summary

Hoje as cores do tema estão espalhadas: neutras e de daltonismo em `styles/tokens.css`, swatches repetidos em `configuracoes/page.tsx`, cor livre aplicada inline em `AppShell.tsx` e ~35 cores de texto literais nos CSS.

A abordagem: um módulo único `frontend/lib/paleta.ts` passa a ser a fonte de verdade das cores (neutras de claro/escuro, amostras de destaque nomeadas, paletas de daltonismo) e expõe uma função pura `tokensDoTema(preferencias)` que **deriva** todos os tokens de cor, inclusive os de texto. As cores de texto saem da paleta assim:

- **Texto sobre a cor** (`--c-on-*`): escolhe escuro ou branco pelo maior contraste.
- **Variante "forte"**: escurecida até 4.5:1 contra os fundos claros.
- **Variante para fundo escuro**: clareada até 4.5:1 contra os fundos escuros.

Os CSS passam a usar só tokens. O `ThemeProvider` aplica os tokens no `<html>`, e o layout raiz aplica os do padrão no primeiro desenho. A tela de Configurações oferece só amostras nomeadas. As preferências antigas (cor livre / "Personalizado") são migradas para a amostra mais próxima ao carregar. Um script Node (`frontend/scripts/verificar-contraste.mjs`) confere 4.5:1 em todos os pares texto × fundo de todas as combinações.

## Technical Context

**Language/Version**: TypeScript 5 / React 19 / Next.js 16 (frontend apenas); Node 24 para o script de verificação (executa `.ts` com remoção de tipos nativa).

**Primary Dependencies**: nenhuma nova.

**Storage**: `localStorage` (`spi:preferencias-tema`), só no navegador — formato muda (ver data-model.md).

**Testing**: não há suíte de testes de frontend; verificação por `node scripts/verificar-contraste.mjs` (cálculo WCAG), busca por literais de cor (`grep`), `tsc`, `eslint`, `npm run build`.

**Target Platform**: navegador (área logada e tela de login).

**Project Type**: web application (só a parte `frontend/` muda).

**Performance Goals**: troca de tema instantânea (só variáveis CSS; cálculo de ~40 tokens por troca).

**Constraints**: WCAG AA 4.5:1 para todo texto; sem cor livre; modos de daltonismo preservados.

**Scale/Scope**: 3 folhas de estilo, ~35 literais de texto, 4 arquivos TS/TSX centrais, 2 gradientes inline.

## Constitution Check

- **I. Exclusão lógica**: não se aplica (sem dados de negócio).
- **II. Validação de negócio no backend**: não se aplica — é preferência visual do navegador, sem regra de negócio; nada a validar no servidor.
- **III. Nenhuma capacidade configurável sem implementação real**: atendido e reforçado — remove-se o modo "Personalizado"/cor livre e cada amostra oferecida é aplicada de fato e verificada pelo script de contraste.
- **IV. Segredos**: não se aplica.
- **V. Documentação retroativa vinculante**: a spec 051 (cores personalizadas) recebe nota apontando para a 057 (FR-012).

**Resultado**: PASS (pré e pós-design).

## Project Structure

### Documentation (this feature)

```text
specs/057-cores-de-texto/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── tokens-de-tema.md
└── tasks.md            # /speckit-tasks
```

### Source Code (repository root)

```text
frontend/
├── lib/
│   ├── paleta.ts               # NOVO: fonte única das cores + tokensDoTema + migração
│   ├── color.ts                # helpers de cor (contraste/mistura) -- ganha luminância/contraste
│   └── format.ts               # avatarColor passa a usar tokens "fortes"
├── contexts/ThemeContext.tsx   # preferências novas + migração + aplica tokens no <html>
├── components/layout/AppShell.tsx  # deixa de montar estilo inline / classes cb-*
├── app/layout.tsx              # tokens do padrão no <html> (primeiro desenho)
├── app/(app)/configuracoes/page.tsx  # amostras nomeadas, sem seletor livre/Personalizado
├── app/(app)/relatorios/{alunos,turmas}/page.tsx  # gradientes via token
├── components/admin/CadastrarProfessoraForm.tsx   # remove var(--border, #e5e5e5) (token inexistente)
├── styles/tokens.css           # fica só com raios, fontes e sombras (claro/escuro)
├── styles/dashboard.css        # literais de texto -> tokens
├── styles/login.css            # literais de texto -> tokens
└── scripts/verificar-contraste.mjs  # NOVO: confere 4.5:1 em todas as combinações
specs/051-cores-personalizadas/spec.md  # nota de substituição
```

**Structure Decision**: tudo no frontend; a paleta vira módulo TS (não CSS) para ser a fonte única usada pela tela de Configurações, pela aplicação dos tokens e pelo script de verificação.

## Complexity Tracking

Sem violações.
