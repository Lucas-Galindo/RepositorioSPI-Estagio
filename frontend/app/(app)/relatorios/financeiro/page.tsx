"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterRelatorioFinanceiro, obterIndicadoresFinanceiros, type RelatorioFinanceiro, type IndicadoresFinanceirosFiltrados } from "@/lib/api/relatorios";
import { listarFormasPagamento, type FormaPagamento } from "@/lib/api/pagamentos";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { listarMaterias, type Materia } from "@/lib/api/materias";
import { currency, fmtMesAbreviado } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

// Relatorio Financeiro Consolidado (Sprint 8): complementa a Visao Geral
// (Financeiro > Visao Geral, sempre mes corrente) permitindo filtrar
// qualquer periodo/forma/aluno/turma -- sem duplicar aquela tela.
//
// O filtro de semestre e so um atalho de UI: preenche inicio/fim do ano
// corrente (Jan-Jun ou Jul-Dez) e some -- os campos de data continuam
// editaveis manualmente depois.
function periodoSemestre(semestre: "1" | "2"): { inicio: string; fim: string } {
  const ano = new Date().getFullYear();
  return semestre === "1" ? { inicio: `${ano}-01-01`, fim: `${ano}-06-30` } : { inicio: `${ano}-07-01`, fim: `${ano}-12-31` };
}

