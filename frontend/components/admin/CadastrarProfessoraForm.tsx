"use client";

import { useState, type FormEvent } from "react";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { cadastrarProfessorInicial, type Professor } from "@/lib/api/professor";

export function CadastrarProfessoraForm() {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  const [nome, setNome] = useState("");
  const [cpf, setCpf] = useState("");
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [telefone, setTelefone] = useState("");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);
  const [criada, setCriada] = useState<Professor | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    try {
      const resultado = await cadastrarProfessorInicial(
        { nome, cpf, email, senha, telefone: telefone || null },
        sessao.accessToken
      );
      setCriada(resultado);
      mostrarToast("Professora cadastrada com sucesso.");
    } catch (excecao) {
      setErros(
        excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível cadastrar a professora."]
      );
    } finally {
      setSalvando(false);
    }
  };

  if (criada) {
    return (
      <div className="form-wrap">
        <div className="err-banner show" style={{ background: "var(--c-accent-tint)", color: "var(--c-accent-deep)" }}>
          <Icon name="check" size={15} />
          <span>
            Professora <b>{criada.nome}</b> cadastrada com sucesso. Ela já pode fazer login em{" "}
            <b>{criada.email}</b> com a senha definida.
          </span>
        </div>
      </div>
    );
  }

  return (
    <div className="form-wrap">
      <form onSubmit={handleSubmit}>
        {erros.length > 0 && (
          <div className="err-banner show" style={{ marginBottom: 20 }}>
            <Icon name="warn" size={15} />
            <span>{erros[0]}</span>
          </div>
        )}

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">1</span> Dados da professora
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>
                Nome completo<span className="req">*</span>
              </label>
              <input required value={nome} onChange={(e) => setNome(e.target.value)} placeholder="Elaine Mello" />
            </div>
            <div className="field">
              <label>
                CPF<span className="req">*</span>
              </label>
              <input
                required
                value={cpf}
                onChange={(e) => setCpf(e.target.value)}
                placeholder="000.000.000-00"
                maxLength={14}
              />
            </div>
          </div>
          <div className="field-grid two" style={{ marginTop: 15 }}>
            <div className="field">
              <label>
                E-mail<span className="req">*</span>
              </label>
              <input
                required
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="elaine.mello@spi.com"
              />
            </div>
            <div className="field">
              <label>Telefone</label>
              <input value={telefone} onChange={(e) => setTelefone(e.target.value)} placeholder="18999999999" />
            </div>
          </div>
          <div className="field" style={{ marginTop: 15 }}>
            <label>
              Senha inicial<span className="req">*</span>
            </label>
            <input
              required
              type="password"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              placeholder="Mínimo 8 caracteres, alfanumérica"
              minLength={8}
            />
          </div>
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Cadastrando..." : "Cadastrar professora"}
          </button>
        </div>
      </form>
    </div>
  );
}
