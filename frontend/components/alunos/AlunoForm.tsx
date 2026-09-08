"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import {
  cadastrarAluno,
  atualizarAluno,
  type Aluno,
  type CadastrarAlunoRequest,
  type AtualizarAlunoRequest,
} from "@/lib/api/alunos";

interface AlunoFormProps {
  aluno?: Aluno;
}

export function AlunoForm({ aluno }: AlunoFormProps) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();
  const editando = Boolean(aluno);

  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [nome, setNome] = useState(aluno?.nome ?? "");
  const [cpf, setCpf] = useState(aluno?.cpf ?? "");
  const [telefoneAluno, setTelefoneAluno] = useState(aluno?.telefoneAluno ?? "");
  const [email, setEmail] = useState(aluno?.email ?? "");
  const [senha, setSenha] = useState("");
  const [ehMenorDeIdade, setEhMenorDeIdade] = useState(false);
  const [telefoneResponsavel, setTelefoneResponsavel] = useState(aluno?.telefoneResponsavel ?? "");
  const [emailResponsavel, setEmailResponsavel] = useState(aluno?.emailResponsavel ?? "");
  const [valorAula, setValorAula] = useState(aluno?.valorAula.toString() ?? "");
  const [turmaId, setTurmaId] = useState("");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarTurmas(sessao.accessToken, { ativo: true }).then(setTurmas).catch(() => setTurmas([]));
  }, [sessao]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    try {
      if (editando && aluno) {
        const request: AtualizarAlunoRequest = {
          nome,
          cpf: cpf || null,
          telefoneAluno: telefoneAluno || null,
          telefoneResponsavel: telefoneResponsavel || null,
          email: email || null,
          emailResponsavel: emailResponsavel || null,
          valorAula: Number(valorAula) || 0,
          ehMenorDeIdade,
        };
        const atualizado = await atualizarAluno(aluno.id, request, sessao.accessToken);
        mostrarToast("Aluno atualizado com sucesso.");
        router.push(`/alunos/${atualizado.id}`);
      } else {
        const request: CadastrarAlunoRequest = {
          nome,
          cpf: cpf || null,
          telefoneAluno: telefoneAluno || null,
          telefoneResponsavel: telefoneResponsavel || null,
          email: email || null,
          senha: senha || null,
          emailResponsavel: emailResponsavel || null,
          valorAula: Number(valorAula) || 0,
          turmaId: turmaId ? Number(turmaId) : null,
          ehMenorDeIdade,
        };
        const criado = await cadastrarAluno(request, sessao.accessToken);
        mostrarToast(`Aluno cadastrado com sucesso. RA: ${criado.ra}`);
        router.push(`/alunos/${criado.id}`);
      }
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar o aluno."]);
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
            <span className="fs-num">1</span> Dados do aluno
          </div>
          <div className="field-grid">
            <div className="field">
              <label>
                Nome completo<span className="req">*</span>
              </label>
              <input required value={nome} onChange={(e) => setNome(e.target.value)} />
            </div>
            <div className="field">
              <label>CPF (opcional)</label>
              <input value={cpf} onChange={(e) => setCpf(e.target.value)} placeholder="000.000.000-00" />
            </div>
            <div className="field">
              <label>
                Valor da aula<span className="req">*</span>
              </label>
              <input required type="number" step="0.01" min="0" value={valorAula} onChange={(e) => setValorAula(e.target.value)} placeholder="80.00" />
            </div>
          </div>
        </div>

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">2</span> Contato e acesso próprio
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>Telefone do aluno</label>
              <input value={telefoneAluno} onChange={(e) => setTelefoneAluno(e.target.value)} />
            </div>
            <div className="field">
              <label>E-mail do aluno (login próprio)</label>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
          </div>
          {!editando && (
            <div className="field-grid two" style={{ marginTop: 15 }}>
              <div className="field">
                <label>Senha do aluno</label>
                <input type="password" value={senha} onChange={(e) => setSenha(e.target.value)} placeholder="Mínimo 8 caracteres, alfanumérica" />
                <span className="hint">Só é usada se um e-mail de login for informado.</span>
              </div>
              <div className="field">
                <label>Turma (opcional)</label>
                <select value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
                  <option value="">Atendimento individual</option>
                  {turmas.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.nome}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}
        </div>

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">3</span> Responsável
          </div>
          <div className="chk-pill" style={{ marginBottom: 15, width: "fit-content" }}>
            <input
              type="checkbox"
              id="menor"
              checked={ehMenorDeIdade}
              onChange={(e) => setEhMenorDeIdade(e.target.checked)}
            />
            <label htmlFor="menor">Aluno é menor de idade</label>
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>
                Telefone do responsável{ehMenorDeIdade && <span className="req">*</span>}
              </label>
              <input
                required={ehMenorDeIdade}
                value={telefoneResponsavel}
                onChange={(e) => setTelefoneResponsavel(e.target.value)}
              />
            </div>
            <div className="field">
              <label>
                E-mail do responsável{ehMenorDeIdade && <span className="req">*</span>}
              </label>
              <input
                required={ehMenorDeIdade}
                type="email"
                value={emailResponsavel}
                onChange={(e) => setEmailResponsavel(e.target.value)}
              />
            </div>
          </div>
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={() => router.push(editando && aluno ? `/alunos/${aluno.id}` : "/alunos")}
          >
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : editando ? "Salvar alterações" : "Cadastrar aluno"}
          </button>
        </div>
      </form>
    </div>
  );
}
