"use client";

import { useEffect, useLayoutEffect, useRef, useState, useSyncExternalStore, type KeyboardEvent } from "react";
import { createPortal } from "react-dom";
import { Icon } from "@/components/shared/Icon";
import { dataIsoLocal } from "@/lib/format";
import {
  ANO_MAX,
  ANO_MIN,
  DIAS_ABREVIADOS,
  MESES,
  MESES_CURTOS,
  adicionarDias,
  adicionarMeses,
  anosDaPagina,
  diaDaSemana,
  diasDoMes,
  estaBloqueado,
  lerIso,
  lerMes,
  montarGrade,
  montarIso,
  montarMes,
  rotuloDoDia,
  tituloDoMes,
} from "@/lib/calendario";

/**
 * specs/070: calendario proprio do sistema (substitui o do navegador da spec 069). Tres visoes no mesmo painel:
 * dias, meses e anos. So tokens de cor do tema (classes `.cal-*` em styles/dashboard.css). Datas sao texto
 * (`yyyy-MM-dd` / `yyyy-MM`), nunca `Date`: ver `lib/calendario.ts`.
 *
 * O painel e desenhado num portal em `document.body` (camada 120), posicionado pelo campo, para nao ser cortado
 * por modais e tabelas que rolam. Ate 640 px vira janela centralizada sobre o fundo escurecido `.modal-overlay`.
 */

type Visao = "dias" | "meses" | "anos";

const DIAS_COMPLETOS = ["domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado"];
const CONSULTA_CELULAR = "(max-width: 640px)";
const MARGEM = 8;
const DISTANCIA_DO_CAMPO = 6;

function assinarCelular(aoMudar: () => void) {
  const consulta = window.matchMedia(CONSULTA_CELULAR);
  consulta.addEventListener("change", aoMudar);
  return () => consulta.removeEventListener("change", aoMudar);
}
const lerCelular = () => window.matchMedia(CONSULTA_CELULAR).matches;
const celularNoServidor = () => false;

