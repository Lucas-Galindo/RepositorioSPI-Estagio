"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import {
  cadastrarProfessorInicial,
  obterProfessorComoAdmin,
  atualizarProfessorComoAdmin,
  solicitarExclusaoProfessor,
  confirmarExclusaoProfessor,
  reativarProfessor,
  type Professor,
} from "@/lib/api/professor";

// Sistema single-tenant: so existe uma professora. Ao abrir a tela, o
// Admin busca se ela ja existe (GET /api/professor/admin) -- se existir,
// so pode editar Nome/Email/Telefone (CPF fica visivel mas bloqueado,
// mesma regra do "Meu Perfil" da propria professora); se nao existir,
// mostra o formulario de cadastro original.
export function CadastrarProfessoraForm() {
  const { sessao } = useAuth();

  const [carregando, setCarregando] = useState(true);
  const [existente, setExistente] = useState<Professor | null>(null);
  const [erroCarregamento, setErroCarregamento] = useState("");

  useEffect(() => {
    if (!sessao) return;
    obterProfessorComoAdmin(sessao.accessToken)
      .then((professor) => setExistente(professor))
      .catch((excecao) => {
        if (excecao instanceof ApiError && excecao.status === 404) {
          setExistente(null);
        } else {
          setErroCarregamento(excecao instanceof ApiError ? excecao.message : "Não foi possível verificar o cadastro da professora.");
        }
      })
      .finally(() => setCarregando(false));
  }, [sessao]);

  if (carregando) {
    return <p className="count-text">Carregando...</p>;
  }

  if (erroCarregamento) {
    return (
      <div className="form-wrap">
        <div className="err-banner show">
          <Icon name="warn" size={15} />
          <span>{erroCarregamento}</span>
        </div>
      </div>
    );
  }

  return existente ? (
    <EditarProfessoraForm professor={existente} onAtualizado={setExistente} />
  ) : (
    <CriarProfessoraForm onCriada={setExistente} />
  );
}

function CriarProfessoraForm({ onCriada }: { onCriada: (professor: Professor) => void }) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  const [nome, setNome] = useState("");
  const [cpf, setCpf] = useState("");
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [telefone, setTelefone] = useState("");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

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
      onCriada(resultado);
      mostrarToast("Professora cadastrada com sucesso.");
    } catch (excecao) {
      setErros(
        excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível cadastrar a professora."]
      );
    } finally {
      setSalvando(false);
    }
  };

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

type EtapaExclusao = "inicial" | "aviso" | "codigo";

