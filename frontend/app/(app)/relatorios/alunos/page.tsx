"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { listarPagamentos, type Pagamento } from "@/lib/api/pagamentos";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { listarMaterias, type Materia } from "@/lib/api/materias";
import { currency } from "@/lib/format";

// Dashboard de negocio da base de alunos (Sprint 2 da evolucao de
// Relatorios). O protótipo (spi_prototipo_clay_final.html) tem um painel
// "Distribuição por matéria" usando Aluno.materiaPrincipal (mock), campo
// que nao existe no schema real -- matéria so se liga ao aluno atraves das
// Aulas. Aqui a "matéria principal" de cada aluno e derivada como a matéria
// com mais aulas frequentadas por ele (no período filtrado, se houver);
// alunos sem nenhuma aula no periodo ficam fora do gráfico.
const CORES_MATERIA = ["var(--c-accent)", "var(--c-accent2)", "var(--c-accent-deep)", "var(--c-accent3)", "var(--c-danger)"];

function periodoSemestre(semestre: "1" | "2"): { inicio: string; fim: string } {
  const ano = new Date().getFullYear();
  return semestre === "1" ? { inicio: `${ano}-01-01`, fim: `${ano}-06-30` } : { inicio: `${ano}-07-01`, fim: `${ano}-12-31` };
}