function capitalizar(texto: string): string {
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

function limitar(valor: number, minimo: number, maximo: number): number {
  return Math.min(Math.max(valor, minimo), maximo);
}

/** Mes (e dia) em que o calendario abre: o do valor, se completo e valido; senao o de hoje. */
function pontoInicial(modo: "dia" | "mes", valor: string, hoje: string): { ano: number; mes: number; dia: number } {
  if (modo === "mes") {
    const m = lerMes(valor);
    if (m) return { ano: m.ano, mes: m.mes, dia: 1 };
  } else {
    const p = lerIso(valor);
    if (p) return p;
  }
  return lerIso(hoje) ?? { ano: 2026, mes: 1, dia: 1 };
}

/** Posiciona o painel abaixo do campo (ou acima, se faltar espaco), dentro da tela. Mexe so no estilo. */
function posicionar(painel: HTMLElement, ancora: HTMLElement) {
  const campo = ancora.getBoundingClientRect();
  const largura = painel.offsetWidth;
  const altura = painel.offsetHeight;
  const esquerda = limitar(campo.left, MARGEM, Math.max(MARGEM, window.innerWidth - largura - MARGEM));
  let topo = campo.bottom + DISTANCIA_DO_CAMPO;
  if (topo + altura > window.innerHeight - MARGEM) {
    const acima = campo.top - DISTANCIA_DO_CAMPO - altura;
    topo = acima >= MARGEM ? acima : Math.max(MARGEM, window.innerHeight - altura - MARGEM);
  }
  painel.style.left = `${esquerda}px`;
  painel.style.top = `${topo}px`;
}

function campoForaDaTela(ancora: HTMLElement): boolean {
  const r = ancora.getBoundingClientRect();
  return r.bottom <= 0 || r.top >= window.innerHeight || r.right <= 0 || r.left >= window.innerWidth;
}

export interface CalendarioProps {
  /** "dia" abre nos dias (valor `yyyy-MM-dd`); "mes" (competencia) abre nos meses (valor `yyyy-MM`). */
  modo: "dia" | "mes";
  valor: string;
  /** `yyyy-MM-dd`: dias anteriores ficam bloqueados (so no modo "dia"). */
  min?: string;
  /** Involucro do campo, para posicionar o painel e tratar o clique no botao que o abre. */
  ancora: HTMLElement | null;
  aoEscolher: (valor: string) => void;
  aoLimpar: () => void;
  /** Esc, clique fora, escolha, "Hoje" ou "Limpar". Quem abriu devolve o foco ao botao. */
  aoFechar: () => void;
}

export function Calendario({ modo, valor, min, ancora, aoEscolher, aoLimpar, aoFechar }: CalendarioProps) {
  const [hoje] = useState(() => dataIsoLocal());
  const [inicial] = useState(() => pontoInicial(modo, valor, hoje));
  const [visao, setVisao] = useState<Visao>(modo === "mes" ? "meses" : "dias");
  const [ano, setAno] = useState(inicial.ano);
  const [mes, setMes] = useState(inicial.mes);
  const [foco, setFoco] = useState(modo === "dia" ? montarIso(inicial.ano, inicial.mes, inicial.dia) : montarIso(inicial.ano, inicial.mes, 1));
  const [focoMes, setFocoMes] = useState(inicial.mes);
  const [anoPagina, setAnoPagina] = useState(inicial.ano);
  const [focoAno, setFocoAno] = useState(inicial.ano);

  const celular = useSyncExternalStore(assinarCelular, lerCelular, celularNoServidor);
  const painelRef = useRef<HTMLDivElement>(null);
  const pedirFoco = useRef(true);
  const aoFecharRef = useRef(aoFechar);

  useEffect(() => {
    aoFecharRef.current = aoFechar;
  });

  const parteHoje = lerIso(hoje);
  const selecionadoDia = modo === "dia" && lerIso(valor) ? valor : "";
  const selecionadoMes = modo === "mes" ? lerMes(valor) : null;

  // Foco movel: so o dia/mes/ano "atual" da navegacao entra na ordem do Tab. Se a navegacao saiu do mes exibido
  // (setas do cabecalho), o foco cai no mesmo dia, ajustado ao tamanho do mes.
  const mesTexto = montarMes(ano, mes);
  const focoDia = foco.startsWith(mesTexto) ? foco : montarIso(ano, mes, Math.min(Number(foco.slice(8, 10)) || 1, diasDoMes(ano, mes)));
  const anosPagina = anosDaPagina(anoPagina);
  const focoAnoEfetivo = anosPagina.includes(focoAno) ? focoAno : anosPagina[0];

  // Foco entra no painel ao abrir e acompanha a navegacao por teclado / troca de visao (nunca por clique nas setas).
  useEffect(() => {
    if (!pedirFoco.current) return;
    pedirFoco.current = false;
    painelRef.current?.querySelector<HTMLElement>('[data-foco="true"]')?.focus();
  });

  // Posicao: reaplicada a cada renderizacao (a altura pode mudar) e na rolagem/redimensionamento.
  useLayoutEffect(() => {
    if (!celular && painelRef.current && ancora) posicionar(painelRef.current, ancora);
  });

  useEffect(() => {
    if (celular || !ancora) return;
    function reposicionar(e: Event) {
      const painel = painelRef.current;
      if (!painel || !ancora) return;
      if (e.target instanceof Node && painel.contains(e.target)) return;
      if (campoForaDaTela(ancora)) {
        aoFecharRef.current();
        return;
      }
      posicionar(painel, ancora);
    }
    window.addEventListener("scroll", reposicionar, true);
    window.addEventListener("resize", reposicionar);
    return () => {
      window.removeEventListener("scroll", reposicionar, true);
      window.removeEventListener("resize", reposicionar);
    };
  }, [celular, ancora]);

  // Esc (em captura, para nao fechar tambem o modal que contem o campo) e clique/toque fora.
  useEffect(() => {
    function aoTeclar(e: globalThis.KeyboardEvent) {
      if (e.key !== "Escape") return;
      e.preventDefault();
      e.stopPropagation();
      aoFecharRef.current();
    }
    function aoApontar(e: PointerEvent) {
      const alvo = e.target;
      if (!(alvo instanceof Element)) return;
      if (painelRef.current?.contains(alvo)) return;
      // O botao do proprio campo alterna (o clique dele fecha); o de outro campo conta como "fora".
      if (ancora?.contains(alvo) && alvo.closest(".date-cal-btn")) return;
      aoFecharRef.current();
    }
    document.addEventListener("keydown", aoTeclar, true);
    document.addEventListener("pointerdown", aoApontar, true);
    return () => {
      document.removeEventListener("keydown", aoTeclar, true);
      document.removeEventListener("pointerdown", aoApontar, true);
    };
  }, [ancora]);

  function concluir(escolhido: string) {
    aoEscolher(escolhido);
    aoFechar();
  }

  function escolherDia(iso: string) {
    if (estaBloqueado(iso, min) || !lerIso(iso)) return;
    concluir(iso);
  }

  function abrirMeses() {
    setFocoMes(mes);
    setVisao("meses");
    pedirFoco.current = true;
  }

  function abrirAnos() {
    setAnoPagina(ano);
    setFocoAno(ano);
    setVisao("anos");
    pedirFoco.current = true;
  }

  function escolherAno(a: number) {
    setAno(a);
    setFocoMes(mes);
    setVisao("meses");
    pedirFoco.current = true;
  }

  function escolherMes(m: number) {
    if (modo === "mes") {
      concluir(montarMes(ano, m));
      return;
    }
    setMes(m);
    setFoco(montarIso(ano, m, Math.min(Number(focoDia.slice(8, 10)) || 1, diasDoMes(ano, m))));
    setVisao("dias");
    pedirFoco.current = true;
  }

  function irParaMes(delta: number) {
    const r = adicionarMeses(ano, mes, delta);
    if (r.ano < ANO_MIN || r.ano > ANO_MAX) return;
    setAno(r.ano);
    setMes(r.mes);
  }

  function irParaAno(delta: number) {
    const novo = ano + delta;
    if (novo < ANO_MIN || novo > ANO_MAX) return;
    setAno(novo);
  }

  function irParaPagina(delta: number) {
    const novo = limitar(anoPagina + 12 * delta, ANO_MIN, ANO_MAX);
    if (anosDaPagina(novo)[0] === anosPagina[0]) return;
    setAnoPagina(novo);
  }

  function escolherHoje() {
    if (modo === "mes") {
      concluir(montarMes(parteHoje?.ano ?? ano, parteHoje?.mes ?? mes));
      return;
    }
    if (estaBloqueado(hoje, min)) return;
    concluir(hoje);
  }

  function limpar() {
    aoLimpar();
    aoFechar();
  }

  // ---- teclado ----
  function moverDia(iso: string) {
    const p = lerIso(iso);
    if (!p) return;
    setFoco(iso);
    setAno(p.ano);
    setMes(p.mes);
    pedirFoco.current = true;
  }

  function teclasDias(e: KeyboardEvent<HTMLDivElement>) {
    const dia = Number(focoDia.slice(8, 10));
    const semana = diaDaSemana(ano, mes, dia);
    let destino: string | null = null;
    switch (e.key) {
      case "ArrowLeft": destino = adicionarDias(focoDia, -1); break;
      case "ArrowRight": destino = adicionarDias(focoDia, 1); break;
      case "ArrowUp": destino = adicionarDias(focoDia, -7); break;
      case "ArrowDown": destino = adicionarDias(focoDia, 7); break;
      case "Home": destino = adicionarDias(focoDia, -semana); break;
      case "End": destino = adicionarDias(focoDia, 6 - semana); break;
      case "PageUp":
      case "PageDown": {
        const r = adicionarMeses(ano, mes, (e.key === "PageUp" ? -1 : 1) * (e.shiftKey ? 12 : 1));
        destino = montarIso(r.ano, r.mes, Math.min(dia, diasDoMes(r.ano, r.mes)));
        break;
      }
      default: return;
    }
    e.preventDefault();
    moverDia(destino);
  }

  function teclasGrade3(
    e: KeyboardEvent<HTMLDivElement>,
    atual: number,
    primeiro: number,
    ultimo: number,
    mover: (valor: number) => void,
    pagina?: (delta: number) => void,
  ) {
    let destino: number;
    switch (e.key) {
      case "ArrowLeft": destino = atual - 1; break;
      case "ArrowRight": destino = atual + 1; break;
      case "ArrowUp": destino = atual - 3; break;
      case "ArrowDown": destino = atual + 3; break;
      case "Home": destino = primeiro; break;
      case "End": destino = ultimo; break;
      case "PageUp":
      case "PageDown":
        if (!pagina) return;
        e.preventDefault();
        pagina(e.key === "PageUp" ? -1 : 1);
        pedirFoco.current = true;
        return;
      default: return;
    }
    e.preventDefault();
    mover(limitar(destino, primeiro, ultimo));
    pedirFoco.current = true;
  }

  // Foco preso no painel: Tab no ultimo volta ao primeiro (e Shift+Tab no primeiro vai ao ultimo).
  function prenderFoco(e: KeyboardEvent<HTMLDivElement>) {
    if (e.key !== "Tab" || !painelRef.current) return;
    const itens = Array.from(painelRef.current.querySelectorAll<HTMLElement>('button:not([disabled]):not([tabindex="-1"])'));
    if (itens.length === 0) return;
    const primeiro = itens[0];
    const ultimo = itens[itens.length - 1];
    if (e.shiftKey && document.activeElement === primeiro) {
      e.preventDefault();
      ultimo.focus();
    } else if (!e.shiftKey && document.activeElement === ultimo) {
      e.preventDefault();
      primeiro.focus();
    }
  }

  // ---- cabecalho ----
  const noLimiteAnterior =
    visao === "dias" ? ano === ANO_MIN && mes === 1 : visao === "meses" ? ano <= ANO_MIN : anosPagina[0] <= ANO_MIN;
  const noLimiteProximo =
    visao === "dias" ? ano === ANO_MAX && mes === 12 : visao === "meses" ? ano >= ANO_MAX : anosPagina[anosPagina.length - 1] >= ANO_MAX;
  const rotuloAnterior = visao === "dias" ? "Mês anterior" : visao === "meses" ? "Ano anterior" : "Anos anteriores";
  const rotuloProximo = visao === "dias" ? "Próximo mês" : visao === "meses" ? "Próximo ano" : "Próximos anos";

  function anterior() {
    if (visao === "dias") irParaMes(-1);
    else if (visao === "meses") irParaAno(-1);
    else irParaPagina(-1);
  }
  function proximo() {
    if (visao === "dias") irParaMes(1);
    else if (visao === "meses") irParaAno(1);
    else irParaPagina(1);
  }

  const hojeBloqueado = modo === "dia" && estaBloqueado(hoje, min);
  const grade = visao === "dias" ? montarGrade(ano, mes) : [];

  const painel = (
    <div ref={painelRef} className="cal-painel" role="dialog" aria-modal="true" aria-label="Calendário" onKeyDown={prenderFoco}>
      <div className="cal-topo">
        <button type="button" className="cal-nav" aria-label={rotuloAnterior} disabled={noLimiteAnterior} onClick={anterior}>
          <Icon name="back" size={14} />
        </button>
        {visao === "dias" && (
          <button type="button" className="cal-titulo" aria-live="polite" title="Escolher mês e ano" onClick={abrirMeses}>
            {capitalizar(tituloDoMes(ano, mes))}
          </button>
        )}
        {visao === "meses" && (
          <button type="button" className="cal-titulo" aria-live="polite" title="Escolher o ano" onClick={abrirAnos}>
            {ano}
          </button>
        )}
        {visao === "anos" && (
          <div className="cal-titulo cal-titulo-fixo" aria-live="polite">
            {anosPagina[0]} – {anosPagina[anosPagina.length - 1]}
          </div>
        )}
        <button type="button" className="cal-nav cal-nav-prox" aria-label={rotuloProximo} disabled={noLimiteProximo} onClick={proximo}>
          <Icon name="back" size={14} />
        </button>
      </div>

      {visao === "dias" && (
        <div role="grid" aria-label={tituloDoMes(ano, mes)} onKeyDown={teclasDias}>
          <div role="row" className="cal-semana">
            {DIAS_ABREVIADOS.map((letra, i) => (
              <span key={i} role="columnheader" aria-label={DIAS_COMPLETOS[i]}>
                {letra}
              </span>
            ))}
          </div>
          {grade.map((semana, s) => (
            <div key={s} role="row" className="cal-linha">
              {semana.map((c) => {
                const bloqueado = estaBloqueado(c.iso, min) || !lerIso(c.iso);
                const comFoco = c.iso === focoDia;
                const classes = ["cal-dia", !c.doMes && "cal-fora", c.iso === hoje && "cal-hoje"].filter(Boolean).join(" ");
                return (
                  <button
                    key={c.iso}
                    type="button"
                    role="gridcell"
                    className={classes}
                    tabIndex={comFoco ? 0 : -1}
                    data-foco={comFoco ? "true" : undefined}
                    aria-label={rotuloDoDia(c.iso)}
                    aria-selected={c.iso === selecionadoDia}
                    aria-current={c.iso === hoje ? "date" : undefined}
                    aria-disabled={bloqueado || undefined}
                    onClick={() => escolherDia(c.iso)}
                  >
                    {c.dia}
                  </button>
                );
              })}
            </div>
          ))}
        </div>
      )}

      {visao === "meses" && (
        <div
          role="group"
          aria-label={`Meses de ${ano}`}
          className="cal-grade3"
          onKeyDown={(e) => teclasGrade3(e, focoMes, 1, 12, setFocoMes)}
        >
          {MESES_CURTOS.map((nome, i) => {
            const numero = i + 1;
            const escolhido = modo === "mes" ? selecionadoMes?.ano === ano && selecionadoMes.mes === numero : !!lerIso(valor) && lerIso(valor)?.ano === ano && lerIso(valor)?.mes === numero;
            const ehHoje = parteHoje?.ano === ano && parteHoje.mes === numero;
            const comFoco = numero === focoMes;
            return (
              <button
                key={nome}
                type="button"
                className={["cal-item", ehHoje && "cal-hoje"].filter(Boolean).join(" ")}
                tabIndex={comFoco ? 0 : -1}
                data-foco={comFoco ? "true" : undefined}
                aria-label={`${MESES[i]} de ${ano}`}
                aria-pressed={escolhido}
                aria-current={ehHoje ? "date" : undefined}
                onClick={() => escolherMes(numero)}
              >
                {capitalizar(nome)}
              </button>
            );
          })}
        </div>
      )}

      {visao === "anos" && (
        <div
          role="group"
          aria-label="Anos"
          className="cal-grade3"
          onKeyDown={(e) =>
            teclasGrade3(e, focoAnoEfetivo, anosPagina[0], anosPagina[anosPagina.length - 1], setFocoAno, (d) => {
              irParaPagina(d);
              setFocoAno(limitar(focoAnoEfetivo + 12 * d, ANO_MIN, ANO_MAX));
            })
          }
        >
          {anosPagina.map((a) => {
            const escolhido = modo === "mes" ? selecionadoMes?.ano === a : lerIso(valor)?.ano === a;
            const ehHoje = parteHoje?.ano === a;
            const comFoco = a === focoAnoEfetivo;
            return (
              <button
                key={a}
                type="button"
                className={["cal-item", ehHoje && "cal-hoje"].filter(Boolean).join(" ")}
                tabIndex={comFoco ? 0 : -1}
                data-foco={comFoco ? "true" : undefined}
                aria-pressed={escolhido}
                aria-current={ehHoje ? "date" : undefined}
                onClick={() => escolherAno(a)}
              >
                {a}
              </button>
            );
          })}
        </div>
      )}

      <div className="cal-rodape">
        <button type="button" className="btn btn-ghost btn-sm" disabled={hojeBloqueado} onClick={escolherHoje}>
          {modo === "mes" ? "Este mês" : "Hoje"}
        </button>
        <button type="button" className="btn btn-ghost btn-sm" onClick={limpar}>
          Limpar
        </button>
      </div>
    </div>
  );

  return createPortal(celular ? <div className="modal-overlay cal-overlay">{painel}</div> : painel, document.body);
}
