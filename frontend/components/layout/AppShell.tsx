"use client";

import type { CSSProperties, ReactNode } from "react";
import { Header } from "./Header";
import { Sidebar } from "./Sidebar";
import { useTheme } from "@/contexts/ThemeContext";
import { darkenHex, hexToRgba } from "@/lib/color";

export function AppShell({ children }: { children: ReactNode }) {
  const { dark, colorMode, accentColor } = useTheme();

  const classes = ["app"];
  if (dark) classes.push("dark-mode");
  if (colorMode !== "normal") classes.push(`cb-${colorMode}`);

  // A cor de destaque customizada só se aplica no modo de cores Padrão,
  // para não sobrepor as paletas de acessibilidade (mesma regra do protótipo).
  const style: CSSProperties =
    colorMode === "normal" && accentColor !== "#4ecdc4"
      ? ({
          "--c-accent": accentColor,
          "--c-accent-deep": darkenHex(accentColor, 0.22),
          "--c-accent-tint": hexToRgba(accentColor, 0.15),
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
