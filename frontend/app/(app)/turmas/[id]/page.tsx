"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterTurma, excluirTurma, vincularAluno, desvincularAluno, type Turma } from "@/lib/api/turmas";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { avatarColor, initials, fmtData, fmtHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function TurmaDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Turmas", "Detalhes da turma");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [turma, setTurma] = useState<Turma | null>(null);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [alunosDisponiveis, setAlunosDisponiveis] = useState<Aluno[]>([]);
  const [alunoParaVincular, setAlunoParaVincular] = useState("");
  const [erro, setErro] = useState("");
  const [confirmandoExclusao, setConfirmandoExclusao] = useState(false);
  const [excluindo, setExcluindo] = useState(false);
  const [processando, setProcessando] = useState(false);

  const carregar = () => {
    if (!sessao) return;
    const turmaId = Number(id);

    Promise.all([
      obterTurma(turmaId, sessao.accessToken),
      listarAulas(sessao.accessToken, { turmaId }),
      listarAlunos(sessao.accessToken, { ativo: true }),
    ])
      .then(([t, aulasT, todosAlunos]) => {
        setTurma(t);
        setAulas(aulasT);
        setAlunosDisponiveis(todosAlunos.filter((a) => !t.alunos.some((ta) => ta.id === a.id)));
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Turma não encontrada."));
  };

  useEffect(carregar, [sessao, id]);

  const handleVincular = async () => {
    if (!sessao || !alunoParaVincular) return;
    setProcessando(true);
    try {
      await vincularAluno(Number(id), Number(alunoParaVincular), sessao.accessToken);
      mostrarToast("Aluno vinculado à turma.");
      setAlunoParaVincular("");
      carregar();
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível vincular o aluno.");
    } finally {
      setProcessando(false);
    }
  };

  const handleDesvincular = async (alunoId: number) => {
    if (!sessao) return;
    setProcessando(true);
    try {
      await desvincularAluno(Number(id), alunoId, sessao.accessToken);
      mostrarToast("Aluno desvinculado da turma.");
      carregar();
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível desvincular o aluno.");
    } finally {
      setProcessando(false);
    }
  };

  const handleExcluir = async () => {
    if (!sessao || !turma) return;
    setExcluindo(true);
    try {
      await excluirTurma(turma.id, sessao.accessToken);
      mostrarToast("Turma excluída com sucesso.");
      router.push("/turmas");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir a turma.");
      setConfirmandoExclusao(false);
    } finally {
      setExcluindo(false);
    }
  };

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!turma) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <Link href="/turmas" className="breadcrumb">
        <Icon name="back" size={13} /> Turmas
      </Link>

      <div className="entity-hero">
        <div className="avatar-lg" style={{ background: "var(--c-sidebar)" }}>
          <Icon name="users" size={24} />
        </div>
        <div>
          <h2>{turma.nome}</h2>
          <div className="meta">
            <span>{turma.alunos.length} aluno(s)</span>·<StatusPill status={turma.ativo ? "Ativo" : "Inativo"} />
          </div>
        </div>
        <div className="actions">
          <Link href={`/turmas/${turma.id}/editar`} className="btn btn-ghost btn-sm">
            <Icon name="edit" size={13} /> Editar
          </Link>
          <button className="btn btn-danger btn-sm" type="button" onClick={() => setConfirmandoExclusao(true)}>
            <Icon name="trash" size={13} /> Excluir
          </button>
        </div>
      </div>

      <div className="grid-2b">
        <div className="mini-panel">
          <h4>
            <Icon name="users" size={15} /> Alunos vinculados
          </h4>
          {turma.alunos.length ? (
            turma.alunos.map((a) => (
              <div className="lesson-item" key={a.id} style={{ cursor: "default" }}>
                <div className="mini-avatar" style={{ background: avatarColor(a.id) }}>
                  {initials(a.nome)}
                </div>
                <div className="lesson-info">
                  <Link href={`/alunos/${a.id}`} className="subj" style={{ textDecoration: "none", color: "inherit" }}>
                    {a.nome}
                  </Link>
                  <div className="who">RA {a.ra}</div>
                </div>
                <button
                  className="btn btn-ghost btn-sm"
                  type="button"
                  disabled={processando}
                  onClick={() => handleDesvincular(a.id)}
                >
                  <Icon name="x" size={12} /> Remover
                </button>
              </div>
            ))
          ) : (
            <EmptyState title="Nenhum aluno vinculado" desc="Vincule alunos a esta turma usando o campo abaixo." />
          )}

          {alunosDisponiveis.length > 0 && (
            <div className="field-row" style={{ marginTop: 14, gap: 8 }}>
              <select
                className="filter-select"
                style={{ flex: 1, maxWidth: "none" }}
                value={alunoParaVincular}
                onChange={(e) => setAlunoParaVincular(e.target.value)}
              >
                <option value="">Selecione um aluno...</option>
                {alunosDisponiveis.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.nome} (RA {a.ra})
                  </option>
                ))}
              </select>
              <button className="btn btn-primary btn-sm" type="button" disabled={!alunoParaVincular || processando} onClick={handleVincular}>
                <Icon name="plus" size={13} /> Vincular
              </button>
            </div>
          )}
        </div>

        <div className="mini-panel">
          <h4>
            <Icon name="cal" size={15} /> Próximas aulas
          </h4>
          {aulas.length ? (
            aulas.slice(0, 6).map((a) => (
              <Link href={`/aulas/${a.id}`} className="lesson-item" key={a.id}>
                <div className="lesson-time">{fmtHora(a.horaInicio)}</div>
                <div className="lesson-info">
                  <div className="subj">{a.materiaNome}</div>
                  <div className="who">{fmtData(a.dataInicio)}</div>
                </div>
                <StatusBadge status={a.status} />
              </Link>
            ))
          ) : (
            <EmptyState title="Nenhuma aula" desc="Nenhuma aula foi cadastrada para esta turma ainda." />
          )}
        </div>
      </div>

      {confirmandoExclusao && (
        <ConfirmModal
          titulo="Excluir turma"
          descricao={`Tem certeza que deseja excluir ${turma.nome}? O histórico de aulas é preservado.`}
          onConfirmar={handleExcluir}
          onCancelar={() => setConfirmandoExclusao(false)}
          confirmando={excluindo}
        />
      )}
    </>
  );
}
