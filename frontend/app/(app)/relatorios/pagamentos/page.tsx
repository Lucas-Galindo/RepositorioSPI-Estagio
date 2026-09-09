"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarPagamentos, listarFormasPagamento, type Pagamento, type FormaPagamento } from "@/lib/api/pagamentos";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { listarMaterias, type Materia } from "@/lib/api/materias";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { currency, fmtData } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

// Dashboard de negocio dos pagamentos/contas a receber (Sprint 2 da
// evolucao de Relatorios), espelhando ROUTES['pagamentos-dashboard'] do
// protótipo. Usa o mesmo endpoint /api/pagamentos ja usado em Contas a
// Receber, filtrando por vencimento no servidor (mesmo campo/nome de
// filtro ja usado la). O filtro de semestre e so um atalho de UI: preenche
// o periodo (vencimentoInicio/vencimentoFim) do ano corrente.
//
// Matéria, turma, forma de pagamento e aluno sao filtrados no cliente
// (mesmo padrao ja usado em Contas a Receber para categoria/forma):
// matéria via Aula.materiaId (por aulaIds do pagamento), turma via
// aluno.turmas (o pagamento nao tem turma direta).
function periodoSemestre(semestre: "1" | "2"): { inicio: string; fim: string } {
  const ano = new Date().getFullYear();
  return semestre === "1" ? { inicio: `${ano}-01-01`, fim: `${ano}-06-30` } : { inicio: `${ano}-07-01`, fim: `${ano}-12-31` };
}

export default function DashboardPagamentosPage() {
  usePageHeader("Relatórios", "Dashboard de Pagamentos");
  const { sessao } = useAuth();
  const router = useRouter();

  const [pagamentos, setPagamentos] = useState<Pagamento[] | null>(null);
  const [alunos, setAlunos] = useState<Aluno[]>([]);
  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [materias, setMaterias] = useState<Materia[]>([]);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [erro, setErro] = useState("");

  const [vencimentoInicio, setVencimentoInicio] = useState("");
  const [vencimentoFim, setVencimentoFim] = useState("");
  const [semestre, setSemestre] = useState("");
  const [materiaId, setMateriaId] = useState("");
  const [turmaId, setTurmaId] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [alunoId, setAlunoId] = useState("");

  const selecionarSemestre = (valor: string) => {
    setSemestre(valor);
    if (valor === "1" || valor === "2") {
      const periodo = periodoSemestre(valor);
      setVencimentoInicio(periodo.inicio);
      setVencimentoFim(periodo.fim);
    }
  };

  useEffect(() => {
    if (!sessao) return;
    listarAlunos(sessao.accessToken).then(setAlunos).catch(() => setAlunos([]));
    listarTurmas(sessao.accessToken).then(setTurmas).catch(() => setTurmas([]));
    listarMaterias(sessao.accessToken, { ativo: true }).then(setMaterias).catch(() => setMaterias([]));
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
    listarAulas(sessao.accessToken).then(setAulas).catch(() => setAulas([]));
  }, [sessao]);

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

  const materiaIdsPorAula = useMemo(() => new Map(aulas.map((a) => [a.id, a.materiaId])), [aulas]);
  const turmasPorAluno = useMemo(() => new Map(alunos.map((a) => [a.id, new Set(a.turmas.map((t) => t.id))])), [alunos]);

  const limparFiltros = () => {
    setVencimentoInicio("");
    setVencimentoFim("");
    setSemestre("");
    setMateriaId("");
    setTurmaId("");
    setFormaPagamentoId("");
    setAlunoId("");
  };
  const temFiltro = Boolean(vencimentoInicio || vencimentoFim || materiaId || turmaId || formaPagamentoId || alunoId);

  const lista = (pagamentos ?? []).filter((p) => {
    if (alunoId && p.alunoId !== Number(alunoId)) return false;
    if (formaPagamentoId && p.formaPagamentoId !== Number(formaPagamentoId)) return false;
    if (materiaId && !p.aulaIds.some((aulaId) => materiaIdsPorAula.get(aulaId) === Number(materiaId))) return false;
    if (turmaId && !(turmasPorAluno.get(p.alunoId)?.has(Number(turmaId)) ?? false)) return false;
    return true;
  });

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
        <select className="filter-select" value={semestre} onChange={(e) => selecionarSemestre(e.target.value)}>
          <option value="">Semestre — todos</option>
          <option value="1">1º semestre</option>
          <option value="2">2º semestre</option>
        </select>
        <select className="filter-select" value={materiaId} onChange={(e) => setMateriaId(e.target.value)}>
          <option value="">Matéria — todas</option>
          {materias.map((m) => (
            <option key={m.id} value={m.id}>
              {m.nome}
            </option>
          ))}
        </select>
        <select className="filter-select" value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
          <option value="">Turma — todas</option>
          {turmas.map((t) => (
            <option key={t.id} value={t.id}>
              {t.nome}
            </option>
          ))}
        </select>
        <select className="filter-select" value={formaPagamentoId} onChange={(e) => setFormaPagamentoId(e.target.value)}>
          <option value="">Forma — todas</option>
          {formas.map((f) => (
            <option key={f.id} value={f.id}>
              {f.forma}
            </option>
          ))}
        </select>
        <select className="filter-select" value={alunoId} onChange={(e) => setAlunoId(e.target.value)}>
          <option value="">Aluno — todos</option>
          {alunos.map((a) => (
            <option key={a.id} value={a.id}>
              {a.nome}
            </option>
          ))}
        </select>
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
