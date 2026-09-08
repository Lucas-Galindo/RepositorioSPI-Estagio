"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterAluno, excluirAluno, type Aluno } from "@/lib/api/alunos";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { listarPagamentos, type Pagamento } from "@/lib/api/pagamentos";
import { avatarColor, initials, fmtData, fmtHora, currency } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function AlunoDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Alunos", "Dashboard individual do aluno");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [aluno, setAluno] = useState<Aluno | null>(null);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [pagamentos, setPagamentos] = useState<Pagamento[]>([]);
  const [erro, setErro] = useState("");
  const [confirmandoExclusao, setConfirmandoExclusao] = useState(false);
  const [excluindo, setExcluindo] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    const alunoId = Number(id);

    Promise.all([
      obterAluno(alunoId, sessao.accessToken),
      listarAulas(sessao.accessToken, { alunoId }),
      listarPagamentos(sessao.accessToken, { alunoId }),
    ])
      .then(([a, aulasA, pagamentosA]) => {
        setAluno(a);
        setAulas(aulasA);
        setPagamentos(pagamentosA);
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Aluno não encontrado."));
  }, [sessao, id]);

  const handleExcluir = async () => {
    if (!sessao || !aluno) return;
    setExcluindo(true);
    try {
      await excluirAluno(aluno.id, sessao.accessToken);
      mostrarToast("Aluno excluído com sucesso.");
      router.push("/alunos");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir o aluno.");
      setConfirmandoExclusao(false);
    } finally {
      setExcluindo(false);
    }
  };

  if (erro) {
    return <EmptyState title="Não foi possível carregar" desc={erro} />;
  }

  if (!aluno) {
    return <p className="count-text">Carregando...</p>;
  }

  const aulasOrdenadas = [...aulas].sort((x, y) => (x.dataInicio + x.horaInicio).localeCompare(y.dataInicio + y.horaInicio));
  const hojeIso = new Date().toISOString().slice(0, 10);
  const proxima = aulasOrdenadas.find((a) => a.dataInicio >= hojeIso && a.status === "Agendada");
  const realizadas = aulas.filter((a) => a.status === "Realizada").length;
  const freqPct = realizadas ? Math.round((aluno.frequencia / realizadas) * 100) : null;

  return (
    <>
      <Link href="/alunos" className="breadcrumb">
        <Icon name="back" size={13} /> Alunos
      </Link>

      <div className="entity-hero">
        <div className="avatar-lg" style={{ background: avatarColor(aluno.id) }}>
          {initials(aluno.nome)}
        </div>
        <div>
          <h2>{aluno.nome}</h2>
          <div className="meta">
            <span>RA {aluno.ra}</span>·<span>{currency(aluno.valorAula)}/aula</span>·
            <StatusPill status={aluno.ativo ? "Ativo" : "Inativo"} />
          </div>
        </div>
        <div className="actions">
          <Link href={`/alunos/${aluno.id}/editar`} className="btn btn-ghost btn-sm">
            <Icon name="edit" size={13} /> Editar
          </Link>
          <button className="btn btn-danger btn-sm" type="button" onClick={() => setConfirmandoExclusao(true)}>
            <Icon name="trash" size={13} /> Excluir
          </button>
        </div>
      </div>

      <div className="grid-2b" style={{ marginBottom: 18 }}>
        <div className="mini-panel">
          <h4>
            <Icon name="cal" size={15} /> Próxima aula
          </h4>
          {proxima ? (
            <>
              <div style={{ fontSize: 15, fontWeight: 700 }}>{proxima.materiaNome}</div>
              <div style={{ fontSize: 12.5, color: "var(--c-text-muted)", marginTop: 4 }}>
                {proxima.turmaNome ?? "Atendimento individual"}
              </div>
              <div style={{ fontSize: 12.5, marginTop: 8 }}>
                {fmtData(proxima.dataInicio)} · {fmtHora(proxima.horaInicio)} — {fmtHora(proxima.horaFim)}
              </div>
              <span className="badge badge-confirmed" style={{ marginTop: 10, display: "inline-block" }}>
                {proxima.status}
              </span>
            </>
          ) : (
            <EmptyState title="Nenhuma aula agendada" desc="Este aluno não possui aulas futuras cadastradas no momento." />
          )}
        </div>

        <div className="mini-panel">
          <h4>
            <Icon name="check" size={15} /> Frequência
          </h4>
          <div className="big-value">{freqPct !== null ? `${freqPct}%` : "—"}</div>
          <div className="big-label">
            {aluno.frequencia} presença(s) em {realizadas} aula(s) realizada(s)
          </div>
        </div>
      </div>

      <div className="grid-2b">
        <div className="mini-panel">
          <h4>
            <Icon name="users" size={15} /> Turmas
          </h4>
          {aluno.turmas.length ? (
            aluno.turmas.map((t) => (
              <Link href={`/turmas/${t.id}`} className="lesson-item" key={t.id}>
                <div className="lesson-info">
                  <div className="subj">{t.nome}</div>
                </div>
                <Icon name="fwd" size={14} />
              </Link>
            ))
          ) : (
            <EmptyState title="Atendimento individual" desc="Este aluno não está vinculado a nenhuma turma." />
          )}
        </div>

        <div className="mini-panel">
          <h4>
            <Icon name="wallet" size={15} /> Contas a receber recentes
          </h4>
          {pagamentos.length ? (
            pagamentos.slice(0, 5).map((p) => (
              <Link href={`/financeiro/contas-a-receber/${p.id}`} className="lesson-item" key={p.id}>
                <div className="lesson-time">{currency(p.valorFinal)}</div>
                <div className="lesson-info">
                  <div className="subj">{fmtData(p.dataVencimento)}</div>
                  <div className="who">{p.formaPagamentoNome ?? "Forma não definida"}</div>
                </div>
                <StatusPill status={p.status} />
              </Link>
            ))
          ) : (
            <EmptyState title="Nenhuma conta a receber" desc="Ainda não há contas a receber registradas para este aluno." />
          )}
        </div>
      </div>

      {confirmandoExclusao && (
        <ConfirmModal
          titulo="Excluir aluno"
          descricao={`Tem certeza que deseja excluir ${aluno.nome}? O histórico de aulas e pagamentos é preservado.`}
          onConfirmar={handleExcluir}
          onCancelar={() => setConfirmandoExclusao(false)}
          confirmando={excluindo}
        />
      )}
    </>
  );
}
