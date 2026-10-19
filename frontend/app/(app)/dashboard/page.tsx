"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { InfoTooltip } from "@/components/shared/InfoTooltip";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { obterDashboard, type Dashboard } from "@/lib/api/dashboard";
import { PacotesPanel } from "@/components/dashboard/PacotesPanel";
import { DateInput } from "@/components/shared/DateInput";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { currency, dataIsoLocal, fmtData, fmtHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

const DIAS_SEMANA = ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];

function diasDaSemana(): { chave: string; label: string; curto: string }[] {
  const hoje = new Date();
  const inicio = new Date(hoje);
  inicio.setDate(hoje.getDate() - hoje.getDay() + 1); // segunda-feira

  return Array.from({ length: 7 }, (_, i) => {
    const dia = new Date(inicio);
    dia.setDate(inicio.getDate() + i);
    const chave = dataIsoLocal(dia);
    return {
      chave,
      label: DIAS_SEMANA[dia.getDay()],
      curto: fmtData(chave).slice(0, 5),
    };
  });
}

export default function DashboardPage() {
  usePageHeader("Home", "Central operacional da sua rotina");
  const { sessao } = useAuth();
  const [aulas, setAulas] = useState<Aula[] | null>(null);
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [erro, setErro] = useState("");
  // specs/058: filtros da secao de pacotes (o periodo vale para o dashboard todo).
  const [periodoInicio, setPeriodoInicio] = useState("");
  const [periodoFim, setPeriodoFim] = useState("");
  const [alunoId, setAlunoId] = useState("");
  const [alunosAtivos, setAlunosAtivos] = useState<Aluno[]>([]);

  const semana = diasDaSemana();
  const hojeIso = dataIsoLocal();

  useEffect(() => {
    if (!sessao) return;

    listarAulas(sessao.accessToken, { dataInicio: semana[0].chave, dataFim: semana[6].chave })
      .then(setAulas)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar a agenda."));
    listarAlunos(sessao.accessToken, { ativo: true }).then(setAlunosAtivos).catch(() => setAlunosAtivos([]));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessao]);

  useEffect(() => {
    if (!sessao) return;
    obterDashboard(sessao.accessToken, {
      periodoInicio: periodoInicio || undefined,
      periodoFim: periodoFim || undefined,
      alunoId: alunoId ? Number(alunoId) : undefined,
    })
      .then(setDashboard)
      .catch(() => setDashboard(null));
  }, [sessao, periodoInicio, periodoFim, alunoId]);

  const alunosAtendidos = dashboard?.alunosAtendidosNoPeriodo ?? null;
  const recebidoMes = dashboard?.valorFaturadoNoPeriodo ?? null;

  const aulasHoje = (aulas ?? []).filter((a) => a.dataInicio === hojeIso).sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
  const proxima = aulasHoje.find((a) => a.status === "Agendada") ?? aulasHoje[0];

  return (
    <>
      <div className="stat-row">
        <div className="card card-feature">
          <div className="card-eyebrow">
            <Icon name="cal" size={13} /> Próxima aula
          </div>
          {proxima ? (
            <>
              <div className="fav-subject">{proxima.materiaNome}</div>
              <div className="fav-time">
                {fmtHora(proxima.horaInicio)} — {fmtHora(proxima.horaFim)}
              </div>
              <div className="fav-meta">
                <div className="fav-person">
                  <Icon name="users" size={13} /> {proxima.turmaNome ?? proxima.alunos.map((al) => al.nome).join(", ")}
                </div>
                <span className="badge badge-today">Hoje</span>
              </div>
            </>
          ) : (
            <div className="fav-subject" style={{ fontSize: 15, opacity: 0.8 }}>
              {aulas === null ? "Carregando..." : "Nenhuma aula restante hoje"}
            </div>
          )}
        </div>

        <div className="card card-small">
          <div className="icon-chip chip-turq">
            <Icon name="users" size={16} />
          </div>
          <div className="card-eyebrow">Alunos atendidos</div>
          <div className="big-value">{alunosAtendidos ?? "—"}</div>
          <div className="big-label">Com aula realizada este mês</div>
        </div>

        <div className="card card-small">
          <div className="icon-chip chip-gold">
            <Icon name="wallet" size={16} />
          </div>
          <div className="card-eyebrow">
            Recebido este mês
            <InfoTooltip text="Total de contas a receber que já foram pagas dentro do mês atual." />
          </div>
          <div className="big-value">{recebidoMes !== null ? currency(recebidoMes) : "—"}</div>
          <div className="big-label">Contas a receber pagas no mês</div>
        </div>
      </div>

      <div className="row-gap" style={{ marginTop: 6 }}>
        <div>
          <div className="section-title">Agenda da semana</div>
          <div className="section-sub" style={{ marginBottom: 0 }}>
            {semana[0].curto} a {semana[6].curto}
          </div>
        </div>
        <Link href="/aulas/nova" className="btn btn-primary btn-sm">
          <Icon name="plus" size={13} /> Nova aula
        </Link>
      </div>

      {erro && (
        <div className="empty-state">
          <div className="ic">
            <Icon name="warn" size={20} />
          </div>
          <p>{erro}</p>
        </div>
      )}

      {dashboard && (
        <div style={{ marginTop: 18, marginBottom: 24 }}>
          <div className="section-title">Pacotes de aulas</div>
          <div className="section-sub" style={{ marginBottom: 0 }}>
            Aulas dadas contam as presenças registradas em cada pacote
          </div>
          <div className="filter-bar" style={{ marginTop: 10 }}>
            <select className="filter-input" value={alunoId} onChange={(e) => setAlunoId(e.target.value)} title="Filtrar por aluno">
              <option value="">Todos os alunos</option>
              {alunosAtivos.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.nome}
                </option>
              ))}
            </select>
            <DateInput className="filter-input filter-date" value={periodoInicio} onChange={setPeriodoInicio} title="Período a partir de" />
            <DateInput className="filter-input filter-date" value={periodoFim} onChange={setPeriodoFim} title="Período até" />
          </div>
          <div style={{ marginTop: 10 }}>
            <PacotesPanel resumo={dashboard.pacotes} />
          </div>
        </div>
      )}

      <div className="week-grid">
        {semana.map((dia) => {
          const isToday = dia.chave === hojeIso;
          const dasAulas = (aulas ?? [])
            .filter((a) => a.dataInicio === dia.chave)
            .sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));

          return (
            <div className={`week-day${isToday ? " today" : ""}`} key={dia.chave}>
              <div className="week-day-head">
                <span>{dia.label}</span>
                <span>{dia.curto}</span>
              </div>
              {dasAulas.length ? (
                dasAulas.map((a) => (
                  <Link
                    href={`/aulas/${a.id}`}
                    className={`week-event${a.status === "Realizada" ? " st-realizada" : a.status === "Cancelada" ? " st-cancelada" : ""}`}
                    key={a.id}
                  >
                    <div className="t">
                      {fmtHora(a.horaInicio)} {a.materiaNome}
                    </div>
                    <div className="s">{a.turmaNome ?? a.alunos.map((al) => al.nome).join(", ")}</div>
                  </Link>
                ))
              ) : (
                <div className="week-empty">{aulas === null ? "Carregando..." : "Sem aulas"}</div>
              )}
            </div>
          );
        })}
      </div>
    </>
  );
}
