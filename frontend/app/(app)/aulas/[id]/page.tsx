"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterAula, excluirAula, registrarSessao, cancelarAula, type Aula } from "@/lib/api/aulas";
import { fmtData, fmtHora, initials, avatarColor } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function AulaDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Aulas", "Detalhes da aula");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [aula, setAula] = useState<Aula | null>(null);
  const [presencas, setPresencas] = useState<Record<number, boolean>>({});
  const [erro, setErro] = useState("");
  const [confirmandoExclusao, setConfirmandoExclusao] = useState(false);
  const [confirmandoCancelamento, setConfirmandoCancelamento] = useState(false);
  const [processando, setProcessando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    obterAula(Number(id), sessao.accessToken)
      .then((a) => {
        setAula(a);
        setPresencas(Object.fromEntries(a.alunos.map((al) => [al.alunoId, true])));
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Aula não encontrada."));
  }, [sessao, id]);

  const handleExcluir = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      await excluirAula(aula.id, sessao.accessToken);
      mostrarToast("Aula excluída com sucesso.");
      router.push("/aulas");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir a aula.");
      setConfirmandoExclusao(false);
    } finally {
      setProcessando(false);
    }
  };

  const handleCancelar = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      const atualizada = await cancelarAula(aula.id, sessao.accessToken);
      setAula(atualizada);
      mostrarToast("Aula cancelada.");
      setConfirmandoCancelamento(false);
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível cancelar a aula.");
    } finally {
      setProcessando(false);
    }
  };

  const handleRegistrarSessao = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      const atualizada = await registrarSessao(aula.id, presencas, sessao.accessToken);
      setAula(atualizada);
      mostrarToast("Sessão registrada. Frequência dos alunos presentes atualizada.");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível registrar a sessão.");
    } finally {
      setProcessando(false);
    }
  };

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!aula) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <Link href="/aulas" className="breadcrumb">
        <Icon name="back" size={13} /> Aulas
      </Link>

      <div className="entity-hero">
        <div className="avatar-lg" style={{ background: "var(--c-accent-tint)", color: "var(--c-accent-deep)" }}>
          <Icon name="cal" size={24} />
        </div>
        <div>
          <h2>{aula.materiaNome}</h2>
          <div className="meta">
            <span>{fmtData(aula.dataInicio)}</span>·
            <span>
              {fmtHora(aula.horaInicio)} — {fmtHora(aula.horaFim)}
            </span>
            ·<StatusBadge status={aula.status} />
          </div>
        </div>
        <div className="actions">
          {aula.status === "Agendada" && (
            <>
              <Link href={`/aulas/${aula.id}/editar`} className="btn btn-ghost btn-sm">
                <Icon name="edit" size={13} /> Editar
              </Link>
              <button className="btn btn-danger btn-sm" type="button" onClick={() => setConfirmandoCancelamento(true)}>
                <Icon name="x" size={13} /> Cancelar aula
              </button>
            </>
          )}
          <button className="btn btn-danger btn-sm" type="button" onClick={() => setConfirmandoExclusao(true)}>
            <Icon name="trash" size={13} /> Excluir
          </button>
        </div>
      </div>

      <div className="grid-2b">
        <div className="mini-panel">
          <h4>
            <Icon name="users" size={15} /> {aula.turmaNome ? `Turma: ${aula.turmaNome}` : "Aula individual"}
          </h4>

          {aula.alunos.map((al) => (
            <div className="lesson-item" key={al.alunoId} style={{ cursor: "default" }}>
              <div className="mini-avatar" style={{ background: avatarColor(al.alunoId) }}>
                {initials(al.nome)}
              </div>
              <div className="lesson-info">
                <Link href={`/alunos/${al.alunoId}`} className="subj" style={{ textDecoration: "none", color: "inherit" }}>
                  {al.nome}
                </Link>
                <div className="who">RA {al.ra}</div>
              </div>

              {aula.status === "Agendada" ? (
                <label className="chk-pill">
                  <input
                    type="checkbox"
                    checked={presencas[al.alunoId] ?? true}
                    onChange={(e) => setPresencas({ ...presencas, [al.alunoId]: e.target.checked })}
                  />
                  Presente
                </label>
              ) : al.presente === null ? (
                <span className="status-pill status-inativo">—</span>
              ) : (
                <span className={`status-pill ${al.presente ? "status-ativo" : "status-atrasado"}`}>
                  {al.presente ? "Presente" : "Faltou"}
                </span>
              )}
            </div>
          ))}

          {aula.status === "Agendada" && aula.alunos.length > 0 && (
            <button
              className="btn btn-primary btn-sm"
              type="button"
              style={{ marginTop: 14 }}
              disabled={processando}
              onClick={handleRegistrarSessao}
            >
              <Icon name="check" size={13} /> Registrar como realizada
            </button>
          )}
        </div>

        {aula.descricao && (
          <div className="mini-panel">
            <h4>Descrição</h4>
            <p style={{ fontSize: 12.5, color: "var(--c-text-muted)" }}>{aula.descricao}</p>
          </div>
        )}
      </div>

      {confirmandoCancelamento && (
        <ConfirmModal
          titulo="Cancelar aula"
          descricao="A frequência dos alunos não será alterada. Deseja continuar?"
          onConfirmar={handleCancelar}
          onCancelar={() => setConfirmandoCancelamento(false)}
          confirmando={processando}
        />
      )}

      {confirmandoExclusao && (
        <ConfirmModal
          titulo="Excluir aula"
          descricao="Tem certeza que deseja excluir esta aula?"
          onConfirmar={handleExcluir}
          onCancelar={() => setConfirmandoExclusao(false)}
          confirmando={processando}
        />
      )}
    </>
  );
}