export default function DashboardAlunosPage() {
  usePageHeader("Relatórios", "Dashboard de Alunos");
  const { sessao } = useAuth();

  const [alunos, setAlunos] = useState<Aluno[] | null>(null);
  const [pagamentos, setPagamentos] = useState<Pagamento[]>([]);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [materias, setMaterias] = useState<Materia[]>([]);
  const [erro, setErro] = useState("");

  const [inicio, setInicio] = useState("");
  const [fim, setFim] = useState("");
  const [semestre, setSemestre] = useState("");
  const [materiaId, setMateriaId] = useState("");
  const [alunoId, setAlunoId] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarAlunos(sessao.accessToken)
      .then(setAlunos)
      .catch(() => setErro("Não foi possível carregar os alunos."));
    listarPagamentos(sessao.accessToken).then(setPagamentos).catch(() => setPagamentos([]));
    listarAulas(sessao.accessToken).then(setAulas).catch(() => setAulas([]));
    listarMaterias(sessao.accessToken, { ativo: true }).then(setMaterias).catch(() => setMaterias([]));
  }, [sessao]);

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
    setSemestre("");
    setMateriaId("");
    setAlunoId("");
  };
  const temFiltro = Boolean(inicio || fim || semestre || materiaId || alunoId);

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!alunos) return <p className="count-text">Carregando...</p>;

  const dentroDoPeriodo = (dataIso: string) => (!inicio || dataIso >= inicio) && (!fim || dataIso <= fim);
  const aulasNoPeriodo = aulas.filter((a) => dentroDoPeriodo(a.dataInicio));
  const pagamentosNoPeriodo = pagamentos.filter((p) => (p.dataPagamento ? dentroDoPeriodo(p.dataPagamento) : false));

  // Alunos com pelo menos uma aula da matéria filtrada, no período.
  const alunosDaMateria = materiaId
    ? new Set(aulasNoPeriodo.filter((a) => a.materiaId === Number(materiaId)).flatMap((a) => a.alunos.map((al) => al.alunoId)))
    : null;

  const alunosFiltrados = alunos.filter((a) => {
    if (alunoId && a.id !== Number(alunoId)) return false;
    if (alunosDaMateria && !alunosDaMateria.has(a.id)) return false;
    return true;
  });

  const total = alunosFiltrados.length;
  const ativos = alunosFiltrados.filter((a) => a.ativo).length;
  const inativos = total - ativos;
  const emTurma = alunosFiltrados.filter((a) => a.turmas.length > 0).length;

  const idsAlunosFiltrados = new Set(alunosFiltrados.map((a) => a.id));
  const faturado = pagamentosNoPeriodo
    .filter((p) => p.status === "Pago" && idsAlunosFiltrados.has(p.alunoId))
    .reduce((s, p) => s + p.valorFinal, 0);
  const ticketMedio = ativos ? faturado / ativos : 0;

  // Materia principal de cada aluno = materia com mais aulas frequentadas no periodo.
  const contagemPorAlunoMateria = new Map<number, Map<string, number>>();
  aulasNoPeriodo.forEach((aula) => {
    aula.alunos.forEach((al) => {
      if (!idsAlunosFiltrados.has(al.alunoId)) return;
      if (!contagemPorAlunoMateria.has(al.alunoId)) contagemPorAlunoMateria.set(al.alunoId, new Map());
      const porMateria = contagemPorAlunoMateria.get(al.alunoId)!;
      porMateria.set(aula.materiaNome, (porMateria.get(aula.materiaNome) ?? 0) + 1);
    });
  });
  const distribuicaoMateria = new Map<string, number>();
  contagemPorAlunoMateria.forEach((porMateria) => {
    let melhor = "";
    let maiorContagem = 0;
    porMateria.forEach((contagem, materia) => {
      if (contagem > maiorContagem) {
        melhor = materia;
        maiorContagem = contagem;
      }
    });
    if (melhor) distribuicaoMateria.set(melhor, (distribuicaoMateria.get(melhor) ?? 0) + 1);
  });
  const materiasLista = [...distribuicaoMateria.entries()].sort((a, b) => b[1] - a[1]);
  const totalNaPizza = materiasLista.reduce((s, [, v]) => s + v, 0);

  let acumulado = 0;
  const stops = materiasLista
    .map(([, valor], i) => {
      const pct = totalNaPizza ? (valor / totalNaPizza) * 100 : 0;
      const inicioFatia = acumulado;
      acumulado += pct;
      return `${CORES_MATERIA[i % CORES_MATERIA.length]} ${inicioFatia.toFixed(2)}% ${acumulado.toFixed(2)}%`;
    })
    .join(", ");

  const porTurma = new Map<string, number>();
  let semTurma = 0;
  alunosFiltrados.forEach((a) => {
    if (a.turmas.length === 0) {
      semTurma += 1;
      return;
    }
    a.turmas.forEach((t) => porTurma.set(t.nome, (porTurma.get(t.nome) ?? 0) + 1));
  });
  const distribuicaoTurma = [...porTurma.entries()].sort((a, b) => b[1] - a[1]);
  if (semTurma > 0) distribuicaoTurma.push(["Sem turma", semTurma]);
  const maiorTurma = Math.max(1, ...distribuicaoTurma.map(([, v]) => v));

  return (
    <>
      <Link href="/relatorios" className="breadcrumb">
        <Icon name="back" size={13} /> Relatórios
      </Link>

      <div className="filter-bar">
        <input className="filter-input" type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} title="Período a partir de" />
        <input className="filter-input" type="date" value={fim} onChange={(e) => setFim(e.target.value)} title="Período até" />
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

      <div className="kpi-row kpi-row-3">
        <div className="kpi">
          <div className="label">
            <Icon name="users" size={12} /> Total de alunos
          </div>
          <div className="value turq">{total}</div>
          <div className="delta">
            {ativos} ativos · {inativos} inativos
          </div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="wallet" size={12} /> Ticket médio
          </div>
          <div className="value">{currency(ticketMedio)}</div>
          <div className="delta">por aluno ativo, no período</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="book" size={12} /> Em turma
          </div>
          <div className="value gold">{emTurma}</div>
          <div className="delta">{total ? Math.round((emTurma / total) * 100) : 0}% da base</div>
        </div>
      </div>

      <div className="grid-2">
        <div className="panel">
          <div className="section-title">Distribuição por matéria</div>
          <div className="section-sub">Matéria com mais aulas frequentadas por aluno, no período</div>
          {materiasLista.length ? (
            <div className="pie-wrap">
              <div className="pie-chart" style={{ background: `conic-gradient(${stops})` }}>
                <div className="pie-center">
                  <div className="pie-center-val">{totalNaPizza}</div>
                  <div className="pie-center-lb">alunos</div>
                </div>
              </div>
              <div className="pie-legend">
                {materiasLista.map(([nome, valor], i) => (
                  <div className="pie-legend-item" key={nome}>
                    <span className="pie-dot" style={{ background: CORES_MATERIA[i % CORES_MATERIA.length] }} />
                    <span className="pie-label">{nome}</span>
                    <span className="pie-val">
                      {valor} aluno(s) · {totalNaPizza ? Math.round((valor / totalNaPizza) * 100) : 0}%
                    </span>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <EmptyState title="Nenhuma aula no período/filtros" desc="Ajuste os filtros para ver outros resultados." />
          )}
        </div>
        <div className="panel">
          <div className="section-title">Situação e modalidade</div>
          <div className="section-sub">Ativos x inativos e em turma, na base filtrada</div>
          <div className="bar-list">
            <div className="bar-row">
              <div className="lb">Ativos</div>
              <div className="bar-track">
                <div className="bar-fill" style={{ width: `${total ? (ativos / total) * 100 : 0}%` }} />
              </div>
              <div className="val">{ativos}</div>
            </div>
            <div className="bar-row">
              <div className="lb">Inativos</div>
              <div className="bar-track">
                <div className="bar-fill" style={{ width: `${total ? (inativos / total) * 100 : 0}%`, background: "var(--c-text-muted)" }} />
              </div>
              <div className="val">{inativos}</div>
            </div>
            <div className="bar-row">
              <div className="lb">Em turma</div>
              <div className="bar-track">
                <div
                  className="bar-fill"
                  style={{ width: `${total ? (emTurma / total) * 100 : 0}%`, background: "linear-gradient(90deg,var(--c-accent2),#8f6f3d)" }}
                />
              </div>
              <div className="val">{emTurma}</div>
            </div>
          </div>
        </div>
      </div>

      <div className="panel" style={{ marginTop: 18 }}>
        <div className="section-title">Distribuição por turma</div>
        <div className="section-sub">Quantidade de alunos matriculados por turma, na base filtrada</div>
        {distribuicaoTurma.length ? (
          <div className="bar-list">
            {distribuicaoTurma.map(([nome, valor]) => (
              <div className="bar-row" key={nome}>
                <div className="lb">{nome}</div>
                <div className="bar-track">
                  <div className="bar-fill" style={{ width: `${(valor / maiorTurma) * 100}%` }} />
                </div>
                <div className="val">{valor}</div>
              </div>
            ))}
          </div>
        ) : (
          <EmptyState title="Nenhum aluno encontrado" desc="Ajuste os filtros para ver outros resultados." />
        )}
      </div>
    </>
  );
}
