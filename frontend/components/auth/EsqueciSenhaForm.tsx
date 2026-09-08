"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { esqueciSenha, redefinirSenha } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

type Etapa = "email" | "token" | "concluido";

export function EsqueciSenhaForm() {
  const [etapa, setEtapa] = useState<Etapa>("email");
  const [email, setEmail] = useState("");
  const [token, setToken] = useState("");
  const [novaSenha, setNovaSenha] = useState("");
  const [confirmarSenha, setConfirmarSenha] = useState("");
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const [erro, setErro] = useState("");
  const [carregando, setCarregando] = useState(false);

  const handleSolicitar = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!email.trim()) {
      setErro("Informe o e-mail cadastrado.");
      return;
    }

    setErro("");
    setCarregando(true);

    try {
      // A resposta e sempre a mesma, exista ou nao o e-mail (nunca revela
      // quais contas existem) -- por isso sempre avancamos para a proxima
      // etapa quando a chamada em si tem sucesso.
      await esqueciSenha({ email: email.trim() });
      setEtapa("token");
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

  const handleRedefinir = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!token.trim() || !novaSenha.trim()) {
      setErro("Preencha o código recebido por e-mail e a nova senha.");
      return;
    }
    if (novaSenha !== confirmarSenha) {
      setErro("As senhas não coincidem.");
      return;
    }

    setErro("");
    setCarregando(true);

    try {
      await redefinirSenha({ token: token.trim(), novaSenha });
      setEtapa("concluido");
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

  if (etapa === "concluido") {
    return (
      <>
        <div className="form-head">
          <h1>Senha redefinida</h1>
          <p>Sua senha foi alterada com sucesso. Todas as outras sessões foram encerradas por segurança.</p>
        </div>
        <Link href="/" className="btn-submit" style={{ marginTop: 24, textDecoration: "none" }}>
          Ir para o login
        </Link>
      </>
    );
  }

  if (etapa === "token") {
    return (
      <>
        <div className="form-head">
          <h1>Digite o código</h1>
          <p>Se o e-mail informado estiver cadastrado, enviamos um código de recuperação para ele. Ele expira em 15 minutos.</p>
        </div>

        <div className={`err-banner${erro ? " show" : ""}`}>
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v5M12 16h.01" />
          </svg>
          <span>{erro}</span>
        </div>

        <form onSubmit={handleRedefinir}>
          <div className="field">
            <label htmlFor="input-token">Código recebido por e-mail</label>
            <div className="input-wrap">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="11" width="18" height="10" rx="2" />
                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              <input
                id="input-token"
                type="text"
                placeholder="Cole o código aqui"
                value={token}
                onChange={(event) => setToken(event.target.value)}
              />
            </div>
          </div>

          <div className="field">
            <label htmlFor="input-nova-senha">Nova senha</label>
            <div className="input-wrap">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="11" width="18" height="10" rx="2" />
                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              <input
                id="input-nova-senha"
                type={mostrarSenha ? "text" : "password"}
                placeholder="Mínimo 8 caracteres, alfanumérica"
                autoComplete="new-password"
                value={novaSenha}
                onChange={(event) => setNovaSenha(event.target.value)}
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

          <div className="field">
            <label htmlFor="input-confirmar-senha">Confirmar nova senha</label>
            <div className="input-wrap">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="11" width="18" height="10" rx="2" />
                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              <input
                id="input-confirmar-senha"
                type={mostrarSenha ? "text" : "password"}
                placeholder="Repita a nova senha"
                autoComplete="new-password"
                value={confirmarSenha}
                onChange={(event) => setConfirmarSenha(event.target.value)}
              />
            </div>
          </div>

          <button type="submit" className="btn-submit" disabled={carregando}>
            {carregando ? "Redefinindo..." : "Redefinir senha"}
          </button>
        </form>

        <p className="form-foot">
          Não recebeu o código?{" "}
          <button className="link" type="button" onClick={() => setEtapa("email")}>
            Tentar outro e-mail
          </button>
        </p>
      </>
    );
  }

  return (
    <>
      <div className="form-head">
        <h1>Esqueceu sua senha?</h1>
        <p>Informe o e-mail cadastrado da professora. Enviaremos um código de recuperação.</p>
      </div>

      <div className={`err-banner${erro ? " show" : ""}`}>
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="9" />
          <path d="M12 8v5M12 16h.01" />
        </svg>
        <span>{erro}</span>
      </div>

      <form onSubmit={handleSolicitar}>
        <div className="field">
          <label htmlFor="input-email-recuperacao">E-mail</label>
          <div className="input-wrap">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="2" y="4" width="20" height="16" rx="2.5" />
              <path d="m2 7 10 6 10-6" />
            </svg>
            <input
              id="input-email-recuperacao"
              type="email"
              placeholder="elaine.mello@spi.com"
              autoComplete="username"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>
        </div>

        <button type="submit" className="btn-submit" disabled={carregando}>
          {carregando ? "Enviando..." : "Enviar código de recuperação"}
        </button>
      </form>

      <div className="divider">ou</div>
      <p className="form-foot">
        <Link href="/" className="link">
          Voltar para o login
        </Link>
      </p>
    </>
  );
}
