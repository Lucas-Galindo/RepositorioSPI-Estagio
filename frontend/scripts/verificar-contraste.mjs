// specs/057: confere, sem navegador, que todo par texto x fundo do tema passa
// de 4.5:1 (WCAG AA) em todas as combinacoes de modo claro/escuro x modo de
// cores x amostra de destaque, e que a migracao das preferencias antigas
// funciona. Uso (em frontend/): node scripts/verificar-contraste.mjs
// Node 24 executa o .ts direto (remocao de tipos nativa).
import {
  CONTRASTE_MINIMO,
  DESTAQUES,
  MODOS_DE_CORES,
  contraste,
  migrarPreferencias,
  misturar,
  tokensDoTema,
} from "../lib/paleta.ts";

const PAPEIS = ["accent", "accent2", "accent3", "danger"];

// rgba(r, g, b, a) composta sobre um fundo opaco -> hex.
function comporTinta(tinta, fundo) {
  const [r, g, b, a] = tinta.match(/[\d.]+/g).map(Number);
  const cor = "#" + [r, g, b].map((v) => v.toString(16).padStart(2, "0")).join("");
  return misturar(fundo, cor, a);
}

// Pares de contracts/tokens-de-tema.md ("Pares conferidos").
function pares(t) {
  const fundos = ["--c-bg", "--c-surface", "--c-surface-2"];
  const lateral = ["--c-sidebar", "--c-sidebar-2"];
  const lista = [];
  for (const f of fundos) {
    lista.push(["--c-text", f], ["--c-text-muted", f]);
  }
  for (const f of lateral) {
    lista.push(["--c-on-sidebar", f], ["--c-on-sidebar-muted", f], ["--c-accent-on-sidebar", f]);
  }
  lista.push(["--c-on-accent", "--c-accent"], ["--c-on-accent", "--c-accent-deep"]);
  for (const p of PAPEIS) {
    for (const f of fundos) lista.push([`--c-${p}-text`, f]);
    lista.push([`--c-${p}-text`, comporTinta(t[`--c-${p}-tint`], t["--c-surface"])]);
    lista.push(["--c-on-strong", `--c-${p}-strong`]);
  }
  return lista;
}

let combinacoes = 0;
const reprovados = [];
for (const dark of [false, true]) {
  for (const modo of MODOS_DE_CORES) {
    for (const amostra of DESTAQUES) {
      combinacoes++;
      const t = tokensDoTema({ dark, colorMode: modo.id, destaque: amostra.id });
      for (const [texto, fundo] of pares(t)) {
        const corFundo = fundo.startsWith("#") ? fundo : t[fundo];
        const razao = contraste(t[texto], corFundo);
        if (razao < CONTRASTE_MINIMO) {
          reprovados.push(`${dark ? "escuro" : "claro"}/${modo.id}/${amostra.id}: ${texto} (${t[texto]}) sobre ${fundo} (${corFundo}) = ${razao.toFixed(2)}`);
        }
      }
    }
  }
}

// Migracao (quickstart.md secao 3).
const casos = [
  { salvo: '{"dark":false,"colorMode":"normal","accentColor":"#1e90ff"}', esperado: { dark: false, colorMode: "normal", destaque: "azul" }, mudou: true },
  { salvo: '{"colorMode":"personalizado","customAccent":"#2e8b57"}', esperado: { dark: false, colorMode: "normal", destaque: "verde" }, mudou: true },
  { salvo: "nao-e-json", esperado: { dark: false, colorMode: "normal", destaque: "turquesa" }, mudou: true },
  { salvo: '{"dark":true,"colorMode":"tritanopia","destaque":"rosa"}', esperado: { dark: true, colorMode: "tritanopia", destaque: "rosa" }, mudou: false },
];
const falhasMigracao = casos
  .map((c) => ({ c, r: migrarPreferencias(c.salvo) }))
  .filter(({ c, r }) => JSON.stringify(r.preferencias) !== JSON.stringify(c.esperado) || r.mudou !== c.mudou)
  .map(({ c, r }) => `migracao de ${c.salvo}: veio ${JSON.stringify(r)}`);

console.log(`${combinacoes} combinações, ${reprovados.length} pares abaixo de ${CONTRASTE_MINIMO}:1; ${casos.length - falhasMigracao.length}/${casos.length} casos de migração ok`);
for (const linha of [...reprovados, ...falhasMigracao]) console.log("  " + linha);
process.exit(reprovados.length + falhasMigracao.length > 0 ? 1 : 0);
