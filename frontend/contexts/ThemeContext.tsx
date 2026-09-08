"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

export type ColorMode = "normal" | "protanopia" | "deuteranopia" | "tritanopia";

const ACCENT_PADRAO = "#4ecdc4";
const STORAGE_KEY = "spi:preferencias-tema";

interface Preferencias {
  dark: boolean;
  colorMode: ColorMode;
  accentColor: string;
}

interface ThemeContextValue extends Preferencias {
  toggleDark: () => void;
  setDark: (dark: boolean) => void;
  setColorMode: (mode: ColorMode) => void;
  setAccentColor: (hex: string) => void;
  resetAccentColor: () => void;
}

const PADRAO: Preferencias = { dark: false, colorMode: "normal", accentColor: ACCENT_PADRAO };

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

  const value = useMemo(
    () => ({ ...prefs, toggleDark, setDark, setColorMode, setAccentColor, resetAccentColor }),
    [prefs, toggleDark, setDark, setColorMode, setAccentColor, resetAccentColor]
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
