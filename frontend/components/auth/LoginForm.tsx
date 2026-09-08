"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";
import { login } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

export function LoginForm() {
  const { definirSessao } = useAuth();
  const router = useRouter();
  const [identificador, setIdentificador] = useState("");
  const [senha, setSenha] = useState("");
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const [erro, setErro] = useState("");
  const [carregando, setCarregando] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!identificador.trim() || !senha.trim()) {
      setErro("Preencha e-mail/RA e senha para continuar.");
      return;
    }

    setErro("");
    setCarregando(true);

    try {
      const resposta = await login({ login: identificador.trim(), senha });
      definirSessao(resposta);
      // Admin nao tem acesso as telas da professora (todas Authorize
      // Roles=Professor no backend); sua unica funcao hoje e o cadastro
      // inicial da professora.
      router.push(resposta.perfil === "Admin" ? "/admin/cadastrar-professora" : "/dashboard");
    } catch (excecao) {
      setErro(
        excecao instanceof ApiError
          ? excecao.message
          : "Não foi possível conectar ao servidor. Tente novamente."
      );
    } finally {
      setCarregando(false);
    }
  };

  const handleContact = () => {
    setErro("Entre em contato com a professora responsável para solicitar o cadastro.");
  };

  return (
    <>
      <div className="form-head">
        <h1>Bem-vinda de volta</h1>
        <p>Entre com suas credenciais para acessar o sistema.</p>
      </div>

      <div className={`err-banner${erro ? " show" : ""}`}>
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="9" />
          <path d="M12 8v5M12 16h.01" />
        </svg>
        <span>{erro}</span>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="field">
          <label htmlFor="input-login">E-mail ou RA</label>
          <div className="input-wrap">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="2" y="4" width="20" height="16" rx="2.5" />
              <path d="m2 7 10 6 10-6" />
            </svg>
            <input
              id="input-login"
              type="text"
              placeholder="elaine.mello@spi.com ou 20261001"
              autoComplete="username"
              value={identificador}
              onChange={(event) => setIdentificador(event.target.value)}
            />
          </div>
        </div>

        <div className="field">
          <label htmlFor="input-senha">Senha</label>
          <div className="input-wrap">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="11" width="18" height="10" rx="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
            <input
              id="input-senha"
              type={mostrarSenha ? "text" : "password"}
              placeholder="••••••••••"
              autoComplete="current-password"
              value={senha}
              onChange={(event) => setSenha(event.target.value)}
            />
            <button
              type="button"
              className="toggle-eye"
              onClick={() => setMostrarSenha((valor) => !valor)}
              aria-label={mostrarSenha ? "Ocultar senha" : "Mostrar senha"}
            >
              {mostrarSenha ? (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19M14.12 14.12a3 3 0 1 1-4.24-4.24" />
                  <path d="M1 1l22 22" />
                </svg>
              ) : (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7Z" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
              )}
            </button>
          </div>
        </div>

        <div className="field-row">
          <label className="remember" title="Em breve">
            <input type="checkbox" disabled /> Manter conectada
          </label>
          <Link href="/recuperar-senha" className="link">
            Esqueci minha senha
          </Link>
        </div>

        <button type="submit" className="btn-submit" disabled={carregando}>
          {!carregando && (
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" />
              <path d="M10 17l5-5-5-5" />
              <path d="M15 12H3" />
            </svg>
          )}
          {carregando ? "Entrando..." : "Entrar"}
        </button>
      </form>

      <div className="divider">acesso restrito ao SPI</div>
      <p className="form-foot">
        Ainda não tem cadastro?{" "}
        <button className="link" type="button" onClick={handleContact}>
          Fale com a professora responsável
        </button>
      </p>
    </>
  );
}
