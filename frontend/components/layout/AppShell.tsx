"use client";

import type { CSSProperties, ReactNode } from "react";
import { Header } from "./Header";
import { Sidebar } from "./Sidebar";
import { useTheme } from "@/contexts/ThemeContext";
import { darkenHex, hexToRgba } from "@/lib/color";

export function AppShell({ children }: { children: ReactNode }) {
  const { dark, colorMode, accentColor, customAccent, customAccent2, customAccent3, customDanger } = useTheme();

  const classes = ["app"];
  if (dark) classes.push("dark-mode");
  if (colorMode !== "normal") classes.push(`cb-${colorMode}`);

  // A cor de destaque customizada só se aplica no modo de cores Padrão,
  // para não sobrepor as paletas de acessibilidade (mesma regra do protótipo).
  // O modo Personalizado injeta as 4 variáveis próprias, com os mesmos
  // fatores de alpha já usados para cada uma em tokens.css.
  const style: CSSProperties =
    colorMode === "normal" && accentColor !== "#4ecdc4"
      ? ({
          "--c-accent": accentColor,
          "--c-accent-deep": darkenHex(accentColor, 0.22),
          "--c-accent-tint": hexToRgba(accentColor, 0.15),
        } as CSSProperties)
      : colorMode === "personalizado"
        ? ({
            "--c-accent": customAccent,
            "--c-accent-deep": darkenHex(customAccent, 0.22),
            "--c-accent-tint": hexToRgba(customAccent, 0.15),
            "--c-accent2": customAccent2,
            "--c-accent2-tint": hexToRgba(customAccent2, 0.15),
            "--c-accent3": customAccent3,
            "--c-accent3-tint": hexToRgba(customAccent3, 0.35),
            "--c-danger": customDanger,
            "--c-danger-tint": hexToRgba(customDanger, 0.12),
          } as CSSProperties)
        : {};

  return (
    <div className={classes.join(" ")} style={style}>
      <Sidebar />
      <div className="main">
        <Header />
        <div className="content">{children}</div>
      </div>
    </div>
  );
}