export default function RelatorioFinanceiroPage() {
  usePageHeader("Relatórios", "Relatório Financeiro Consolidado");
  const { sessao } = useAuth();

  // Duas visoes dentro da mesma pagina (Sprint 3 da evolucao do Financeiro):
  // troca de estado local, sem navegacao de rota.
  const [aba, setAba] = useState<"financeiro" | "indicadores">("financeiro");

  const [dados, setDados] = useState<RelatorioFinanceiro | null>(null);
  const [indicadoresDados, setIndicadoresDados] = useState<IndicadoresFinanceirosFiltrados | null>(null);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [alunos, setAlunos] = useState<Aluno[]>([]);
  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [materias, setMaterias] = useState<Materia[]>([]);
  const [erro, setErro] = useState("");
  const [erroIndicadores, setErroIndicadores] = useState("");

  const [inicio, setInicio] = useState("");
  const [fim, setFim] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [alunoId, setAlunoId] = useState("");
  const [turmaId, setTurmaId] = useState("");
  const [materiaId, setMateriaId] = useState("");
  const [semestre, setSemestre] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
    listarAlunos(sessao.accessToken, { ativo: true }).then(setAlunos).catch(() => setAlunos([]));
    listarTurmas(sessao.accessToken, { ativo: true }).then(setTurmas).catch(() => setTurmas([]));
    listarMaterias(sessao.accessToken, { ativo: true }).then(setMaterias).catch(() => setMaterias([]));
  }, [sessao]);

  // Contador de requisicoes: evita que uma resposta antiga (de um filtro ja
  // substituido) chegue depois da mais recente e sobrescreva a tela com dados
  // errados -- pode acontecer quando dois filtros mudam em sequencia rapida
  // (ex: preencher inicio e fim do periodo em seguida).
  const requisicaoAtual = useRef(0);
  useEffect(() => {
    if (!sessao) return;
    const idDestaRequisicao = ++requisicaoAtual.current;
    obterRelatorioFinanceiro(sessao.accessToken, {
      inicio: inicio || undefined,
      fim: fim || undefined,
      formaPagamentoId: formaPagamentoId ? Number(formaPagamentoId) : undefined,
      alunoId: alunoId ? Number(alunoId) : undefined,
      turmaId: turmaId ? Number(turmaId) : undefined,
      materiaId: materiaId ? Number(materiaId) : undefined,
    })
      .then((resultado) => {
        if (idDestaRequisicao === requisicaoAtual.current) setDados(resultado);
      })
      .catch((excecao) => {
        if (idDestaRequisicao === requisicaoAtual.current) {
          setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar o relatório.");
        }
      });
  }, [sessao, inicio, fim, formaPagamentoId, alunoId, turmaId, materiaId]);

  // Indicadores buscados em paralelo, independente da aba ativa, pra troca
  // de aba ser instantanea (sem esperar a requisicao). Nao usa formaPagamentoId
  // (esse filtro so existe na Visao Financeiro).
  const requisicaoIndicadoresAtual = useRef(0);
  useEffect(() => {
    if (!sessao) return;
    const idDestaRequisicao = ++requisicaoIndicadoresAtual.current;
    obterIndicadoresFinanceiros(sessao.accessToken, {
      inicio: inicio || undefined,
      fim: fim || undefined,
      turmaId: turmaId ? Number(turmaId) : undefined,
      materiaId: materiaId ? Number(materiaId) : undefined,
      alunoId: alunoId ? Number(alunoId) : undefined,
    })
      .then((resultado) => {
        if (idDestaRequisicao === requisicaoIndicadoresAtual.current) setIndicadoresDados(resultado);
      })
      .catch((excecao) => {
        if (idDestaRequisicao === requisicaoIndicadoresAtual.current) {
          setErroIndicadores(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar os indicadores.");
        }
      });
  }, [sessao, inicio, fim, turmaId, materiaId, alunoId]);

  const selecionarSemestre = (valor: string) => {
    setSemestre(valor);
    if (valor === "1" || valor === "2") {
      const periodo = periodoSemestre(valor);
      setInicio(periodo.inicio);
      setFim(periodo.fim);
    }
  };

  const limparFiltros = () => {
    setInicio("");
    setFim("");
    setFormaPagamentoId("");
    setAlunoId("");
    setTurmaId("");
    setMateriaId("");
    setSemestre("");
  };
  const temFiltro = Boolean(inicio || fim || formaPagamentoId || alunoId || turmaId || materiaId);
  const maiorForma = Math.max(1, ...(dados?.porFormaPagamento.map((f) => f.total) ?? [1]));
  const maiorTurma = Math.max(1, ...(dados?.porTurma.map((t) => t.total) ?? [1]));
  const totalPorTurma = dados?.porTurma.reduce((s, t) => s + t.total, 0) ?? 0;

  return (
    <>
      <Link href="/relatorios" className="breadcrumb">
        <Icon name="back" size={13} /> Relatórios
      </Link>

      <div className="row-gap" style={{ marginBottom: 18, gap: 8 }}>
        <button type="button" className={`btn btn-sm ${aba === "financeiro" ? "btn-primary" : "btn-ghost"}`} onClick={() => setAba("financeiro")}>
          Visão Financeiro
        </button>
        <button type="button" className={`btn btn-sm ${aba === "indicadores" ? "btn-primary" : "btn-ghost"}`} onClick={() => setAba("indicadores")}>
          Visão de Indicadores
        </button>
      </div>

      <div className="filter-bar">
        <input className="filter-input" type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} title="Recebido/pago a partir de" />
        <input className="filter-input" type="date" value={fim} onChange={(e) => setFim(e.target.value)} title="Recebido/pago até" />
        <select className="filter-select" value={semestre} onChange={(e) => selecionarSemestre(e.target.value)}>
          <option value="">Semestre — todos</option>
          <option value="1">1º semestre</option>
          <option value="2">2º semestre</option>
        </select>
        {aba === "financeiro" && (
          <select className="filter-select" value={formaPagamentoId} onChange={(e) => setFormaPagamentoId(e.target.value)}>
            <option value="">Forma — todas</option>
            {formas.map((f) => (
              <option key={f.id} value={f.id}>
                {f.forma}
              </option>
            ))}
          </select>
        )}
        <select className="filter-select" value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
          <option value="">Turma — todas</option>
          {turmas.map((t) => (
            <option key={t.id} value={t.id}>
              {t.nome}
            </option>
          ))}
        </select>
        <select className="filter-select" value={materiaId} onChange={(e) => setMateriaId(e.target.value)}>
          <option value="">Matéria — todas</option>
          {materias.map((m) => (
            <option key={m.id} value={m.id}>
              {m.nome}
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

      {aba === "financeiro" && erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {aba === "financeiro" && !erro && !dados && <p className="count-text">Carregando...</p>}

      {aba === "financeiro" && dados && (
        <>
          <div className="kpi-row kpi-row-3">
            <div className="kpi">
              <div className="label">
                <Icon name="wallet" size={12} /> Recebido
              </div>
              <div className="value turq">{currency(dados.totalRecebido)}</div>
            </div>
            <div className="kpi">
              <div className="label">
                <Icon name="money" size={12} /> Pago
              </div>
              <div className="value">{currency(dados.totalPago)}</div>
            </div>
            <div className="kpi">
              <div className="label">
                <Icon name="cal" size={12} /> Saldo realizado
              </div>
              <div className={`value ${dados.saldoRealizado >= 0 ? "turq" : "danger"}`}>{currency(dados.saldoRealizado)}</div>
            </div>
          </div>

          <div className="kpi-row kpi-row-3">
            <div className="kpi">
              <div className="label">
                <Icon name="warn" size={12} /> Receita pendente
              </div>
              <div className="value gold">{currency(dados.receitaPendente)}</div>
            </div>
            <div className="kpi">
              <div className="label">
                <Icon name="warn" size={12} /> Despesa pendente
              </div>
              <div className="value gold">{currency(dados.despesaPendente)}</div>
            </div>
            <div className="kpi">
              <div className="label">
                <Icon name="fwd" size={12} /> Saldo previsto
              </div>
              <div className={`value ${dados.saldoPrevisto >= 0 ? "turq" : "danger"}`}>{currency(dados.saldoPrevisto)}</div>
            </div>
          </div>

          <div className="grid-2b">
            <div className="mini-panel">
              <h4>
                <Icon name="users" size={15} /> Somatória por turma
              </h4>
              <p className="hint" style={{ marginTop: -4, marginBottom: 10 }}>
                Total recebido, atribuído por turma do aluno
              </p>
              {dados.porTurma.length ? (
                <div className="bar-list">
                  {dados.porTurma.map((t) => (
                    <div className="bar-row" key={t.chave}>
                      <div className="lb">{t.chave}</div>
                      <div className="bar-track">
                        <div className="bar-fill" style={{ width: `${(t.total / maiorTurma) * 100}%` }} />
                      </div>
                      <div className="val">{currency(t.total)}</div>
                    </div>
                  ))}
                </div>
              ) : (
                <EmptyState title="Sem dados" desc="Nenhum recebimento no período/filtros selecionados." />
              )}
            </div>

            <div className="mini-panel">
              <h4>
                <Icon name="wallet" size={15} /> Recebido por forma de pagamento
              </h4>
              {dados.porFormaPagamento.length ? (
                <div className="bar-list">
                  {dados.porFormaPagamento.map((f) => (
                    <div className="bar-row" key={f.chave}>
                      <div className="lb">{f.chave}</div>
                      <div className="bar-track">
                        <div className="bar-fill" style={{ width: `${(f.total / maiorForma) * 100}%` }} />
                      </div>
                      <div className="val">{currency(f.total)}</div>
                    </div>
                  ))}
                </div>
              ) : (
                <EmptyState title="Sem dados" desc="Nenhum recebimento no período/filtros selecionados." />
              )}
            </div>
          </div>

          <div className="grid-2b" style={{ marginTop: 24 }}>
            <div className="mini-panel">
              <h4>
                <Icon name="users" size={15} /> Recebido por aluno
              </h4>
              {dados.porAluno.length ? (
                dados.porAluno.map((a) => (
                  <div className="lesson-item" key={a.chave} style={{ cursor: "default" }}>
                    <div className="lesson-info">
                      <div className="subj">{a.chave}</div>
                    </div>
                    <div style={{ fontWeight: 700, fontSize: 12.5 }}>{currency(a.total)}</div>
                  </div>
                ))
              ) : (
                <EmptyState title="Sem dados" desc="Nenhum recebimento no período/filtros selecionados." />
              )}
            </div>

            <div className="mini-panel">
              <h4>
                <Icon name="book" size={15} /> Distribuição turma x particular
              </h4>
              <p className="hint" style={{ marginTop: -4, marginBottom: 10 }}>
                Participação de cada modalidade no total recebido
              </p>
              {dados.porTurma.length ? (
                <div className="bar-list">
                  {dados.porTurma.map((t) => (
                    <div className="bar-row" key={t.chave}>
                      <div className="lb">{t.chave}</div>
                      <div className="bar-track">
                        <div className="bar-fill" style={{ width: `${totalPorTurma ? (t.total / totalPorTurma) * 100 : 0}%` }} />
                      </div>
                      <div className="val">{totalPorTurma ? Math.round((t.total / totalPorTurma) * 100) : 0}%</div>
                    </div>
                  ))}
                </div>
              ) : (
                <EmptyState title="Sem dados" desc="Nenhum recebimento no período/filtros selecionados." />
              )}
            </div>
          </div>
        </>
      )}

      {aba === "indicadores" && erroIndicadores && <EmptyState title="Não foi possível carregar" desc={erroIndicadores} />}
      {aba === "indicadores" && !erroIndicadores && !indicadoresDados && <p className="count-text">Carregando...</p>}

      {aba === "indicadores" && indicadoresDados && (
        <>
          <div className="kpi-row">
            <div className="kpi">
              <div className="label">
                <Icon name="warn" size={12} /> Inadimplência
              </div>
              <div className={`value ${indicadoresDados.indicadores.taxaInadimplenciaPercentual > 0 ? "danger" : "turq"}`}>
                {indicadoresDados.indicadores.taxaInadimplenciaPercentual}%
              </div>
              <div className="delta">Do valor vencido no período</div>
            </div>
            <div className="kpi">
              <div className="label">
                <Icon name="cal" size={12} /> Prazo médio de atraso
              </div>
              <div className="value">
                {indicadoresDados.indicadores.prazoMedioAtrasoDias !== null ? `${indicadoresDados.indicadores.prazoMedioAtrasoDias} dias` : "—"}
              </div>
              <div className="delta">Contas pagas com atraso</div>
            </div>
            <div className="kpi">
              <div className="label">
                <Icon name="wallet" size={12} /> Margem de segurança
              </div>
              <div className={`value ${indicadoresDados.indicadores.margemSegurancaPercentual >= 0 ? "turq" : "danger"}`}>
                {indicadoresDados.indicadores.margemSegurancaPercentual}%
              </div>
              <div className="delta">Folga do caixa após despesas</div>
            </div>
            <div className="kpi">
              <div className="label">
                <Icon name="money" size={12} /> Cobertura de custos
              </div>
              <div
                className={`value ${
                  indicadoresDados.indicadores.indiceCoberturaCustosFixos === null || indicadoresDados.indicadores.indiceCoberturaCustosFixos >= 1
                    ? "turq"
                    : "danger"
                }`}
              >
                {indicadoresDados.indicadores.indiceCoberturaCustosFixos !== null ? `${indicadoresDados.indicadores.indiceCoberturaCustosFixos}x` : "—"}
              </div>
              <div className="delta">Recebido ÷ pago no período</div>
            </div>
          </div>

          <p className="hint" style={{ marginTop: -10, marginBottom: 18 }}>
            Filtros de turma/matéria/aluno aqui se aplicam só ao lado da receita — despesas (Contas a Pagar) não têm ligação
            com aluno/turma/matéria, então continuam representando o total do negócio.
          </p>

          <div className="grid-2b">
            <div className="mini-panel">
              <h4>
                <Icon name="cal" size={15} /> Gargalo de caixa
              </h4>
              <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
                <div className="lesson-info">
                  <div className="subj">Maior entrada do período</div>
                  <div className="who">
                    {indicadoresDados.indicadores.gargaloCaixa.diaMaiorEntrada !== null
                      ? `Dia ${indicadoresDados.indicadores.gargaloCaixa.diaMaiorEntrada} — ${currency(indicadoresDados.indicadores.gargaloCaixa.valorMaiorEntrada)}`
                      : "Sem dados"}
                  </div>
                </div>
              </div>
              <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0 }}>
                <div className="lesson-info">
                  <div className="subj">Maior saída do período</div>
                  <div className="who">
                    {indicadoresDados.indicadores.gargaloCaixa.diaMaiorSaida !== null
                      ? `Dia ${indicadoresDados.indicadores.gargaloCaixa.diaMaiorSaida} — ${currency(indicadoresDados.indicadores.gargaloCaixa.valorMaiorSaida)}`
                      : "Sem dados"}
                  </div>
                </div>
              </div>
              <p className="hint" style={{ marginTop: 12 }}>
                Datas de vencimento com maior concentração de valor — ajuda a antecipar a necessidade de capital de giro.
              </p>
            </div>

            <div className="mini-panel">
              <h4>
                <Icon name="wallet" size={15} /> Fluxo de caixa (últimos 6 meses)
              </h4>
              <div className="table-wrap" style={{ boxShadow: "none", margin: 0 }}>
                <table>
                  <thead>
                    <tr>
                      <th>Período</th>
                      <th>Entradas</th>
                      <th>Saídas</th>
                      <th>Saldo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {indicadoresDados.fluxoCaixaMensal.map((item) => (
                      <tr key={`${item.ano}-${item.mes}`}>
                        <td>{fmtMesAbreviado(item.ano, item.mes)}</td>
                        <td>{currency(item.entradas)}</td>
                        <td>{currency(item.saidas)}</td>
                        <td style={{ color: item.saldo >= 0 ? "var(--c-accent-deep)" : "var(--c-danger)", fontWeight: 700 }}>
                          {currency(item.saldo)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </>
      )}
    </>
  );
}