function EditarProfessoraForm({
  professor,
  onAtualizado,
}: {
  professor: Professor;
  onAtualizado: (professor: Professor) => void;
}) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  const [nome, setNome] = useState(professor.nome);
  const [email, setEmail] = useState(professor.email);
  const [telefone, setTelefone] = useState(professor.telefone ?? "");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  const [etapaExclusao, setEtapaExclusao] = useState<EtapaExclusao>("inicial");
  const [codigoExclusao, setCodigoExclusao] = useState("");
  const [erroExclusao, setErroExclusao] = useState("");
  const [processandoExclusao, setProcessandoExclusao] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    try {
      const resultado = await atualizarProfessorComoAdmin({ nome, email, telefone: telefone || null }, sessao.accessToken);
      onAtualizado(resultado);
      mostrarToast("Cadastro da professora atualizado com sucesso.");
    } catch (excecao) {
      setErros(
        excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível atualizar o cadastro."]
      );
    } finally {
      setSalvando(false);
    }
  };

  const handleSolicitarExclusao = async () => {
    if (!sessao) return;
    setErroExclusao("");
    setProcessandoExclusao(true);

    try {
      await solicitarExclusaoProfessor(sessao.accessToken);
      setEtapaExclusao("codigo");
    } catch (excecao) {
      setErroExclusao(excecao instanceof ApiError ? excecao.message : "Não foi possível enviar o código de verificação.");
    } finally {
      setProcessandoExclusao(false);
    }
  };

  const handleConfirmarExclusao = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao || !codigoExclusao.trim()) return;

    setErroExclusao("");
    setProcessandoExclusao(true);

    try {
      await confirmarExclusaoProfessor({ codigo: codigoExclusao.trim() }, sessao.accessToken);
      onAtualizado({ ...professor, ativo: false });
      mostrarToast("Professora excluída. Reative-a a qualquer momento para restaurar o acesso.");
      setEtapaExclusao("inicial");
      setCodigoExclusao("");
    } catch (excecao) {
      setErroExclusao(excecao instanceof ApiError ? excecao.message : "Não foi possível confirmar a exclusão.");
    } finally {
      setProcessandoExclusao(false);
    }
  };

  const handleReativar = async () => {
    if (!sessao) return;
    setErroExclusao("");
    setProcessandoExclusao(true);

    try {
      const resultado = await reativarProfessor(sessao.accessToken);
      onAtualizado(resultado);
      mostrarToast("Professora reativada com sucesso.");
    } catch (excecao) {
      setErroExclusao(excecao instanceof ApiError ? excecao.message : "Não foi possível reativar a professora.");
    } finally {
      setProcessandoExclusao(false);
    }
  };

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
          <p className="hint" style={{ marginTop: -6, marginBottom: 15 }}>
            Cadastro único do sistema — já existe uma professora cadastrada. Você pode atualizar os dados abaixo.
          </p>
          <div className="field-grid two">
            <div className="field">
              <label>
                Nome completo<span className="req">*</span>
              </label>
              <input required value={nome} onChange={(e) => setNome(e.target.value)} placeholder="Elaine Mello" />
            </div>
            <div className="field">
              <label>CPF</label>
              <input value={professor.cpf} disabled title="CPF não pode ser alterado" />
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
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : "Salvar alterações"}
          </button>
        </div>
      </form>

      <div className="form-section" style={{ marginTop: 24, borderTop: "1px solid var(--border, #e5e5e5)", paddingTop: 20 }}>
        <div className="fs-title">
          <span className="fs-num">2</span> Zona de risco
        </div>

        {erroExclusao && (
          <div className="err-banner show" style={{ marginBottom: 15 }}>
            <Icon name="warn" size={15} />
            <span>{erroExclusao}</span>
          </div>
        )}

        {!professor.ativo && (
          <>
            <p className="hint" style={{ marginBottom: 15 }}>
              Esta professora está <strong>inativa</strong> — ela não consegue logar no sistema. Reative para restaurar o
              acesso normal (nenhum dado foi perdido).
            </p>
            <button type="button" className="btn btn-primary" onClick={handleReativar} disabled={processandoExclusao}>
              {processandoExclusao ? "Reativando..." : "Reativar Professor"}
            </button>
          </>
        )}

        {professor.ativo && etapaExclusao === "inicial" && (
          <>
            <p className="hint" style={{ marginBottom: 15 }}>
              Excluir a professora desativa o cadastro dela (ela deixa de conseguir logar), mas preserva todo o
              histórico (turmas, alunos, aulas e pagamentos). A ação exige confirmação por um código enviado ao seu
              próprio e-mail de Admin.
            </p>
            <button type="button" className="btn btn-danger" onClick={() => setEtapaExclusao("aviso")}>
              Excluir Professor
            </button>
          </>
        )}

        {professor.ativo && etapaExclusao === "aviso" && (
          <>
            <div className="err-banner show" style={{ marginBottom: 15 }}>
              <Icon name="warn" size={15} />
              <span>
                <strong>Atenção:</strong> este sistema hoje tem só esta professora cadastrada. Ao excluí-la, o sistema
                fica inacessível para o uso normal (login da professora, alunos e aulas) até que o Admin cadastre uma
                nova professora. Isso é diferente de um aviso comum de "ação irreversível" — aqui, todo mundo fica sem
                acesso até você cadastrar alguém de novo.
              </span>
            </div>
            <div style={{ display: "flex", gap: 10 }}>
              <button type="button" className="btn btn-danger" onClick={handleSolicitarExclusao} disabled={processandoExclusao}>
                {processandoExclusao ? "Enviando código..." : "Entendi, enviar código de confirmação"}
              </button>
              <button type="button" className="btn btn-ghost" onClick={() => setEtapaExclusao("inicial")} disabled={processandoExclusao}>
                Cancelar
              </button>
            </div>
          </>
        )}

        {professor.ativo && etapaExclusao === "codigo" && (
          <form onSubmit={handleConfirmarExclusao}>
            <p className="hint" style={{ marginBottom: 15 }}>
              Enviamos um código de verificação para o seu e-mail de Admin. Ele expira em 15 minutos e só pode ser
              usado uma vez.
            </p>
            <div className="field" style={{ marginBottom: 15 }}>
              <label htmlFor="input-codigo-exclusao">Código recebido por e-mail</label>
              <input
                id="input-codigo-exclusao"
                type="text"
                placeholder="Cole o código aqui"
                value={codigoExclusao}
                onChange={(e) => setCodigoExclusao(e.target.value)}
              />
            </div>
            <div style={{ display: "flex", gap: 10 }}>
              <button type="submit" className="btn btn-danger" disabled={processandoExclusao || !codigoExclusao.trim()}>
                {processandoExclusao ? "Confirmando..." : "Confirmar exclusão"}
              </button>
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => {
                  setEtapaExclusao("inicial");
                  setCodigoExclusao("");
                  setErroExclusao("");
                }}
                disabled={processandoExclusao}
              >
                Cancelar
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
