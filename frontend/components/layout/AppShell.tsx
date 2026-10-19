"use client";

import type { ReactNode } from "react";
import { Header } from "./Header";
import { Sidebar } from "./Sidebar";

// specs/057: as cores do tema (modo, daltonismo, destaque) sao aplicadas no
// <html> pelo ThemeProvider, nao mais aqui.
export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="app">
      <Sidebar />
      <div className="main">
        <Header />
        <div className="content">{children}</div>
      </div>
    </div>
  );
}
