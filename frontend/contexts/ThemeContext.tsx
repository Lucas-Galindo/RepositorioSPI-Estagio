"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { migrarPreferencias, PREFERENCIAS_PADRAO, tokensDoTema, type ColorMode, type PreferenciasTema } from "@/lib/paleta";

export type { ColorMode } from "@/lib/paleta";

const STORAGE_KEY = "spi:preferencias-tema";

interface ThemeContextValue extends PreferenciasTema {
  toggleDark: () => void;
  setDark: (dark: boolean) => void;
  setColorMode: (mode: ColorMode) => void;
  setDestaque: (id: string) => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [prefs, setPrefs] = useState<PreferenciasTema>(PREFERENCIAS_PADRAO);

  const persistir = useCallback((novo: PreferenciasTema) => {
    setPrefs(novo);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(novo));
    } catch {
      // ignora falha de persistencia
    }
  }, []);

  // Preferencias de tema/acessibilidade nao sao sensiveis (ao contrario dos
  // tokens de auth): usar localStorage aqui e uma decisao separada, para
  // nao perder a escolha da professora a cada F5.
  useEffect(() => {
    // Le do localStorage (sistema externo) so apos o mount, de proposito:
    // localStorage nao existe no SSR, e usar isso num inicializador
    // preguicoso do useState quebraria a hidratacao (server sem tema salvo
    // x client com tema salvo). Excecao legitima a regra set-state-in-effect.
    try {
      // specs/057: o que estiver salvo (inclusive cor livre/modo Personalizado
      // da spec 051) vira uma opcao da paleta; regrava se precisou converter.
      const { preferencias, mudou } = migrarPreferencias(localStorage.getItem(STORAGE_KEY));
      if (mudou) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(preferencias));
      }
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setPrefs(preferencias);
    } catch {
      // ambiente sem localStorage (SSR, modo privado restrito): mantem o padrao
    }
  }, []);

  // specs/057: os tokens vao no <html> para valer em todas as telas e camadas
  // (o aviso flutuante e a tela de login ficam fora do AppShell).
  useEffect(() => {
    const raiz = document.documentElement;
    for (const [nome, valor] of Object.entries(tokensDoTema(prefs))) {
      raiz.style.setProperty(nome, valor);
    }
    raiz.classList.toggle("dark-mode", prefs.dark);
  }, [prefs]);

  const toggleDark = useCallback(() => persistir({ ...prefs, dark: !prefs.dark }), [prefs, persistir]);
  const setDark = useCallback((dark: boolean) => persistir({ ...prefs, dark }), [prefs, persistir]);
  const setColorMode = useCallback((colorMode: ColorMode) => persistir({ ...prefs, colorMode }), [prefs, persistir]);
  const setDestaque = useCallback((destaque: string) => persistir({ ...prefs, destaque }), [prefs, persistir]);

  const value = useMemo(
    () => ({ ...prefs, toggleDark, setDark, setColorMode, setDestaque }),
    [prefs, toggleDark, setDark, setColorMode, setDestaque]
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
