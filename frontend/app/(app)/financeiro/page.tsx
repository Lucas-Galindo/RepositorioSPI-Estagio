"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterVisaoGeralFinanceira, type VisaoGeralFinanceira } from "@/lib/api/financeiro";
import { currency, fmtData } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

// Visao Geral: consolida Contas a Receber e Contas a Pagar via um unico
// endpoint agregado de backend (api/financeiro/visao-geral), substituindo a
// soma client-side que existia antes da Sprint 7.
export default function FinanceiroPage() {
  usePageHeader("Financeiro", "Visão geral consolidada do mês");
  const { sessao } = useAuth();
  const [dados, setDados] = useState<VisaoGeralFinanceira | null>(null);
  const [erro, setErro] = useState("");

  useEffect(() => {
    if (!sessao) return;
    obterVisaoGeralFinanceira(sessao.accessToken)
      .then(setDados)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar o financeiro."));
  }, [sessao]);

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!dados) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <div className="kpi-row kpi-row-2">
        <div className="kpi">
          <div className="label">
            <Icon name="wallet" size={12} /> Saldo realizado (mês)
          </div>
          <div className={`value ${dados.resultado.saldoRealizado >= 0 ? "turq" : "danger"}`}>
            {currency(dados.resultado.saldoRealizado)}
          </div>
          <div className="delta">Recebido menos pago no mês corrente</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="money" size={12} /> Saldo previsto
          </div>
          <div className={`value ${dados.resultado.saldoPrevisto >= 0 ? "turq" : "danger"}`}>
            {currency(dados.resultado.saldoPrevisto)}
          </div>
          <div className="delta">Se tudo em aberto hoje for liquidado</div>
        </div>
      </div>

      <div className="grid-2b">
        <div className="mini-panel">
          <h4>
            <Icon name="wallet" size={15} /> Receitas
          </h4>
          <div className="bar-list">
            <div className="bar-row">
              <div className="lb">Recebido (mês)</div>
              <div className="val">{currency(dados.receitas.recebido)}</div>
            </div>
            <div className="bar-row">
              <div className="lb">A receber</div>
              <div className="val">{currency(dados.receitas.aReceber)}</div>
            </div>
            <div className="bar-row">
              <div className="lb">Atrasado</div>
              <div className="val" style={{ color: "var(--c-danger)" }}>{currency(dados.receitas.atrasado)}</div>
            </div>
          </div>
        </div>

        <div className="mini-panel">
          <h4>
            <Icon name="money" size={15} /> Despesas
          </h4>
          <div className="bar-list">
            <div className="bar-row">
              <div className="lb">Pago (mês)</div>
              <div className="val">{currency(dados.despesas.pago)}</div>
            </div>
            <div className="bar-row">
              <div className="lb">A pagar</div>
              <div className="val">{currency(dados.despesas.aPagar)}</div>
            </div>
            <div className="bar-row">
              <div className="lb">Atrasado</div>
              <div className="val" style={{ color: "var(--c-danger)" }}>{currency(dados.despesas.atrasado)}</div>
            </div>
          </div>
        </div>
      </div>

      <div className="mini-panel">
        <h4>
          <Icon name="cal" size={15} /> Próximos vencimentos (7 dias)
        </h4>
        {dados.proximosVencimentos.length ? (
          dados.proximosVencimentos.map((item, i) => (
            <Link
              href={item.tipo === "Receber" ? "/financeiro/contas-a-receber" : "/financeiro/contas-a-pagar"}
              className="lesson-item"
              key={i}
            >
              <div className="lesson-time">{fmtData(item.dataVencimento).slice(0, 5)}</div>
              <div className="lesson-info">
                <div className="subj">{item.descricao}</div>
                <div className="who">{item.tipo === "Receber" ? "A receber" : "A pagar"}</div>
              </div>
              <div style={{ fontWeight: 700, fontSize: 12.5, color: item.tipo === "Receber" ? "var(--c-accent-deep)" : "var(--c-danger)" }}>
                {item.tipo === "Receber" ? "+" : "-"}
                {currency(item.valor)}
              </div>
            </Link>
          ))
        ) : (
          <EmptyState title="Nenhum vencimento próximo" desc="Nada a receber ou pagar nos próximos 7 dias." />
        )}
      </div>
    </>
  );
}
