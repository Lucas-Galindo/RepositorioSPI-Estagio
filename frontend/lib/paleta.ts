/**
 * specs/057: fonte unica das cores do SPI. Telas, estilos e o script de
 * verificacao (scripts/verificar-contraste.mjs) leem daqui -- nenhum outro
 * arquivo deve ter codigo de cor. As cores de TEXTO nao sao escolhidas a
 * mao: sao derivadas das cores base para sempre dar contraste WCAG AA
 * (4.5:1) no modo claro e no escuro (research.md R2/R3).
 *
 * Sem imports de proposito: o script Node importa este arquivo direto.
 */

export type ColorMode = "normal" | "protanopia" | "deuteranopia" | "tritanopia";

export interface PreferenciasTema {
  dark: boolean;
  colorMode: ColorMode;
  /** Id de DESTAQUES; so vale no modo de cores "normal". */
  destaque: string;
}

export const CONTRASTE_MINIMO = 4.5;

const TEXTO_ESCURO = "#1a2024";
const BRANCO = "#ffffff";
const PRETO = "#000000";

// ---------- Cores base ----------

const NEUTROS = {
  claro: {
    bg: "#f7fff7",
    surface: "#ffffff",
    surface2: "#f7fff7",
    text: "#292f36",
    textMuted: "#7c848c",
    sidebar: "#262c33",
    sidebar2: "#2c333b",
  },
  escuro: {
    bg: "#1b1f24",
    surface: "#242a31",
    surface2: "#1e242a",
    text: "#eef1f2",
    textMuted: "#95a0a8",
    sidebar: "#14171b",
    sidebar2: "#181c21",
  },
};

// Tom base dos textos secundarios da barra lateral (escura nos dois modos).
const LATERAL_MUTED = "#8b939b";

/** Amostras de destaque oferecidas na tela de Configuracoes (ordem = ordem de exibicao). */
export const DESTAQUES = [
  { id: "turquesa", nome: "Turquesa", cor: "#4ecdc4" },
  { id: "dourado", nome: "Dourado", cor: "#bb9457" },
  { id: "azul", nome: "Azul", cor: "#0072b2" },
  { id: "laranja", nome: "Laranja", cor: "#e69f00" },
  { id: "rosa", nome: "Rosa", cor: "#cc79a7" },
  { id: "verde", nome: "Verde", cor: "#009e73" },
] as const;

export const DESTAQUE_PADRAO = "turquesa";

interface CoresDoModo {
  /** null = usa a amostra de destaque escolhida (so no modo "normal"). */
  accent: string | null;
  accent2: string;
  accent3: string;
  danger: string;
}

/** Modos de cores: o Padrao e as paletas seguras para daltonismo (Okabe-Ito). */
export const MODOS_DE_CORES: { id: ColorMode; nome: string; desc: string; cores: CoresDoModo }[] = [
  { id: "normal", nome: "Padrão", desc: "Paleta original do SPI.", cores: { accent: null, accent2: "#bb9457", accent3: "#ffe66d", danger: "#c0392b" } },
  { id: "protanopia", nome: "Protanopia", desc: "Ajuste para dificuldade com tons de vermelho.", cores: { accent: "#0072b2", accent2: "#e69f00", accent3: "#f0e442", danger: "#d55e00" } },
  { id: "deuteranopia", nome: "Deuteranopia", desc: "Ajuste para dificuldade com tons de verde.", cores: { accent: "#0072b2", accent2: "#cc79a7", accent3: "#f0e442", danger: "#d55e00" } },
  { id: "tritanopia", nome: "Tritanopia", desc: "Ajuste para dificuldade com tons de azul.", cores: { accent: "#cc79a7", accent2: "#e69f00", accent3: "#009e73", danger: "#d55e00" } },
];

export const PREFERENCIAS_PADRAO: PreferenciasTema = { dark: false, colorMode: "normal", destaque: DESTAQUE_PADRAO };

// Opacidade das tintas (fundos translucidos de selos/chips), como ja era antes.
const ALFA_TINTA = {
  accent: { claro: 0.15, escuro: 0.16 },
  accent2: { claro: 0.15, escuro: 0.18 },
  accent3: { claro: 0.35, escuro: 0.16 },
  danger: { claro: 0.12, escuro: 0.22 },
};

// ---------- Matematica de cor (WCAG 2.x) ----------

