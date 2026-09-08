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
import { obterMateria, excluirMateria, type Materia } from "@/lib/api/materias";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { fmtData, fmtHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function MateriaDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Matérias", "Detalhes da matéria");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [materia, setMateria] = useState<Materia | null>(null);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [erro, setErro] = useState("");
  const [confirmandoExclusao, setConfirmandoExclusao] = useState(false);
  const [excluindo, setExcluindo] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    const materiaId = Number(id);

    Promise.all([obterMateria(materiaId, sessao.accessToken), listarAulas(sessao.accessToken)])
      .then(([m, todasAulas]) => {
        setMateria(m);
        setAulas(todasAulas.filter((a) => a.materiaId === materiaId));
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Matéria não encontrada."));
  }, [sessao, id]);

  const handleExcluir = async () => {
    if (!sessao || !materia) return;
    setExcluindo(true);
    try {
      await excluirMateria(materia.id, sessao.accessToken);
      mostrarToast("Matéria excluída com sucesso.");
      router.push("/materias");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir a matéria.");
      setConfirmandoExclusao(false);
    } finally {
      setExcluindo(false);
    }
  };

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!materia) return <p className="count-text">Carregando...</p>;

  const alunosUnicos = new Set(aulas.flatMap((a) => a.alunos.map((al) => al.alunoId)));

  return (
    <>
      <Link href="/materias" className="breadcrumb">
        <Icon name="back" size={13} /> Matérias
      </Link>

      <div className="entity-hero">
        <div className="avatar-lg chip-turq" style={{ background: "var(--c-accent-tint)", color: "var(--c-accent-deep)" }}>
          <Icon name="book" size={24} />
        </div>
        <div>
          <h2>{materia.nome}</h2>
          <div className="meta">
            <span>{materia.nivel ?? "Nível não informado"}</span>·<StatusPill status={materia.ativo ? "Ativo" : "Inativo"} />
          </div>
        </div>
        <div className="actions">
          <Link href={`/materias/${materia.id}/editar`} className="btn btn-ghost btn-sm">
            <Icon name="edit" size={13} /> Editar
          </Link>
          <button className="btn btn-danger btn-sm" type="button" onClick={() => setConfirmandoExclusao(true)}>
            <Icon name="trash" size={13} /> Excluir
          </button>
        </div>
      </div>

      <div className="kpi-row kpi-row-3">
        <div className="kpi">
          <div className="label">
            <Icon name="cal" size={12} /> Aulas cadastradas
          </div>
          <div className="value turq">{aulas.length}</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="users" size={12} /> Alunos atendidos
          </div>
          <div className="value gold">{alunosUnicos.size}</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="check" size={12} /> Aulas realizadas
          </div>
          <div className="value">{aulas.filter((a) => a.status === "Realizada").length}</div>
        </div>
      </div>

      {materia.descricao && (
        <div className="mini-panel" style={{ marginBottom: 18 }}>
          <h4>Descrição</h4>
          <p style={{ fontSize: 12.5, color: "var(--c-text-muted)" }}>{materia.descricao}</p>
        </div>
      )}

      <div className="mini-panel">
        <h4>
          <Icon name="cal" size={15} /> Aulas desta matéria
        </h4>
        {aulas.length ? (
          aulas.slice(0, 8).map((a) => (
            <Link href={`/aulas/${a.id}`} className="lesson-item" key={a.id}>
              <div className="lesson-time">{fmtHora(a.horaInicio)}</div>
              <div className="lesson-info">
                <div className="subj">{fmtData(a.dataInicio)}</div>
                <div className="who">{a.turmaNome ?? a.alunos.map((al) => al.nome).join(", ")}</div>
              </div>
              <StatusBadge status={a.status} />
            </Link>
          ))
        ) : (
          <EmptyState title="Nenhuma aula" desc="Nenhuma aula foi cadastrada para esta matéria ainda." />
        )}
      </div>

      {confirmandoExclusao && (
        <ConfirmModal
          titulo="Excluir matéria"
          descricao={`Tem certeza que deseja excluir ${materia.nome}?`}
          onConfirmar={handleExcluir}
          onCancelar={() => setConfirmandoExclusao(false)}
          confirmando={excluindo}
        />
      )}
    </>
  );
}
