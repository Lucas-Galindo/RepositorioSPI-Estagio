# Quickstart: validar cores de texto (spec 057)

Tokens descritos em [contracts/tokens-de-tema.md](./contracts/tokens-de-tema.md); decisões em [research.md](./research.md).

## 1. Verificação automática

| # | Comando (em `frontend/`) | Esperado |
|---|---|---|
| 1 | `node scripts/verificar-contraste.mjs` | "48 combinações, 0 pares abaixo de 4.5:1" e código de saída 0 |
| 2 | `grep -rnE "#[0-9a-fA-F]{3,8}\b" styles app components lib --include=*.css --include=*.tsx --include=*.ts` | só ocorrências em `lib/paleta.ts`; as sombras de `styles/tokens.css` usam `rgba` |
| 3 | `npx tsc --noEmit`, `npx eslint`, `npm run build` | limpos |

## 2. Navegador (manual)

| # | Passo | Esperado |
|---|---|---|
| 1 | Configurações → Cor de destaque | Só as 6 amostras nomeadas, com a atual marcada; nenhum seletor de cor |
| 2 | Modo de cores | Padrão, Protanopia, Deuteranopia, Tritanopia (sem Personalizado) |
| 3 | Para cada amostra, alternar claro/escuro e passar por dashboard, aulas, alunos, financeiro, relatórios e um modal | Todo texto legível; botão principal, menu ativo e selo "Hoje" com texto legível |
| 4 | Modo de daltonismo ativo | Cor de destaque desabilitada, com explicação |
| 5 | Sair e ver a tela de login com o modo escuro salvo | Login no modo escuro, legível |

## 3. Migração de preferência antiga

| # | Passo (DevTools → Application → Local Storage) | Esperado após recarregar |
|---|---|---|
| 1 | `spi:preferencias-tema` = `{"dark":false,"colorMode":"normal","accentColor":"#1e90ff"}` | Destaque "Azul"; o valor salvo passa a ter `destaque: "azul"`, sem `accentColor` |
| 2 | `{"colorMode":"personalizado","customAccent":"#2e8b57"}` | Modo Padrão, destaque "Verde" |
| 3 | `nao-e-json` | Padrão do sistema, sem erro |
