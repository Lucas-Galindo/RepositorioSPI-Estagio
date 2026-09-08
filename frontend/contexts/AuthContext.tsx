"use client";

import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import type { LoginResponse, Perfil } from "@/lib/api/auth";
import { extrairNomeDoToken } from "@/lib/jwt";

export interface Sessao {
  perfil: Perfil;
  nome: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiraEm: string;
}

interface AuthContextValue {
  sessao: Sessao | null;
  definirSessao: (resposta: LoginResponse) => void;
  encerrarSessao: () => void;
}

// Guardado somente em memoria (estado do Context): nada e persistido em
// localStorage/sessionStorage. Ao recarregar a pagina a sessao se perde e
// e preciso logar de novo -- decisao deliberada para esta fase do projeto.
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [sessao, setSessao] = useState<Sessao | null>(null);

  const definirSessao = useCallback((resposta: LoginResponse) => {
    setSessao({
      perfil: resposta.perfil,
      nome: extrairNomeDoToken(resposta.accessToken) ?? resposta.perfil,
      accessToken: resposta.accessToken,
      refreshToken: resposta.refreshToken,
      accessTokenExpiraEm: resposta.accessTokenExpiraEm,
    });
  }, []);

  const encerrarSessao = useCallback(() => setSessao(null), []);

  const value = useMemo(
    () => ({ sessao, definirSessao, encerrarSessao }),
    [sessao, definirSessao, encerrarSessao]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth precisa ser usado dentro de um AuthProvider.");
  }

  return context;
}
