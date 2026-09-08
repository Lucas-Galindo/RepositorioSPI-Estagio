"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { fmtData, fmtHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function AulasPage() {
  usePageHeader("Aulas", "Agenda de aulas individuais e de turma");
  const { sessao } = useAuth();
  const router = useRouter();
  const [aulas, setAulas] = useState<Aula[] | null>(null);
  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [erro, setErro] = useState("");
  const [status, setStatus] = useState("");
  const [turmaId, setTurmaId] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarTurmas(sessao.accessToken).then(setTurmas).catch(() => setTurmas([]));
  }, [sessao]);

  useEffect(() => {
    if (!sessao) return;
    listarAulas(sessao.accessToken, {
      status: status || undefined,
      turmaId: turmaId ? Number(turmaId) : undefined,
    })
      .then(setAulas)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar as aulas."));
  }, [sessao, status, turmaId]);

  const total = aulas?.length ?? 0;

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
