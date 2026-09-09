"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { obterDashboard, type Dashboard } from "@/lib/api/dashboard";
import { currency, fmtHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

const DIAS_SEMANA = ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];

function diasDaSemana(): { chave: string; label: string; curto: string }[] {
  const hoje = new Date();
  const inicio = new Date(hoje);
  inicio.setDate(hoje.getDate() - hoje.getDay() + 1); // segunda-feira

  return Array.from({ length: 7 }, (_, i) => {
    const dia = new Date(inicio);
    dia.setDate(inicio.getDate() + i);
    const chave = dia.toISOString().slice(0, 10);
    return {
      chave,
      label: DIAS_SEMANA[dia.getDay()],
      curto: dia.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" }),
    };
  });
}

export default function DashboardPage() {
  usePageHeader("Home", "Central operacional da sua rotina");
  const { sessao } = useAuth();
  const [aulas, setAulas] = useState<Aula[] | null>(null);
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [erro, setErro] = useState("");

  const semana = diasDaSemana();
  const hojeIso = new Date().toISOString().slice(0, 10);

  useEffect(() => {
    if (!sessao) return;

    listarAulas(sessao.accessToken, { dataInicio: semana[0].chave, dataFim: semana[6].chave })
      .then(setAulas)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar a agenda."));
    obterDashboard(sessao.accessToken).then(setDashboard).catch(() => setDashboard(null));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessao]);

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
          <div className="card-eyebrow">Recebido este mês</div>
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
                      {fmtHora(a.horaInicio)} · {a.materiaNome}
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
