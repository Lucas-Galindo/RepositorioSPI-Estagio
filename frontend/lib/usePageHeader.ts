"use client";

import { useEffect } from "react";
import { useHeader } from "@/contexts/HeaderContext";

/** Define o titulo/subtitulo do header ao montar a pagina. */
export function usePageHeader(title: string, sub: string) {
  const { setHeader } = useHeader();

  useEffect(() => {
    setHeader({ title, sub });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [title, sub]);
}
