"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterContaPagar, atualizarStatusContaPagar, type ContaPagar } from "@/lib/api/contasPagar";
import { currency, fmtData, fmtMesAno } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

const STATUS_OPCOES = ["Pendente", "Pago", "Atrasado", "Cancelado"] as const;

export default function ContaAPagarDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Financeiro", "Detalhes da conta a pagar");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  const [conta, setConta] = useState<ContaPagar | null>(null);
  const [erro, setErro] = useState("");
  const [processando, setProcessando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    obterContaPagar(Number(id), sessao.accessToken)
      .then(setConta)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Conta a pagar não encontrada."));
  }, [sessao, id]);

  const handleStatus = async (novoStatus: (typeof STATUS_OPCOES)[number]) => {
    if (!sessao || !conta) return;
    setProcessando(true);
    try {
      const atualizada = await atualizarStatusContaPagar(conta.id, novoStatus, sessao.accessToken);
      setConta(atualizada);
      mostrarToast(`Status atualizado para ${novoStatus}.`);
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível atualizar o status.");
    } finally {
      setProcessando(false);
    }
  };

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!conta) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <Link href="/financeiro/contas-a-pagar" className="breadcrumb">
        <Icon name="back" size={13} /> Contas a Pagar
      </Link>

      <div className="entity-hero">
        <div className="avatar-lg" style={{ background: "var(--c-danger-tint)", color: "var(--c-danger)" }}>
          <Icon name="money" size={24} />
        </div>
        <div>
          <h2>{currency(conta.valor)}</h2>
          <div className="meta">
            <span>{conta.categoriaDespesaNome}</span>·<span>{conta.formaPagamentoNome ?? "Forma não definida"}</span>·
            <StatusPill status={conta.status} />
          </div>
        </div>
        <Link href={`/financeiro/contas-a-pagar/${conta.id}/editar`} className="btn btn-ghost btn-sm" style={{ marginLeft: "auto" }}>
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
              <div className="who">{conta.descricao}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Favorecido</div>
              <div className="who">{conta.favorecido ?? "—"}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Vencimento</div>
              <div className="who">{fmtData(conta.dataVencimento)}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Pagamento</div>
              <div className="who">{conta.dataPagamento ? fmtData(conta.dataPagamento) : "Ainda não pago"}</div>
            </div>
          </div>
          <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
            <div className="lesson-info">
              <div className="subj">Competência</div>
              <div className="who">{conta.competencia ? fmtMesAno(conta.competencia) : "—"}</div>
            </div>
          </div>
          {conta.observacoes && (
            <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
              <div className="lesson-info">
                <div className="subj">Observações</div>
                <div className="who">{conta.observacoes}</div>
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
                className={`btn btn-sm ${conta.status === s ? "btn-primary" : "btn-ghost"}`}
                disabled={processando || conta.status === s}
                onClick={() => handleStatus(s)}
              >
                {s}
              </button>
            ))}
          </div>
          <p className="hint" style={{ marginTop: 12 }}>
            Ao marcar como Pago, a data de pagamento é preenchida automaticamente. Cancelar não exclui o registro, apenas encerra a despesa.
          </p>
        </div>
      </div>
    </>
  );
}
