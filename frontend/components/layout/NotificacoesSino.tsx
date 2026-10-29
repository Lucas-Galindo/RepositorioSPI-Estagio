"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { listarNotificacoes, marcarNotificacaoComoLida, type Notificacao, type TipoNotificacao } from "@/lib/api/notificacoes";
import { currency, fmtData, fmtDataHora, fmtHora } from "@/lib/format";

// Com a tela aberta, o sino reconsulta a cada minuto (spec 059, Assumptions).
const INTERVALO_MS = 60_000;

// specs/060: rotulo na lista e titulo do popup de cada tipo.
const TIPOS: Record<TipoNotificacao, { rotulo: string; titulo: string; grupo: "Aulas" | "Pagamentos" }> = {
  AulaPendente: { rotulo: "Aula", titulo: "Aula sem status", grupo: "Aulas" },
  AulaFutura: { rotulo: "Próxima aula", titulo: "Aula em breve", grupo: "Aulas" },
  PagamentoPendente: { rotulo: "Pagamento", titulo: "Pagamento pendente", grupo: "Pagamentos" },
};

const FILTROS = [
  { id: "Todas", rotulo: "Todas" },
  { id: "Aulas", rotulo: "Aulas" },
  { id: "Pagamentos", rotulo: "Pagamentos" },
] as const;

// specs/059/060: centro de notificacoes do cabecalho. So exibe o que a API
// devolve -- quando cada aviso nasce e some fica todo no backend.
export function NotificacoesSino() {
  const { sessao } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [notificacoes, setNotificacoes] = useState<Notificacao[]>([]);
  const [aberto, setAberto] = useState(false);
  const [filtro, setFiltro] = useState<(typeof FILTROS)[number]["id"]>("Todas");
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

  // O contador soma as nao lidas de todos os tipos (FR-013), independente do filtro.
  const naoLidas = notificacoes.filter((n) => !n.lida).length;
  const visiveis = filtro === "Todas" ? notificacoes : notificacoes.filter((n) => TIPOS[n.tipo].grupo === filtro);

  const abrir = (n: Notificacao) => {
    setSelecionada(n);
    setAberto(false);
    if (!n.lida && sessao) {
      // Abrir marca como lida; a notificacao so sai quando a situacao for atualizada.
      setNotificacoes((lista) => lista.map((x) => (x.id === n.id ? { ...x, lida: true } : x)));
      marcarNotificacaoComoLida(n.id, sessao.accessToken).catch(() => undefined);
    }
  };

  // Clicar na pendencia leva direto ao registro do pagamento (mesmo padrao das
  // tabelas das outras telas: linha clicavel, sem aparencia de link).
  const irParaPagamento = (pagamentoId: number) => {
    router.push(`/financeiro/contas-a-receber/${pagamentoId}`);
    setSelecionada(null);
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
          <div className="notif-filtros" role="tablist">
            {FILTROS.map((f) => (
              <button
                type="button"
                role="tab"
                key={f.id}
                aria-selected={filtro === f.id}
                className={`notif-filtro${filtro === f.id ? " active" : ""}`}
                onClick={() => setFiltro(f.id)}
              >
                {f.rotulo}
              </button>
            ))}
          </div>
          {visiveis.length === 0 ? (
            <p className="notif-empty">Nenhuma pendência. Tudo em dia!</p>
          ) : (
            visiveis.map((n) => (
              <button type="button" key={n.id} className={`notif-item${n.lida ? "" : " unread"}`} onClick={() => abrir(n)}>
                <span className={`notif-tag${n.pagamento ? " pagamento" : ""}`}>{TIPOS[n.tipo].rotulo}</span>
                <span className="notif-text">
                  <span className="notif-title">
                    {n.aula ? `${n.aula.materiaNome} · ${fmtData(n.aula.data)}` : n.pagamento?.alunoNome ?? n.mensagem}
                  </span>
                  <span className="notif-sub">
                    {n.aula?.contexto ??
                      (n.pagamento &&
                        `${n.pagamento.pendencias.length} ${n.pagamento.pendencias.length === 1 ? "pagamento vencido" : "pagamentos vencidos"}`)}
                  </span>
                </span>
              </button>
            ))
          )}
        </div>
      )}

      {selecionada && (
        <div className="modal-overlay" onClick={() => setSelecionada(null)}>
          <div className={`modal notif-modal${selecionada.pagamento ? " notif-modal-wide" : ""}`} onClick={(e) => e.stopPropagation()}>
            <div className="ic">
              <Icon name={selecionada.pagamento ? "money" : "bell"} size={18} />
            </div>
            <h3>{TIPOS[selecionada.tipo].titulo}</h3>
            <p>{selecionada.mensagem}</p>
            {selecionada.pagamento && (
              <div className="table-wrap notif-pendencias">
                <table>
                  <thead>
                    <tr>
                      <th>Descrição</th>
                      <th>Valor</th>
                      <th>Vencimento</th>
                      <th>Atraso</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selecionada.pagamento.pendencias.map((p) => (
                      <tr className="row-link" key={p.pagamentoId} onClick={() => irParaPagamento(p.pagamentoId)}>
                        <td>{p.descricao}</td>
                        <td>{currency(p.valor)}</td>
                        <td>{fmtData(p.vencimento)}</td>
                        <td>{p.diasAtraso === 1 ? "1 dia" : `${p.diasAtraso} dias`}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
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
                  {selecionada.tipo === "AulaFutura" ? "Abrir aula" : "Ir para a aula"}
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
