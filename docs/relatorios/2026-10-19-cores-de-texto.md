# Relatório: Cores de Texto — contraste automático e paleta restrita (spec 057)

**Card Trello**: "Cores de texto" (Backlog → Design → A Fazer → Em andamento → Revisão)
**Spec**: `specs/057-cores-de-texto/` (specify, plan, tasks, analyze e implement em 2026-10-19)
**Status final**: ✅ Implementado. Falta só a validação visual num navegador (T019, parte manual).

## O que foi entendido do pedido

- **Parte A**: todo texto acompanha o modo claro/escuro e fica legível, com contraste WCAG AA (4.5:1), usando tokens de tema no lugar de cores fixas.
- **Parte B**: acabou a cor livre. A cor de destaque é escolhida só entre amostras nomeadas da paleta oficial, cada uma com variantes para claro e escuro e cor de texto automática. Personalizações antigas são migradas para a opção mais próxima.

## O que foi feito

**Paleta numa fonte única: `frontend/lib/paleta.ts`**
- Reúne neutros de claro e escuro, as 6 amostras de destaque (Turquesa, Dourado, Azul, Laranja, Rosa e Verde) e as cores dos modos Padrão e de daltonismo, com as mesmas cores de hoje.
- `tokensDoTema()` **deriva** todos os tokens de cor, incluindo os de texto:
  - **texto sobre a cor**: escuro ou branco, o que contrastar mais;
  - **variante "forte"**: escurecida até 4.5:1 contra os fundos claros e a tinta da cor;
  - **variante para o escuro**: clareada até 4.5:1;
  - **gradientes**: limitados para o texto continuar legível;
  - **barra lateral**: tokens próprios, porque ela é escura nos dois modos.
- `migrarPreferencias()` converte o que estava salvo no navegador (detalhes em "Migração" abaixo).

**Aplicação**
- O `ThemeProvider` grava os tokens e a classe `dark-mode` no `<html>`. Com isso, toast, modais e a tela de login seguem o tema. Antes o toast nunca seguia o modo escuro, porque fica fora do `.app`.
- `app/layout.tsx` já entrega os tokens do padrão no HTML do servidor, para o primeiro desenho sair com as cores certas.

**Código removido**
- Estilo inline e classes `cb-*` do `AppShell`.
- Classes `.cb-*` e as cores de `tokens.css`, que ficou só com fontes, raios e sombras.
- `lib/color.ts`, que só o `AppShell` usava.
- O `dark-mode` duplicado das telas de login e recuperação de senha.
- O seletor `input type="color"` e os estilos `accent-*`.
- O modo "Personalizado", com seus campos e setters.

**CSS e telas**
- Todas as cores de texto literais de `dashboard.css` e `login.css` viraram tokens.
- Os usos inline em TSX em que a cor de destaque virava texto passaram para as variantes `-text` (agenda, aulas, financeiro, lembretes, matérias).
- Os gradientes `#8f6f3d` dos relatórios passaram a usar `--c-accent2-deep`.
- Os avatares passaram a usar as variantes fortes.
- Em `CadastrarProfessoraForm`, a borda apontava para `var(--border, #e5e5e5)`, um token que não existe; agora usa `var(--c-bg)`.

**Configurações**
- "Modo de cores" mostra as 4 opções, sem Personalizado; as amostras vêm da paleta.
- "Cor de destaque" virou uma grade com as 6 amostras nomeadas, com a padrão marcada como "(padrão)".
- Com um modo de daltonismo ativo, a grade fica desabilitada e mostra a explicação.

**Verificação permanente**
- `npm run verificar-contraste` (`frontend/scripts/verificar-contraste.mjs`) confere todos os pares texto × fundo do contrato em 48 combinações (claro/escuro × 4 modos × 6 amostras) e os casos de migração.

**Documentação**: a spec 051 ganhou nota apontando para a 057.

## Problemas de contraste que já existiam (corrigidos junto)

| Cor | Onde | Antes | Depois |
|---|---|---|---|
| `#7c848c` (texto secundário) | sobre branco | 3.79:1 | `#6d747b` |
| `#69717a` (rótulos da barra lateral) | barra lateral | 2.85:1 | derivado, ≥ 4.5:1 |
| `#8a6d00` (Pendente/aviso), no modo escuro | tinta amarela escura | 1.89:1 | variante própria do escuro |

Os gradientes do botão principal com destaque Dourado, Rosa ou Verde caíam para 3.1–3.7:1 e agora ficam ≥ 4.5:1.

## Decisões tomadas e por quê

- **Amarelos e vermelhos não viram amostra de destaque.** O amarelo não dá contraste como texto em fundo claro, e o vermelho é reservado a alerta/perigo. Está registrado nas Assumptions da spec.
- **A conta de cor ficou dentro de `paleta.ts`, sem imports.** Assim o script Node importa um arquivo só.
- **Tokens de texto só onde há uso.** `--c-on-*` e `--c-*-on-sidebar` existem só para o destaque principal, porque nenhum estilo põe texto sobre as outras cores. Gerá-los para os outros papéis criaria tokens mortos. O contrato foi ajustado.
- **Sem botão "Restaurar padrão".** A amostra padrão aparece marcada na grade, então voltar a ela é um clique.
- **Rótulos secundários do painel do login.** Alguns cinzas mais claros foram unificados no token secundário da barra lateral. A hierarquia visual fica um pouco mais discreta, mas sempre ≥ 4.5:1.

## Migração das preferências antigas

| Salvo antes | Vira |
|---|---|
| Cor livre (`accentColor`) | A amostra mais próxima |
| Modo "Personalizado" | Modo Padrão com a amostra mais próxima do destaque principal |
| Valor inválido | O padrão do sistema |

## Como foi testado

- `npm run verificar-contraste`: **48 combinações, 0 pares abaixo de 4.5:1; 4/4 casos de migração OK.**
- **MUST NOT**:
  - nenhum código de cor fora de `lib/paleta.ts` (grep em styles/app/components/lib/contexts);
  - nenhum `type="color"`, `personalizado`, `customAccent` ou `accentColor` fora da função de migração.
  - A leitura do diff confirma que a cor aplicada sempre vem de `DESTAQUES` por id.
- `git status --porcelain`: os arquivos novos são só `lib/paleta.ts`, `scripts/verificar-contraste.mjs` e os documentos da spec.
- `tsc` limpo e `npm run build` OK.
- `eslint`: limpo nos arquivos tocados. Restam 2 erros antigos em `contexts/AuthContext.tsx`, que esta feature não toca.
- **Build de produção servido localmente**: o HTML do login já chega com os tokens no `<html>`.

## Pendências e riscos

- **Validação visual (T019).** Não havia navegador na sessão. Falta passar pelas telas do quickstart.md §2 em claro e escuro, com algumas amostras.
- **Lint antigo.** Os 2 erros de `AuthContext.tsx` merecem um card próprio.
