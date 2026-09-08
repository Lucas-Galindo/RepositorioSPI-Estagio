"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterPagamento, atualizarStatusPagamento, type Pagamento } from "@/lib/api/pagamentos";
import { currency, fmtData, fmtMesAno } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

const STATUS_OPCOES = ["Pendente", "Pago", "Atrasado", "Cancelado"] as const;

export default function ContaAReceberDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Financeiro", "Detalhes da conta a receber");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  const [pagamento, setPagamento] = useState<Pagamento | null>(null);
  const [erro, setErro] = useState("");
  const [processando, setProcessando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    obterPagamento(Number(id), sessao.accessToken)
      .then(setPagamento)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Conta a receber não encontrada."));
  }, [sessao, id]);

  const handleStatus = async (novoStatus: (typeof STATUS_OPCOES)[number]) => {
    if (!sessao || !pagamento) return;
    setProcessando(true);
    try {
      const atualizado = await atualizarStatusPagamento(pagamento.id, novoStatus, sessao.accessToken);
      setPagamento(atualizado);
      mostrarToast(`Status atualizado para ${novoStatus}.`);
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível atualizar o status.");
    } finally {
      setProcessando(false);
    }
  };

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!pagamento) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <Link href="/financeiro/contas-a-receber" className="breadcrumb">
        <Icon name="back" size={13} /> Contas a Receber
      </Link>

      <div className="entity-hero">
        <div className="avatar-lg" style={{ background: "var(--c-accent2-tint)", color: "var(--c-accent2)" }}>
          <Icon name="wallet" size={24} />
        </div>
        <div>
          <h2>{currency(pagamento.valorFinal)}</h2>
          <div className="meta">
            <Link href={`/alunos/${pagamento.alunoId}`} style={{ color: "inherit" }}>
              {pagamento.alunoNome}
            </Link>
            ·<span>{pagamento.formaPagamentoNome ?? "Forma não definida"}</span>·<StatusPill status={pagamento.status} />
          </div>
        </div>
        <Link href={`/financeiro/contas-a-receber/${pagamento.id}/editar`} className="btn btn-ghost btn-sm" style={{ marginLeft: "auto" }}>
          <Icon name="edit" size={13} /> Editar
        </Link>
      </div>

      <div className="grid-2b">
        <div className="mini-panel">
          <h4>
            <Icon name="cal" size={15} /> Detalhes
          </h4>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Descrição</div>
              <div className="who">{pagamento.descricao ?? "—"}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Categoria</div>
              <div className="who">{pagamento.categoriaReceitaNome ?? "—"}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Vencimento</div>
              <div className="who">{fmtData(pagamento.dataVencimento)}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Pagamento</div>
              <div className="who">{pagamento.dataPagamento ? fmtData(pagamento.dataPagamento) : "Ainda não pago"}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Competência</div>
              <div className="who">{pagamento.competencia ? fmtMesAno(pagamento.competencia) : "—"}</div>
            </div>
          </div>
          {pagamento.aulaIds.length > 0 && (
            <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
              <div className="lesson-info">
                <div className="subj">Aulas cobertas</div>
                <div className="who">{pagamento.aulaIds.length} aula(s)</div>
              </div>
            </div>
          )}
          {pagamento.observacoes && (
            <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
              <div className="lesson-info">
                <div className="subj">Observações</div>
                <div className="who">{pagamento.observacoes}</div>
              </div>
            </div>
          )}
        </div>

        <div className="mini-panel">
          <h4>
            <Icon name="edit" size={15} /> Alterar status
          </h4>
          <div className="checkbox-list">
            {STATUS_OPCOES.map((s) => (
              <button
                key={s}
                type="button"
                className={`btn btn-sm ${pagamento.status === s ? "btn-primary" : "btn-ghost"}`}
                disabled={processando || pagamento.status === s}
                onClick={() => handleStatus(s)}
              >
                {s}
              </button>
            ))}
          </div>
          <p className="hint" style={{ marginTop: 12 }}>
            Ao marcar como Pago, a data de pagamento é preenchida automaticamente. Cancelar não exclui o registro, apenas encerra a cobrança.
          </p>
        </div>
      </div>
    </>
  );
}