function rgb(hex: string): [number, number, number] {
  const n = parseInt(hex.replace("#", ""), 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
}

function paraHex(c: number[]): string {
  return "#" + c.map((v) => Math.round(Math.max(0, Math.min(255, v))).toString(16).padStart(2, "0")).join("");
}

/** Mistura `a` com `b` na proporcao `t` (0 = a, 1 = b). */
export function misturar(a: string, b: string, t: number): string {
  const [x, y] = [rgb(a), rgb(b)];
  return paraHex(x.map((v, i) => v + (y[i] - v) * t));
}

function luminancia(hex: string): number {
  const [r, g, b] = rgb(hex).map((v) => {
    const s = v / 255;
    return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/** Razao de contraste WCAG entre duas cores opacas (1 a 21). */
export function contraste(a: string, b: string): number {
  const [claro, escuro] = [luminancia(a), luminancia(b)].sort((p, q) => q - p);
  return (claro + 0.05) / (escuro + 0.05);
}

// Leva `cor` aos poucos rumo a `alvo` (preto para escurecer, branco para
// clarear) ate passar do contraste minimo contra TODOS os fundos. Para no
// primeiro passo que passa, para mudar o tom o minimo possivel.
function ajustarAteContraste(cor: string, fundos: string[], alvo: string): string {
  for (let passo = 0; passo <= 50; passo++) {
    const candidata = misturar(cor, alvo, passo / 50);
    if (fundos.every((f) => contraste(candidata, f) >= CONTRASTE_MINIMO)) {
      return candidata;
    }
  }
  return alvo;
}

function rgba(hex: string, alfa: number): string {
  const [r, g, b] = rgb(hex);
  return `rgba(${r}, ${g}, ${b}, ${alfa})`;
}

// ---------- Tokens ----------

/** Cores base do modo escolhido (a amostra so entra no modo "normal"). */
export function coresBase(prefs: PreferenciasTema): { accent: string; accent2: string; accent3: string; danger: string } {
  const modo = MODOS_DE_CORES.find((m) => m.id === prefs.colorMode) ?? MODOS_DE_CORES[0];
  const amostra = DESTAQUES.find((d) => d.id === prefs.destaque) ?? DESTAQUES[0];
  return { ...modo.cores, accent: modo.cores.accent ?? amostra.cor };
}

/**
 * Todas as variaveis CSS de cor do tema (contracts/tokens-de-tema.md).
 * Funcao pura: mesma entrada, mesma saida -- usada no servidor (primeiro
 * desenho), no navegador e no script de verificacao.
 */
export function tokensDoTema(prefs: PreferenciasTema): Record<string, string> {
  const n = prefs.dark ? NEUTROS.escuro : NEUTROS.claro;
  const fundosDoModo = [n.bg, n.surface, n.surface2];
  const lateral = [n.sidebar, n.sidebar2];
  const base = coresBase(prefs);

  const tokens: Record<string, string> = {
    "--c-bg": n.bg,
    "--c-surface": n.surface,
    "--c-surface-2": n.surface2,
    "--c-text": n.text,
    "--c-text-muted": ajustarAteContraste(n.textMuted, fundosDoModo, prefs.dark ? BRANCO : PRETO),
    "--c-sidebar": n.sidebar,
    "--c-sidebar-2": n.sidebar2,
    "--c-on-sidebar": BRANCO,
    "--c-on-sidebar-muted": ajustarAteContraste(LATERAL_MUTED, lateral, BRANCO),
    "--c-on-strong": BRANCO,
  };

  for (const papel of ["accent", "accent2", "accent3", "danger"] as const) {
    const cor = base[papel];
    const alfa = ALFA_TINTA[papel][prefs.dark ? "escuro" : "claro"];
    const alfaClaro = ALFA_TINTA[papel].claro;

    // "Forte": escurecida ate ler bem sobre os fundos claros e sobre a propria
    // tinta. Nao depende do modo, e por isso branco por cima tambem passa.
    const claros = [NEUTROS.claro.bg, NEUTROS.claro.surface, NEUTROS.claro.surface2];
    const forte = ajustarAteContraste(cor, [...claros, ...claros.map((f) => misturar(f, cor, alfaClaro))], PRETO);

    // Cor usada como texto: no claro a forte; no escuro clareada contra os
    // fundos escuros (e a tinta) do modo.
    const texto = prefs.dark
      ? ajustarAteContraste(cor, [...fundosDoModo, ...fundosDoModo.map((f) => misturar(f, cor, alfa))], BRANCO)
      : forte;

    tokens[`--c-${papel}`] = cor;
    tokens[`--c-${papel}-deep`] = misturar(cor, PRETO, 0.22);
    tokens[`--c-${papel}-strong`] = forte;
    tokens[`--c-${papel}-text`] = texto;
    tokens[`--c-${papel}-tint`] = rgba(cor, alfa);
  }

  // So o destaque e fundo solido com texto por cima (botao principal, menu
  // ativo, selo "Hoje") e cor sobre a barra lateral.
  const corDestaque = base.accent;
  const sobreDestaque = contraste(TEXTO_ESCURO, corDestaque) >= contraste(BRANCO, corDestaque) ? TEXTO_ESCURO : BRANCO;
  tokens["--c-on-accent"] = sobreDestaque;
  // O gradiente vai ate 22% mais escuro, mas para antes se o texto escuro
  // deixar de ler bem (com texto branco, escurecer so melhora).
  let fundoGradiente = 0.22;
  while (fundoGradiente > 0 && contraste(sobreDestaque, misturar(corDestaque, PRETO, fundoGradiente)) < CONTRASTE_MINIMO) {
    fundoGradiente -= 0.01;
  }
  tokens["--c-accent-deep"] = misturar(corDestaque, PRETO, Math.max(0, fundoGradiente));
  tokens["--c-accent-on-sidebar"] = ajustarAteContraste(corDestaque, lateral, BRANCO);

  return tokens;
}

// ---------- Preferencias salvas ----------

/** Amostra de menor distancia RGB a `hex`; no empate, a primeira da lista. */
function amostraMaisProxima(hex: string): string {
  if (!/^#[0-9a-fA-F]{6}$/.test(hex)) return DESTAQUE_PADRAO;
  const alvo = rgb(hex);
  let melhor: string = DESTAQUE_PADRAO;
  let menor = Infinity;
  for (const d of DESTAQUES) {
    const distancia = rgb(d.cor).reduce((soma, v, i) => soma + (v - alvo[i]) ** 2, 0);
    if (distancia < menor) {
      menor = distancia;
      melhor = d.id;
    }
  }
  return melhor;
}

/**
 * Converte o que estiver salvo no navegador para o formato atual
 * (research.md R5). `mudou` indica que o valor precisa ser regravado.
 * Formatos antigos (spec 051): cor de destaque livre (`accentColor`) e o
 * modo "personalizado" com 4 cores livres (`customAccent`, ...).
 */
export function migrarPreferencias(salvo: string | null): { preferencias: PreferenciasTema; mudou: boolean } {
  if (salvo === null) return { preferencias: PREFERENCIAS_PADRAO, mudou: false };

  let bruto: Record<string, unknown>;
  try {
    const lido = JSON.parse(salvo);
    if (typeof lido !== "object" || lido === null) throw new Error("formato invalido");
    bruto = lido;
  } catch {
    // Valor corrompido: volta ao padrao (e o grava, para nao reler o lixo).
    return { preferencias: PREFERENCIAS_PADRAO, mudou: true };
  }

  const dark = bruto.dark === true;
  let colorMode: ColorMode = MODOS_DE_CORES.some((m) => m.id === bruto.colorMode) ? (bruto.colorMode as ColorMode) : "normal";
  let destaque = typeof bruto.destaque === "string" && DESTAQUES.some((d) => d.id === bruto.destaque) ? bruto.destaque : null;

  if (bruto.colorMode === "personalizado") {
    // O modo de 4 cores livres deixou de existir: vira o Padrao com a amostra
    // mais parecida com o destaque principal que a professora tinha escolhido.
    colorMode = "normal";
    destaque = amostraMaisProxima(String(bruto.customAccent ?? ""));
  } else if (destaque === null && typeof bruto.accentColor === "string") {
    destaque = amostraMaisProxima(bruto.accentColor);
  }

  const preferencias: PreferenciasTema = { dark, colorMode, destaque: destaque ?? DESTAQUE_PADRAO };
  // Regrava se o salvo nao for exatamente o formato atual (campos antigos, valores invalidos).
  const mudou = JSON.stringify(bruto) !== JSON.stringify(preferencias);
  return { preferencias, mudou };
}
