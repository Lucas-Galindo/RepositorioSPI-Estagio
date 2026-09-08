"use client";

import { useTheme } from "@/contexts/ThemeContext";

interface ThemeToggleProps {
  /** Classe do botão — telas diferentes usam estilos diferentes (".theme-toggle" no login, ".icon-round" no dashboard). */
  className: string;
}

export function ThemeToggle({ className }: ThemeToggleProps) {
  const { dark, toggleDark } = useTheme();

  return (
    <button
      className={className}
      onClick={toggleDark}
      title="Alternar modo claro/escuro"
      aria-label="Alternar modo claro/escuro"
      type="button"
    >
      {dark ? (
        <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41" />
        </svg>
      ) : (
        <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79Z" />
        </svg>
      )}
    </button>
  );
}
