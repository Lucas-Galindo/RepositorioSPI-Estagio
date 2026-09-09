"use client";

import { useEffect, useState } from "react";
import { Icon } from "@/components/shared/Icon";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { AulaForm } from "@/components/aulas/AulaForm";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAulas, excluirAula, type Aula } from "@/lib/api/aulas";
import { fmtData, fmtHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

// Agenda mensal (calendario estilo Teams), restaurada a pedido do usuario:
// existia no protótipo como a propria tela "Aulas" e foi substituida, em
// algum momento, pela lista/tabela simples que hoje vive em /aulas. As
// duas coexistem: /aulas continua sendo a lista, /agenda e esta tela nova.
const DIAS_SEMANA = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];
const MESES = [
  "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
  "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro",
];

function pad2(n: number): string {
  return String(n).padStart(2, "0");
}
function nomeParticipantes(a: Aula): string {
  return a.turmaNome ?? (a.alunos.map((al) => al.nome).join(", ") || "Atendimento individual");
}

export default function AgendaPage() {
  usePageHeader("Agenda", "Agenda mensal e painel operacional de aulas");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  const hoje = new Date();
  const [ano, setAno] = useState(hoje.getFullYear());
  const [mes, setMes] = useState(hoje.getMonth() + 1);
  const [aulas, setAulas] = useState<Aula[] | null>(null);
  const [aulasHoje, setAulasHoje] = useState<Aula[]>([]);
  const [erro, setErro] = useState("");
  const [diaSelecionado, setDiaSelecionado] = useState<string | null>(null);
  const [exclusaoPendente, setExclusaoPendente] = useState<Aula | null>(null);
  const [excluindo, setExcluindo] = useState(false);
  // Popup compacto de criar/editar (em vez de navegar pra /aulas/nova ou
  // /aulas/[id]/editar): permite cadastrar ou selecionar uma aula do dia e
  // alterar as informações sem sair da Agenda.
  const [popupAula, setPopupAula] = useState<{ modo: "criar"; data: string } | { modo: "editar"; aula: Aula } | null>(null);

  const hojeIso = `${hoje.getFullYear()}-${pad2(hoje.getMonth() + 1)}-${pad2(hoje.getDate())}`;
  const primeiroDiaMes = `${ano}-${pad2(mes)}-01`;
  const ultimoDiaDoMes = new Date(ano, mes, 0).getDate();
  const ultimoDiaMes = `${ano}-${pad2(mes)}-${pad2(ultimoDiaDoMes)}`;

  const carregarMes = () => {
    if (!sessao) return;
    listarAulas(sessao.accessToken, { dataInicio: primeiroDiaMes, dataFim: ultimoDiaMes })
      .then(setAulas)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar a agenda."));
  };

  useEffect(() => {
    carregarMes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessao, ano, mes]);

  useEffect(() => {
    if (!sessao) return;
    listarAulas(sessao.accessToken, { dataInicio: hojeIso, dataFim: hojeIso })
      .then(setAulasHoje)
      .catch(() => setAulasHoje([]));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessao]);

  const mudarMes = (delta: number) => {
    let m = mes + delta;
    let y = ano;
    if (m > 12) {
      m = 1;
      y += 1;
    } else if (m < 1) {
      m = 12;
      y -= 1;
    }
    setMes(m);
    setAno(y);
  };
  const irParaHoje = () => {
    setAno(hoje.getFullYear());
    setMes(hoje.getMonth() + 1);
  };

  const lista = aulas ?? [];
  const agendadas = lista.filter((a) => a.status === "Agendada").length;
  const realizadas = lista.filter((a) => a.status === "Realizada").length;
  const canceladas = lista.filter((a) => a.status === "Cancelada").length;
  const proximasDoMes = lista
    .filter((a) => a.dataInicio >= hojeIso && a.status === "Agendada")
    .sort((a, b) => (a.dataInicio + a.horaInicio).localeCompare(b.dataInicio + b.horaInicio))
    .slice(0, 5);

  const primeiroDiaSemana = new Date(ano, mes - 1, 1).getDay();
  const celulas: (number | null)[] = [];
  for (let i = 0; i < primeiroDiaSemana; i++) celulas.push(null);
  for (let d = 1; d <= ultimoDiaDoMes; d++) celulas.push(d);
  while (celulas.length % 7 !== 0) celulas.push(null);

  const aulasDoDia = (iso: string) => lista.filter((a) => a.dataInicio === iso).sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));

  const handleExcluir = async () => {
    if (!sessao || !exclusaoPendente) return;
    setExcluindo(true);
    try {
      await excluirAula(exclusaoPendente.id, sessao.accessToken);
      mostrarToast("Aula excluída com sucesso.");
      setExclusaoPendente(null);
      carregarMes();
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir a aula.");
    } finally {
      setExcluindo(false);
    }
  };

  const diaModal = diaSelecionado ? aulasDoDia(diaSelecionado) : [];

  return (
    <>
      <div className="kpi-row">
        <div className="kpi">
          <div className="label">
            <Icon name="cal" size={12} /> Hoje
          </div>
          <div className="value turq">{aulasHoje.length}</div>
          <div className="delta">aulas programadas</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="fwd" size={12} /> Agendadas
          </div>
          <div className="value">{agendadas}</div>
          <div className="delta">no mês selecionado</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="check" size={12} /> Realizadas
          </div>
          <div className="value">{realizadas}</div>
          <div className="delta">no mês selecionado</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="x" size={12} /> Canceladas
          </div>
          <div className="value danger">{canceladas}</div>
          <div className="delta">no mês selecionado</div>
        </div>
      </div>

      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}

      <div className="grid-2">
        <div className="panel">
          <div className="row-gap">
            <div>
              <div className="section-title">Agenda mensal</div>
              <div className="section-sub" style={{ marginBottom: 0 }}>
                Clique em uma data para agendar uma aula, ou gerenciar as aulas já marcadas nela.
              </div>
            </div>
          </div>

          <div className="cal-toolbar">
            <div className="cal-nav">
              <button type="button" className="cal-nav-btn" onClick={() => mudarMes(-1)}>
                <Icon name="back" size={14} />
              </button>
              <div className="cal-month-label">
                {MESES[mes - 1]} de {ano}
              </div>
              <button type="button" className="cal-nav-btn" onClick={() => mudarMes(1)}>
                <Icon name="fwd" size={14} />
              </button>
              <button type="button" className="cal-today-btn" onClick={irParaHoje}>
                Hoje
              </button>
            </div>
            <div className="cal-legend">
              <div className="cal-legend-item">
                <span className="cal-legend-dot dot-agendada" /> Agendada
              </div>
              <div className="cal-legend-item">
                <span className="cal-legend-dot dot-realizada" /> Realizada
              </div>
              <div className="cal-legend-item">
                <span className="cal-legend-dot dot-cancelada" /> Cancelada
              </div>
            </div>
          </div>

          <div className="cal-grid">
            {DIAS_SEMANA.map((w) => (
              <div className="cal-weekday" key={w}>
                {w}
              </div>
            ))}
            {celulas.map((d, i) => {
              if (!d) return <div className="cal-cell outside" key={`vazia-${i}`} />;
              const iso = `${ano}-${pad2(mes)}-${pad2(d)}`;
              const isToday = iso === hojeIso;
              const das = aulasDoDia(iso);
              const mostradas = das.slice(0, 3);
              const resto = das.length - mostradas.length;
              return (
                <div
                  className={`cal-cell${isToday ? " today" : ""}`}
                  key={iso}
                  onClick={() => setDiaSelecionado(iso)}
                >
                  <div className="cal-date">
                    <span>{d}</span>
                    <span className="add-hint">
                      <Icon name="plus" size={10} />
                    </span>
                  </div>
                  {mostradas.map((a) => (
                    <div
                      className={`cal-event${a.status === "Realizada" ? " st-realizada" : a.status === "Cancelada" ? " st-cancelada" : ""}`}
                      key={a.id}
                      onClick={(e) => {
                        e.stopPropagation();
                        setPopupAula({ modo: "editar", aula: a });
                      }}
                    >
                      <b>{fmtHora(a.horaInicio)}</b> {a.materiaNome}
                    </div>
                  ))}
                  {resto > 0 && (
                    <div
                      className="cal-more"
                      onClick={(e) => {
                        e.stopPropagation();
                        setDiaSelecionado(iso);
                      }}
                    >
                      +{resto} mais
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
          <div className="panel">
            <div className="section-title">Próximas aulas do mês</div>
            <div className="section-sub">A partir de hoje</div>
            {proximasDoMes.length ? (
              proximasDoMes.map((a) => (
                <div className="lesson-item" key={a.id} onClick={() => setPopupAula({ modo: "editar", aula: a })}>
                  <div className="lesson-time">{fmtHora(a.horaInicio)}</div>
                  <div className="lesson-info">
                    <div className="subj">{a.materiaNome}</div>
                    <div className="who">
                      {fmtData(a.dataInicio)} · {nomeParticipantes(a)}
                    </div>
                  </div>
                  <StatusBadge status={a.status} />
                </div>
              ))
            ) : (
              <EmptyState title="Nenhuma aula futura neste mês" desc="Clique em uma data no calendário para agendar uma nova aula." />
            )}
          </div>
        </div>
      </div>

      {diaSelecionado && (
        <div className="modal-overlay" onClick={() => setDiaSelecionado(null)}>
          <div className="modal modal-lg" onClick={(e) => e.stopPropagation()}>
            <h3>{fmtData(diaSelecionado)}</h3>
            <p style={{ marginBottom: 14 }}>{diaModal.length} aula(s) neste dia. Clique para ver os detalhes, ou gerencie rapidamente abaixo.</p>
            <div style={{ display: "flex", flexDirection: "column", gap: 8, maxHeight: 280, overflow: "auto", marginBottom: 18 }}>
              {diaModal.length ? (
                diaModal.map((a) => (
                  <div
                    className="lesson-item"
                    key={a.id}
                    style={{ cursor: "pointer" }}
                    onClick={() => {
                      setDiaSelecionado(null);
                      setPopupAula({ modo: "editar", aula: a });
                    }}
                    title="Clique para selecionar e alterar esta aula"
                  >
                    <div className="lesson-time">{fmtHora(a.horaInicio)}</div>
                    <div className="lesson-info">
                      <div className="subj">{a.materiaNome}</div>
                      <div className="who">{nomeParticipantes(a)}</div>
                    </div>
                    <StatusBadge status={a.status} />
                    <button
                      type="button"
                      className="icon-round"
                      style={{ width: 28, height: 28, borderRadius: 9, color: "var(--c-danger)" }}
                      title="Excluir aula"
                      onClick={(e) => {
                        e.stopPropagation();
                        setExclusaoPendente(a);
                      }}
                    >
                      <Icon name="trash" size={13} />
                    </button>
                  </div>
                ))
              ) : (
                <EmptyState title="Nenhuma aula neste dia" desc="Agende uma nova aula para esta data." />
              )}
            </div>
            <div className="modal-actions" style={{ justifyContent: "space-between" }}>
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => setDiaSelecionado(null)}>
                Fechar
              </button>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={() => {
                  if (!diaSelecionado) return;
                  setPopupAula({ modo: "criar", data: diaSelecionado });
                  setDiaSelecionado(null);
                }}
              >
                <Icon name="plus" size={13} /> Agendar aula neste dia
              </button>
            </div>
          </div>
        </div>
      )}

      {exclusaoPendente && (
        <ConfirmModal
          titulo="Excluir aula?"
          descricao="Esta ação não poderá ser desfeita."
          onConfirmar={handleExcluir}
          onCancelar={() => setExclusaoPendente(null)}
          confirmando={excluindo}
        />
      )}

      {popupAula && (
        <div className="modal-overlay" onClick={() => setPopupAula(null)}>
          <div className="modal modal-aula-popup" onClick={(e) => e.stopPropagation()}>
            <h3>{popupAula.modo === "editar" ? "Editar aula" : "Agendar aula"}</h3>
            <AulaForm
              aula={popupAula.modo === "editar" ? popupAula.aula : undefined}
              dataPadrao={popupAula.modo === "criar" ? popupAula.data : undefined}
              onSalvo={() => {
                setPopupAula(null);
                carregarMes();
              }}
              onCancelar={() => setPopupAula(null)}
            />
          </div>
        </div>
      )}
    </>
  );
}
