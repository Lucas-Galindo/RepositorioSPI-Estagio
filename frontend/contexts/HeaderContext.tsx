"use client";

import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

interface HeaderInfo {
  title: string;
  sub: string;
}

interface HeaderContextValue extends HeaderInfo {
  setHeader: (info: HeaderInfo) => void;
}

const HeaderContext = createContext<HeaderContextValue | undefined>(undefined);

export function HeaderProvider({ children }: { children: ReactNode }) {
  const [info, setInfo] = useState<HeaderInfo>({ title: "Home", sub: "Central operacional da sua rotina" });

  const value = useMemo(() => ({ ...info, setHeader: setInfo }), [info]);

  return <HeaderContext.Provider value={value}>{children}</HeaderContext.Provider>;
}

export function useHeader(): HeaderContextValue {
  const context = useContext(HeaderContext);
  if (!context) {
    throw new Error("useHeader precisa ser usado dentro de um HeaderProvider.");
  }

  return context;
}
