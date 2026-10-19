"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { listarNotificacoes, marcarNotificacaoComoLida, type Notificacao } from "@/lib/api/notificacoes";
import { fmtData, fmtDataHora, fmtHora } from "@/lib/format";

// Com a tela aberta, o sino reconsulta a cada minuto (spec 059, Assumptions).
const INTERVALO_MS = 60_000;

// specs/059: sino do cabecalho. So exibe o que a API devolve -- a regra de
// "aula pendente" (e quando o aviso some) fica toda no backend.
export function NotificacoesSino() {
  const { sessao } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [notificacoes, setNotificacoes] = useState<Notificacao[]>([]);
  const [aberto, setAberto] = useState(false);
  const [selecionada, setSelecionada] = useState<Notificacao | null>(null);
  const painelRef = useRef<HTMLDivElement>(null);

  const carregar = useCallback(() => {
    if (!sessao) return;
    listarNotificacoes(sessao.accessToken)
      .then(setNotificacoes)
      // Falha de rede nao deve quebrar o cabecalho; mantem a ultima lista.
      .catch(() => undefined);
  }, [sessao]);

  // Ao montar, a cada troca de tela (o aviso some logo depois de registrar a
  // aula e voltar) e periodicamente.
  useEffect(() => {
    carregar();
    const timer = setInterval(carregar, INTERVALO_MS);
    return () => clearInterval(timer);
  }, [carregar, pathname]);

  // Fecha o painel ao clicar fora dele.
  useEffect(() => {
    if (!aberto) return;
    const fechar = (e: MouseEvent) => {
      if (painelRef.current && !painelRef.current.contains(e.target as Node)) setAberto(false);
    };
    document.addEventListener("mousedown", fechar);
    return () => document.removeEventListener("mousedown", fechar);
  }, [aberto]);

  const naoLidas = notificacoes.filter((n) => !n.lida).length;

  const abrir = (n: Notificacao) => {
    setSelecionada(n);
    setAberto(false);
    if (!n.lida && sessao) {
      // Abrir marca como lida; a notificacao so sai quando a aula for atualizada.
      setNotificacoes((lista) => lista.map((x) => (x.id === n.id ? { ...x, lida: true } : x)));
      marcarNotificacaoComoLida(n.id, sessao.accessToken).catch(() => undefined);
    }
  };

  const irParaAula = () => {
    if (!selecionada?.aula) return;
    router.push(`/aulas/${selecionada.aula.id}`);
    setSelecionada(null);
  };

  return (
    <div className="notif-wrap" ref={painelRef}>
      <button
        type="button"
        className="icon-round"
        aria-label={naoLidas ? `Notificações (${naoLidas} não lidas)` : "Notificações"}
        onClick={() => setAberto((v) => !v)}
      >
        <Icon name="bell" size={15} />
        {naoLidas > 0 && <span className="notif-count">{naoLidas > 9 ? "9+" : naoLidas}</span>}
      </button>

      {aberto && (
        <div className="notif-panel">
          <div className="notif-panel-head">Notificações</div>
          {notificacoes.length === 0 ? (
            <p className="notif-empty">Nenhuma pendência. Tudo em dia!</p>
          ) : (
            notificacoes.map((n) => (
              <button type="button" key={n.id} className={`notif-item${n.lida ? "" : " unread"}`} onClick={() => abrir(n)}>
                <span className="notif-tag">Aula</span>
                <span className="notif-text">
                  <span className="notif-title">{n.aula ? `${n.aula.materiaNome} · ${fmtData(n.aula.data)}` : n.mensagem}</span>
                  <span className="notif-sub">{n.aula?.contexto}</span>
                </span>
              </button>
            ))
          )}
        </div>
      )}

      {selecionada && (
        <div className="modal-overlay" onClick={() => setSelecionada(null)}>
          <div className="modal notif-modal" onClick={(e) => e.stopPropagation()}>
            <div className="ic">
              <Icon name="bell" size={18} />
            </div>
            <h3>Aula sem status</h3>
            <p>{selecionada.mensagem}</p>
            {selecionada.aula && (
              <dl className="notif-detalhes">
                <dt>Data</dt>
                <dd>
                  {fmtData(selecionada.aula.data)}, {fmtHora(selecionada.aula.horaInicio)} — {fmtHora(selecionada.aula.horaFim)}
                </dd>
                <dt>Matéria</dt>
                <dd>{selecionada.aula.materiaNome}</dd>
                <dt>Turma</dt>
                <dd>{selecionada.aula.contexto}</dd>
                <dt>Aviso desde</dt>
                <dd>{fmtDataHora(selecionada.criadaEm)}</dd>
              </dl>
            )}
            <div className="modal-actions">
              <button type="button" className="btn btn-ghost" onClick={() => setSelecionada(null)}>
                Fechar
              </button>
              {selecionada.aula && (
                <button type="button" className="btn btn-primary" onClick={irParaAula}>
                  Ir para a aula
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
