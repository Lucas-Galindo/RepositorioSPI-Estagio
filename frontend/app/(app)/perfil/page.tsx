"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterMeuPerfil, atualizarMeuPerfil, alterarMinhaSenha, type Professor } from "@/lib/api/professor";
import { initials } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function PerfilPage() {
  usePageHeader("Meu Perfil", "Seus dados de acesso e contato");
  const { sessao, encerrarSessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [professor, setProfessor] = useState<Professor | null>(null);
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [telefone, setTelefone] = useState("");
  const [errosPerfil, setErrosPerfil] = useState<string[]>([]);
  const [salvandoPerfil, setSalvandoPerfil] = useState(false);

  const [senhaAtual, setSenhaAtual] = useState("");
  const [novaSenha, setNovaSenha] = useState("");
  const [errosSenha, setErrosSenha] = useState<string[]>([]);
  const [salvandoSenha, setSalvandoSenha] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    obterMeuPerfil(sessao.accessToken).then((p) => {
      setProfessor(p);
      setNome(p.nome);
      setEmail(p.email);
      setTelefone(p.telefone ?? "");
    });
  }, [sessao]);

  const handleSalvarPerfil = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErrosPerfil([]);
    setSalvandoPerfil(true);
    try {
      const atualizado = await atualizarMeuPerfil({ nome, email, telefone: telefone || null }, sessao.accessToken);
      setProfessor(atualizado);
      mostrarToast("Perfil atualizado com sucesso.");
    } catch (excecao) {
      setErrosPerfil(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar o perfil."]);
    } finally {
      setSalvandoPerfil(false);
    }
  };

  const handleAlterarSenha = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErrosSenha([]);
    setSalvandoSenha(true);
    try {
      await alterarMinhaSenha({ senhaAtual, novaSenha }, sessao.accessToken);
      mostrarToast("Senha alterada com sucesso. Faça login novamente.");
      setSenhaAtual("");
      setNovaSenha("");
      encerrarSessao();
      router.push("/");
    } catch (excecao) {
      setErrosSenha(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível alterar a senha."]);
    } finally {
      setSalvandoSenha(false);
    }
  };

  const handleSair = () => {
    encerrarSessao();
    router.push("/");
  };

  if (!professor) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <div className="entity-hero">
        <div className="avatar-lg" style={{ background: "var(--c-sidebar)" }}>
          {initials(professor.nome)}
        </div>
        <div>
          <h2>{professor.nome}</h2>
          <div className="meta">
            <span>{professor.email}</span>·<span>CPF {professor.cpf}</span>
          </div>
        </div>
        <div className="actions">
          <button className="btn btn-danger btn-sm" type="button" onClick={handleSair}>
            <Icon name="x" size={13} /> Sair
          </button>
        </div>
      </div>

      <div className="grid-2b">
        <div className="form-wrap">
          <form onSubmit={handleSalvarPerfil}>
            {errosPerfil.length > 0 && (
              <div className="err-banner show" style={{ marginBottom: 20 }}>
                <Icon name="warn" size={15} />
                <span>{errosPerfil[0]}</span>
              </div>
            )}
            <div className="fs-title">
              <span className="fs-num">1</span> Dados pessoais
            </div>
            <div className="field" style={{ marginBottom: 15 }}>
              <label>
                Nome<span className="req">*</span>
              </label>
              <input required value={nome} onChange={(e) => setNome(e.target.value)} />
            </div>
            <div className="field" style={{ marginBottom: 15 }}>
              <label>
                E-mail<span className="req">*</span>
              </label>
              <input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <div className="field">
              <label>Telefone</label>
              <input value={telefone} onChange={(e) => setTelefone(e.target.value)} />
            </div>
            <div className="form-actions">
              <button type="submit" className="btn btn-primary" disabled={salvandoPerfil}>
                {salvandoPerfil ? "Salvando..." : "Salvar alterações"}
              </button>
            </div>
          </form>
        </div>

        <div className="form-wrap">
          <form onSubmit={handleAlterarSenha}>
            {errosSenha.length > 0 && (
              <div className="err-banner show" style={{ marginBottom: 20 }}>
                <Icon name="warn" size={15} />
                <span>{errosSenha[0]}</span>
              </div>
            )}
            <div className="fs-title">
              <span className="fs-num">2</span> Alterar senha
            </div>
            <div className="field" style={{ marginBottom: 15 }}>
              <label>
                Senha atual<span className="req">*</span>
              </label>
              <input required type="password" value={senhaAtual} onChange={(e) => setSenhaAtual(e.target.value)} />
            </div>
            <div className="field">
              <label>
                Nova senha<span className="req">*</span>
              </label>
              <input required type="password" value={novaSenha} onChange={(e) => setNovaSenha(e.target.value)} placeholder="Mínimo 8 caracteres, alfanumérica" />
            </div>
            <div className="form-actions">
              <button type="submit" className="btn btn-primary" disabled={salvandoSenha}>
                {salvandoSenha ? "Salvando..." : "Alterar senha"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
