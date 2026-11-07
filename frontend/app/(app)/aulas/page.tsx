"use client";

import { useEffect, useState, type CSSProperties } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { DateInput } from "@/components/shared/DateInput";
import { Icon } from "@/components/shared/Icon";
import { TimeInput } from "@/components/shared/TimeInput";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { listarMaterias, type Materia } from "@/lib/api/materias";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { fmtData, fmtHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

// specs/074: agrupa o rotulo e os dois campos De/Ate para quebrarem de linha juntos.
const grupoDeFiltro: CSSProperties = { display: "inline-flex", flexWrap: "wrap", alignItems: "center", gap: 9, maxWidth: "100%" };

export default function AulasPage() {
  usePageHeader("Aulas", "Agenda de aulas individuais e de turma");
  const { sessao } = useAuth();
  const router = useRouter();
  const [aulas, setAulas] = useState<Aula[] | null>(null);
  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [erro, setErro] = useState("");
  const [status, setStatus] = useState("");
  const [turmaId, setTurmaId] = useState("");
  // specs/074: filtros de data e horario (De/Ate) e materia. O servidor filtra; aqui so o estado dos campos.
  const [materias, setMaterias] = useState<Materia[]>([]);
  const [materiaId, setMateriaId] = useState("");
  const [dataDe, setDataDe] = useState("");
  const [dataAte, setDataAte] = useState("");
  const [horaDe, setHoraDe] = useState("");
  const [horaAte, setHoraAte] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarTurmas(sessao.accessToken).then(setTurmas).catch(() => setTurmas([]));
    listarMaterias(sessao.accessToken).then(setMaterias).catch(() => setMaterias([]));
  }, [sessao]);

  useEffect(() => {
    if (!sessao) return;
    // So a resposta da ultima combinacao de filtros vale; um erro passado some quando uma nova consulta der certo.
    let atual = true;
    listarAulas(sessao.accessToken, {
      status: status || undefined,
      turmaId: turmaId ? Number(turmaId) : undefined,
      materiaId: materiaId ? Number(materiaId) : undefined,
      dataInicio: dataDe || undefined,
      dataFim: dataAte || undefined,
      horaInicioDe: horaDe || undefined,
      horaInicioAte: horaAte || undefined,
    })
      .then((r) => {
        if (!atual) return;
        setErro("");
        setAulas(r);
      })
      .catch((excecao) => {
        if (atual) setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar as aulas.");
      });
    return () => {
      atual = false;
    };
  }, [sessao, status, turmaId, materiaId, dataDe, dataAte, horaDe, horaAte]);

  const total = aulas?.length ?? 0;
  const semFiltros = !status && !turmaId && !materiaId && !dataDe && !dataAte && !horaDe && !horaAte;

  return (
    <>
      <div className="filter-bar">
        <select className="filter-select" value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="">Status — todos</option>
          <option value="Agendada">Agendada</option>
          <option value="Realizada">Realizada</option>
          <option value="Cancelada">Cancelada</option>
        </select>
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
        <span style={grupoDeFiltro}>
          <span className="count-text">Data</span>
          <DateInput className="filter-input filter-date" value={dataDe} onChange={setDataDe} title="Data a partir de" />
          <span className="count-text">a</span>
          <DateInput className="filter-input filter-date" value={dataAte} onChange={setDataAte} title="Data até" />
        </span>
        <span style={grupoDeFiltro}>
          <span className="count-text">Horário</span>
          <TimeInput className="filter-input filter-date" value={horaDe} onChange={setHoraDe} title="Horário de início a partir de" />
          <span className="count-text">a</span>
          <TimeInput className="filter-input filter-date" value={horaAte} onChange={setHoraAte} title="Horário de início até" />
        </span>
        {!semFiltros && (
          <button
            type="button"
            className="filter-clear"
            onClick={() => {
              setStatus("");
              setTurmaId("");
              setMateriaId("");
              setDataDe("");
              setDataAte("");
              setHoraDe("");
              setHoraAte("");
            }}
          >
            <Icon name="x" size={12} /> Limpar filtros
          </button>
        )}
      </div>

      <div className="row-gap">
        <span className="count-text">{total} aula(s) encontrada(s)</span>
        <Link href="/aulas/nova" className="btn btn-primary">
          <Icon name="plus" size={13} /> Nova aula
        </Link>
      </div>

      {erro && <EmptyState title="Erro ao carregar" desc={erro} />}

      {!erro && (
        <div className="table-wrap">
          {aulas === null ? (
            <p className="count-text" style={{ padding: 24 }}>Carregando...</p>
          ) : total === 0 ? (
            <EmptyState title="Nenhuma aula encontrada" desc="Ajuste os filtros ou agende uma nova aula." />
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Data</th>
                  <th>Horário</th>
                  <th>Matéria</th>
                  <th>Turma/Aluno</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {aulas.map((a) => (
                  <tr className="row-link" key={a.id} onClick={() => router.push(`/aulas/${a.id}`)}>
                    <td>{fmtData(a.dataInicio)}</td>
                    <td>
                      {fmtHora(a.horaInicio)} — {fmtHora(a.horaFim)}
                    </td>
                    <td>{a.materiaNome}</td>
                    <td>{a.turmaNome ?? a.alunos.map((al) => al.nome).join(", ")}</td>
                    <td>
                      <StatusBadge status={a.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </>
  );
}
