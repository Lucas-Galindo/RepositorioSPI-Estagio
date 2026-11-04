"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import type { LoginResponse, Perfil } from "@/lib/api/auth";
import { refresh, logout as logoutApi } from "@/lib/api/auth";
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
  /** true enquanto tenta restaurar a sessao a partir do refresh token salvo (ver "Manter conectada"). */
  carregando: boolean;
  definirSessao: (resposta: LoginResponse, manterConectada?: boolean) => void;
  encerrarSessao: () => void;
}

// Guardado somente em memoria por padrao (estado do Context): ao recarregar
// a pagina a sessao se perde e e preciso logar de novo. Quando a professora
// marca "Manter conectada" no login, o refresh token (nao o access token,
// que e curto e sensivel) e salvo aqui para restaurar a sessao no proximo
// carregamento via POST /api/auth/refresh.
const CHAVE_REFRESH_TOKEN = "spi.refreshToken";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function montarSessao(resposta: LoginResponse): Sessao {
  return {
    perfil: resposta.perfil,
    nome: extrairNomeDoToken(resposta.accessToken) ?? resposta.perfil,
    accessToken: resposta.accessToken,
    refreshToken: resposta.refreshToken,
    accessTokenExpiraEm: resposta.accessTokenExpiraEm,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [sessao, setSessao] = useState<Sessao | null>(null);
  const [carregando, setCarregando] = useState(true);
  // Copia da sessao para quem roda fora da renderizacao (encerrarSessao); atualizada depois de cada commit.
  const sessaoRef = useRef<Sessao | null>(null);
  useEffect(() => {
    sessaoRef.current = sessao;
  }, [sessao]);

  // Restaura a sessao salva ("Manter conectada"). Sem token salvo, so termina o carregamento; o estado so muda
  // dentro do .finally (nunca de forma sincrona no corpo do efeito).
  useEffect(() => {
    const tokenSalvo = localStorage.getItem(CHAVE_REFRESH_TOKEN);

    const restaurada = tokenSalvo
      ? refresh(tokenSalvo)
          .then((resposta) => {
            setSessao(montarSessao(resposta));
            // Rotacao: o backend revoga o token usado e emite um novo a cada troca.
            localStorage.setItem(CHAVE_REFRESH_TOKEN, resposta.refreshToken);
          })
          .catch(() => {
            localStorage.removeItem(CHAVE_REFRESH_TOKEN);
          })
      : Promise.resolve();

    restaurada.finally(() => setCarregando(false));
  }, []);

  const definirSessao = useCallback((resposta: LoginResponse, manterConectada = false) => {
    setSessao(montarSessao(resposta));
    if (manterConectada) {
      localStorage.setItem(CHAVE_REFRESH_TOKEN, resposta.refreshToken);
    } else {
      localStorage.removeItem(CHAVE_REFRESH_TOKEN);
    }
  }, []);

  const encerrarSessao = useCallback(() => {
    const refreshTokenAtual = sessaoRef.current?.refreshToken;
    setSessao(null);
    localStorage.removeItem(CHAVE_REFRESH_TOKEN);
    if (refreshTokenAtual) {
      logoutApi(refreshTokenAtual).catch(() => {});
    }
  }, []);

  const value = useMemo(
    () => ({ sessao, carregando, definirSessao, encerrarSessao }),
    [sessao, carregando, definirSessao, encerrarSessao]
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
