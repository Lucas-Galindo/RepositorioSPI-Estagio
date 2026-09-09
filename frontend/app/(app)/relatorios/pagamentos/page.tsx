"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarPagamentos, type Pagamento } from "@/lib/api/pagamentos";
import { currency, fmtData } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

// Dashboard de negocio dos pagamentos/contas a receber (Sprint 2 da
// evolucao de Relatorios), espelhando ROUTES['pagamentos-dashboard'] do
// protótipo. Usa o mesmo endpoint /api/pagamentos ja usado em Contas a
// Receber, filtrando por vencimento (mesmo campo/nome de filtro ja usado
// la, em vez do "semestre" fictício do protótipo).
export default function DashboardPagamentosPage() {
  usePageHeader("Relatórios", "Dashboard de Pagamentos");
  const { sessao } = useAuth();
  const router = useRouter();

  const [pagamentos, setPagamentos] = useState<Pagamento[] | null>(null);
  const [erro, setErro] = useState("");
  const [vencimentoInicio, setVencimentoInicio] = useState("");
  const [vencimentoFim, setVencimentoFim] = useState("");

  const requisicaoAtual = useRef(0);
  useEffect(() => {
    if (!sessao) return;
    const idDestaRequisicao = ++requisicaoAtual.current;
    listarPagamentos(sessao.accessToken, {
      vencimentoInicio: vencimentoInicio || undefined,
      vencimentoFim: vencimentoFim || undefined,
    })
      .then((dados) => {
        if (idDestaRequisicao === requisicaoAtual.current) setPagamentos(dados);
      })
      .catch((excecao) => {
        if (idDestaRequisicao === requisicaoAtual.current) {
          setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar os pagamentos.");
        }
      });
  }, [sessao, vencimentoInicio, vencimentoFim]);

  const limparFiltros = () => {
    setVencimentoInicio("");
    setVencimentoFim("");
  };
  const temFiltro = Boolean(vencimentoInicio || vencimentoFim);

  const lista = pagamentos ?? [];
  const pagos = lista.filter((p) => p.status === "Pago").reduce((s, p) => s + p.valorFinal, 0);
  const pendentes = lista.filter((p) => p.status === "Pendente").reduce((s, p) => s + p.valorFinal, 0);
  const atrasados = lista.filter((p) => p.status === "Atrasado").reduce((s, p) => s + p.valorFinal, 0);
  const total = pagos + pendentes + atrasados;
  const porStatus: [string, number][] = [
    ["Pago", pagos],
    ["Pendente", pendentes],
    ["Atrasado", atrasados],
  ];
  const maiorStatus = Math.max(1, ...porStatus.map(([, v]) => v));

  const ultimosLancamentos = [...lista].sort((a, b) => b.dataVencimento.localeCompare(a.dataVencimento)).slice(0, 6);

  return (
    <>
      <Link href="/relatorios" className="breadcrumb">
        <Icon name="back" size={13} /> Relatórios
      </Link>

      <div className="filter-bar">
        <input
          className="filter-input"
          type="date"
          value={vencimentoInicio}
          onChange={(e) => setVencimentoInicio(e.target.value)}
          title="Vencimento a partir de"
        />
        <input className="filter-input" type="date" value={vencimentoFim} onChange={(e) => setVencimentoFim(e.target.value)} title="Vencimento até" />
        {temFiltro && (
          <button type="button" className="filter-clear" onClick={limparFiltros}>
            Limpar filtros
          </button>
        )}
      </div>

      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {!erro && !pagamentos && <p className="count-text">Carregando...</p>}

      {!erro && pagamentos && (
        <>
          <div className="kpi-row">
            <div className="kpi">
              <div className="label">Recebido</div>
              <div className="value turq">{currency(pagos)}</div>
            </div>
            <div className="kpi">
              <div className="label">Pendente</div>
              <div className="value gold">{currency(pendentes)}</div>
            </div>
            <div className="kpi">
              <div className="label">Atrasado</div>
              <div className="value danger">{currency(atrasados)}</div>
            </div>
            <div className="kpi">
              <div className="label">Total no período</div>
              <div className="value">{currency(total)}</div>
            </div>
          </div>

          <div className="grid-2">
            <div className="panel">
              <div className="section-title">Distribuição por status</div>
              <div className="section-sub">Valores por situação do pagamento</div>
              <div className="bar-list">
                {porStatus.map(([status, valor]) => (
                  <div className="bar-row" key={status}>
                    <div className="lb">{status}</div>
                    <div className="bar-track">
                      <div className="bar-fill" style={{ width: `${(valor / maiorStatus) * 100}%` }} />
                    </div>
                    <div className="val">{currency(valor)}</div>
                  </div>
                ))}
              </div>
            </div>

            <div className="panel">
              <div className="row-gap">
                <div>
                  <div className="section-title">Últimos lançamentos</div>
                  <div className="section-sub" style={{ marginBottom: 0 }}>
                    6 mais recentes no filtro aplicado
                  </div>
                </div>
                <Link href="/financeiro/contas-a-receber" className="btn btn-ghost btn-sm">
                  <Icon name="wallet" size={13} /> Ver planilha
                </Link>
              </div>
              {ultimosLancamentos.length ? (
                <div className="table-wrap" style={{ boxShadow: "none", margin: 0 }}>
                  <table>
                    <thead>
                      <tr>
                        <th>Aluno</th>
                        <th>Vencimento</th>
                        <th>Valor</th>
                        <th>Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {ultimosLancamentos.map((p) => (
                        <tr className="row-link" key={p.id} onClick={() => router.push(`/financeiro/contas-a-receber/${p.id}`)}>
                          <td>{p.alunoNome}</td>
                          <td>{fmtData(p.dataVencimento)}</td>
                          <td>{currency(p.valorFinal)}</td>
                          <td>
                            <StatusPill status={p.status} />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <EmptyState title="Nenhum lançamento no período" desc="Ajuste os filtros para ver outros resultados." />
              )}
            </div>
          </div>
        </>
      )}
    </>
  );
}
