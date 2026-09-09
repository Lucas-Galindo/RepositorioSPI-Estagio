"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { listarMaterias, type Materia } from "@/lib/api/materias";

// Dashboard de negocio das turmas (Sprint 2 da evolucao de Relatorios). O
// protótipo (spi_prototipo_clay_final.html) tem um painel "turmas por nivel
// de escolaridade", mas Turma nao tem nivel no schema atual (nivel e um
// campo de Materia, e materia so se liga a turma atraves das Aulas) -- por
// isso os paineis aqui sao "turmas por matéria" e "alunos por matéria",
// exatamente os dois indicadores pedidos na Sprint 2.2 do documento,
// derivados de Aula.materiaId/turmaId.
function periodoSemestre(semestre: "1" | "2"): { inicio: string; fim: string } {
  const ano = new Date().getFullYear();
  return semestre === "1" ? { inicio: `${ano}-01-01`, fim: `${ano}-06-30` } : { inicio: `${ano}-07-01`, fim: `${ano}-12-31` };
}

export default function DashboardTurmasPage() {
  usePageHeader("Relatórios", "Dashboard de Turmas");
  const { sessao } = useAuth();

  const [turmas, setTurmas] = useState<Turma[] | null>(null);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [materias, setMaterias] = useState<Materia[]>([]);
  const [erro, setErro] = useState("");

  const [inicio, setInicio] = useState("");
  const [fim, setFim] = useState("");
  const [semestre, setSemestre] = useState("");
  const [materiaId, setMateriaId] = useState("");
  const [turmaId, setTurmaId] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarTurmas(sessao.accessToken)
      .then(setTurmas)
      .catch(() => setErro("Não foi possível carregar as turmas."));
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
    setTurmaId("");
  };
  const temFiltro = Boolean(inicio || fim || semestre || materiaId || turmaId);

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!turmas) return <p className="count-text">Carregando...</p>;

  const dentroDoPeriodo = (dataIso: string) => (!inicio || dataIso >= inicio) && (!fim || dataIso <= fim);
  const aulasFiltradas = aulas.filter(
    (a) =>
      dentroDoPeriodo(a.dataInicio) &&
      (!materiaId || a.materiaId === Number(materiaId)) &&
      (!turmaId || a.turmaId === Number(turmaId))
  );

  const turmasFiltradas = turmas.filter((t) => !turmaId || t.id === Number(turmaId));

  const total = turmasFiltradas.length;
  const totalAlunosEmTurma = turmasFiltradas.reduce((s, t) => s + t.alunos.length, 0);
  const ocupacaoMedia = total ? totalAlunosEmTurma / total : 0;

  const turmasPorMateria = new Map<string, Set<number>>();
  const alunosPorMateria = new Map<string, Set<number>>();
  aulasFiltradas.forEach((a) => {
    if (a.turmaId !== null) {
      if (!turmasPorMateria.has(a.materiaNome)) turmasPorMateria.set(a.materiaNome, new Set());
      turmasPorMateria.get(a.materiaNome)!.add(a.turmaId);
    }
    if (!alunosPorMateria.has(a.materiaNome)) alunosPorMateria.set(a.materiaNome, new Set());
    a.alunos.forEach((al) => alunosPorMateria.get(a.materiaNome)!.add(al.alunoId));
  });
  const turmasPorMateriaLista = [...turmasPorMateria.entries()].map(([nome, ids]) => [nome, ids.size] as const).sort((a, b) => b[1] - a[1]);
  const alunosPorMateriaLista = [...alunosPorMateria.entries()].map(([nome, ids]) => [nome, ids.size] as const).sort((a, b) => b[1] - a[1]);
  const maiorTurmasMateria = Math.max(1, ...turmasPorMateriaLista.map(([, v]) => v));
  const maiorAlunosMateria = Math.max(1, ...alunosPorMateriaLista.map(([, v]) => v));

  const aulasPorTurma = turmasFiltradas.map((t) => {
    const das = aulasFiltradas.filter((a) => a.turmaId === t.id);
    return { turma: t, realizadas: das.filter((a) => a.status === "Realizada").length, agendadas: das.filter((a) => a.status === "Agendada").length };
  });
  const maiorAulasTurma = Math.max(1, ...aulasPorTurma.map((x) => x.realizadas + x.agendadas));

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
        <select className="filter-select" value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
          <option value="">Turma — todas</option>
          {turmas.map((t) => (
            <option key={t.id} value={t.id}>
              {t.nome}
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
            <Icon name="users" size={12} /> Total de turmas
          </div>
          <div className="value turq">{total}</div>
          <div className="delta">turmas ativas</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="users" size={12} /> Alunos em turma
          </div>
          <div className="value gold">{totalAlunosEmTurma}</div>
          <div className="delta">soma das turmas filtradas</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="book" size={12} /> Ocupação média
          </div>
          <div className="value">{ocupacaoMedia.toFixed(1)}</div>
          <div className="delta">aluno(s) por turma</div>
        </div>
      </div>

      <div className="grid-2">
        <div className="panel">
          <div className="section-title">Turmas por matéria</div>
          <div className="section-sub">Quantidade de turmas com aulas de cada matéria, no período/filtros</div>
          {turmasPorMateriaLista.length ? (
            <div className="bar-list">
              {turmasPorMateriaLista.map(([nome, valor]) => (
                <div className="bar-row" key={nome}>
                  <div className="lb">{nome}</div>
                  <div className="bar-track">
                    <div className="bar-fill" style={{ width: `${(valor / maiorTurmasMateria) * 100}%` }} />
                  </div>
                  <div className="val">{valor}</div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="Nenhuma aula no período/filtros" desc="Ajuste os filtros para ver outros resultados." />
          )}
        </div>
        <div className="panel">
          <div className="section-title">Alunos por matéria</div>
          <div className="section-sub">Quantidade de alunos com aula de cada matéria, no período/filtros</div>
          {alunosPorMateriaLista.length ? (
            <div className="bar-list">
              {alunosPorMateriaLista.map(([nome, valor]) => (
                <div className="bar-row" key={nome}>
                  <div className="lb">{nome}</div>
                  <div className="bar-track">
                    <div className="bar-fill" style={{ width: `${(valor / maiorAlunosMateria) * 100}%`, background: "linear-gradient(90deg,var(--c-accent2),#8f6f3d)" }} />
                  </div>
                  <div className="val">{valor}</div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="Nenhuma aula no período/filtros" desc="Ajuste os filtros para ver outros resultados." />
          )}
        </div>
      </div>

      <div className="panel" style={{ marginTop: 18 }}>
        <div className="section-title">Aulas por turma</div>
        <div className="section-sub">Realizadas x agendadas, no período/filtros</div>
        {aulasPorTurma.length ? (
          <div className="bar-list">
            {aulasPorTurma.map((x) => (
              <div className="bar-row" key={x.turma.id}>
                <div className="lb">{x.turma.nome}</div>
                <div className="bar-track">
                  <div className="bar-fill" style={{ width: `${((x.realizadas + x.agendadas) / maiorAulasTurma) * 100}%` }} />
                </div>
                <div className="val">
                  {x.realizadas}R · {x.agendadas}A
                </div>
              </div>
            ))}
          </div>
        ) : (
          <EmptyState title="Nenhuma turma encontrada" desc="Ajuste os filtros para ver outros resultados." />
        )}
      </div>
    </>
  );
}
