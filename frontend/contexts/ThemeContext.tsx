"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

export type ColorMode = "normal" | "protanopia" | "deuteranopia" | "tritanopia" | "personalizado";

const ACCENT_PADRAO = "#4ecdc4";
const CUSTOM_ACCENT_PADRAO = "#4ecdc4";
const CUSTOM_ACCENT2_PADRAO = "#bb9457";
const CUSTOM_ACCENT3_PADRAO = "#ffe66d";
const CUSTOM_DANGER_PADRAO = "#c0392b";
const STORAGE_KEY = "spi:preferencias-tema";

interface Preferencias {
  dark: boolean;
  colorMode: ColorMode;
  accentColor: string;
  customAccent: string;
  customAccent2: string;
  customAccent3: string;
  customDanger: string;
}

interface ThemeContextValue extends Preferencias {
  toggleDark: () => void;
  setDark: (dark: boolean) => void;
  setColorMode: (mode: ColorMode) => void;
  setAccentColor: (hex: string) => void;
  resetAccentColor: () => void;
  setCustomAccent: (hex: string) => void;
  setCustomAccent2: (hex: string) => void;
  setCustomAccent3: (hex: string) => void;
  setCustomDanger: (hex: string) => void;
  resetCustomColors: () => void;
}

const PADRAO: Preferencias = {
  dark: false,
  colorMode: "normal",
  accentColor: ACCENT_PADRAO,
  customAccent: CUSTOM_ACCENT_PADRAO,
  customAccent2: CUSTOM_ACCENT2_PADRAO,
  customAccent3: CUSTOM_ACCENT3_PADRAO,
  customDanger: CUSTOM_DANGER_PADRAO,
};

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [prefs, setPrefs] = useState<Preferencias>(PADRAO);

  // Preferencias de tema/acessibilidade nao sao sensiveis (ao contrario dos
  // tokens de auth): usar localStorage aqui e uma decisao separada, para
  // nao perder a escolha da professora a cada F5.
  useEffect(() => {
    // Le do localStorage (sistema externo) so apos o mount, de proposito:
    // localStorage nao existe no SSR, e usar isso num inicializador
    // preguicoso do useState quebraria a hidratacao (server sem tema salvo
    // x client com tema salvo). Excecao legitima a regra set-state-in-effect.
    try {
      const salvo = localStorage.getItem(STORAGE_KEY);
      if (salvo) {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setPrefs({ ...PADRAO, ...JSON.parse(salvo) });
      }
    } catch {
      // ambiente sem localStorage (SSR, modo privado restrito): mantem o padrao
    }
  }, []);

  const persistir = useCallback((novo: Preferencias) => {
    setPrefs(novo);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(novo));
    } catch {
      // ignora falha de persistencia
    }
  }, []);

  const toggleDark = useCallback(() => persistir({ ...prefs, dark: !prefs.dark }), [prefs, persistir]);
  const setDark = useCallback((dark: boolean) => persistir({ ...prefs, dark }), [prefs, persistir]);
  const setColorMode = useCallback((colorMode: ColorMode) => persistir({ ...prefs, colorMode }), [prefs, persistir]);
  const setAccentColor = useCallback((accentColor: string) => persistir({ ...prefs, accentColor }), [prefs, persistir]);
  const resetAccentColor = useCallback(() => persistir({ ...prefs, accentColor: ACCENT_PADRAO }), [prefs, persistir]);
  const setCustomAccent = useCallback((customAccent: string) => persistir({ ...prefs, customAccent }), [prefs, persistir]);
  const setCustomAccent2 = useCallback((customAccent2: string) => persistir({ ...prefs, customAccent2 }), [prefs, persistir]);
  const setCustomAccent3 = useCallback((customAccent3: string) => persistir({ ...prefs, customAccent3 }), [prefs, persistir]);
  const setCustomDanger = useCallback((customDanger: string) => persistir({ ...prefs, customDanger }), [prefs, persistir]);
  const resetCustomColors = useCallback(
    () =>
      persistir({
        ...prefs,
        customAccent: CUSTOM_ACCENT_PADRAO,
        customAccent2: CUSTOM_ACCENT2_PADRAO,
        customAccent3: CUSTOM_ACCENT3_PADRAO,
        customDanger: CUSTOM_DANGER_PADRAO,
      }),
    [prefs, persistir]
  );

  const value = useMemo(
    () => ({
      ...prefs,
      toggleDark,
      setDark,
      setColorMode,
      setAccentColor,
      resetAccentColor,
      setCustomAccent,
      setCustomAccent2,
      setCustomAccent3,
      setCustomDanger,
      resetCustomColors,
    }),
    [
      prefs,
      toggleDark,
      setDark,
      setColorMode,
      setAccentColor,
      resetAccentColor,
      setCustomAccent,
      setCustomAccent2,
      setCustomAccent3,
      setCustomDanger,
      resetCustomColors,
    ]
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme precisa ser usado dentro de um ThemeProvider.");
  }

  return context;
}
