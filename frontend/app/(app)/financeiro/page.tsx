"use client";

import { useEffect, useRef, useState } from "react";
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
//
// Filtros de turma/matéria/aluno (Sprint 4.2, instrucao direta do usuario):
// busca por texto (nome, ou nome/RA no caso do aluno), nao dropdown -- e so
// se aplicam ao lado da receita, ja que despesas (Contas a Pagar) nao tem
// nenhuma ligacao com aluno/turma/matéria no dominio.
export default function FinanceiroPage() {
  usePageHeader("Financeiro", "Visão geral consolidada do mês");
  const { sessao } = useAuth();
  const [dados, setDados] = useState<VisaoGeralFinanceira | null>(null);
  const [erro, setErro] = useState("");

  const [inicio, setInicio] = useState("");
  const [fim, setFim] = useState("");
  const [turmaNome, setTurmaNome] = useState("");
  const [materiaNome, setMateriaNome] = useState("");
  const [alunoBusca, setAlunoBusca] = useState("");

  const requisicaoAtual = useRef(0);
  useEffect(() => {
    if (!sessao) return;
    const idDestaRequisicao = ++requisicaoAtual.current;
    obterVisaoGeralFinanceira(sessao.accessToken, {
      periodoInicio: inicio || undefined,
      periodoFim: fim || undefined,
      turmaNome: turmaNome || undefined,
      materiaNome: materiaNome || undefined,
      alunoBusca: alunoBusca || undefined,
    })
      .then((resultado) => {
        if (idDestaRequisicao === requisicaoAtual.current) setDados(resultado);
      })
      .catch((excecao) => {
        if (idDestaRequisicao === requisicaoAtual.current) {
          setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar o financeiro.");
        }
      });
  }, [sessao, inicio, fim, turmaNome, materiaNome, alunoBusca]);

  const limparFiltros = () => {
    setInicio("");
    setFim("");
    setTurmaNome("");
    setMateriaNome("");
    setAlunoBusca("");
  };
  const temFiltro = Boolean(inicio || fim || turmaNome || materiaNome || alunoBusca);

  return (
    <>
      <div className="filter-bar">
        <input className="filter-input" type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} title="Período a partir de" />
        <input className="filter-input" type="date" value={fim} onChange={(e) => setFim(e.target.value)} title="Período até" />
        <input className="filter-input" placeholder="Buscar turma..." value={turmaNome} onChange={(e) => setTurmaNome(e.target.value)} />
        <input className="filter-input" placeholder="Buscar matéria..." value={materiaNome} onChange={(e) => setMateriaNome(e.target.value)} />
        <input
          className="filter-input"
          placeholder="Buscar aluno (nome ou RA)..."
          value={alunoBusca}
          onChange={(e) => setAlunoBusca(e.target.value)}
        />
        {temFiltro && (
          <button type="button" className="filter-clear" onClick={limparFiltros}>
            Limpar filtros
          </button>
        )}
      </div>

      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {!erro && !dados && <p className="count-text">Carregando...</p>}

      {dados && (
        <>
          {temFiltro && (
            <p className="hint" style={{ marginTop: -8, marginBottom: 14 }}>
              Turma/matéria/aluno filtram só o lado da receita — despesas (Contas a Pagar) não têm ligação com
              aluno/turma/matéria, então continuam representando o total do negócio.
            </p>
          )}

          <div className="kpi-row kpi-row-2">
            <div className="kpi">
              <div className="label">
                <Icon name="wallet" size={12} /> Saldo realizado (mês)
              </div>
              <div className={`value ${dados.resultado.saldoRealizado >= 0 ? "turq" : "danger"}`}>
                {currency(dados.resultado.saldoRealizado)}
              </div>
              <div className="delta">Recebido menos pago no período</div>
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
                  <div className="lb">Recebido (período)</div>
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
                  <div className="lb">Pago (período)</div>
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
      )}
    </>
  );
}
