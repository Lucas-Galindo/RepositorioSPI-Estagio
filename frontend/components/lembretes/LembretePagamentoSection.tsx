"use client";

import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { TimeInput } from "@/components/shared/TimeInput";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import {
  atualizarRegraLembretePagamento,
  listarEnviosLembretePagamento,
  obterRegraLembretePagamento,
  type EnvioLembretePagamento,
} from "@/lib/api/lembretes";
import { fmtData, fmtDataHora } from "@/lib/format";

const DIAS = ["Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado"];

const RESULTADO: Record<EnvioLembretePagamento["resultado"], { texto: string; badge: string }> = {
  Enviado: { texto: "Enviado", badge: "Enviado" },
  Falha: { texto: "Falha", badge: "Falha" },
  SemDestinatario: { texto: "Sem e-mail cadastrado", badge: "Pendente" },
};

/**
 * specs/060: regra do e-mail semanal de pagamento pendente (editavel) e o
 * historico de envios. Quem decide quando e para quem enviar e o backend
 * (LembretePagamentoService); aqui so exibe e salva a regra.
 */
export function LembretePagamentoSection() {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const [ativo, setAtivo] = useState(true);
  const [diaSemana, setDiaSemana] = useState("1");
  const [hora, setHora] = useState("08:00");
  const [envios, setEnvios] = useState<EnvioLembretePagamento[] | null>(null);
  const [erro, setErro] = useState("");
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(() => {
    if (!sessao) return;
    obterRegraLembretePagamento(sessao.accessToken)
      .then((r) => {
        setAtivo(r.ativo);
        setDiaSemana(String(r.diaSemana));
        setHora(r.hora.slice(0, 5));
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar a regra."));
    listarEnviosLembretePagamento(sessao.accessToken)
      .then(setEnvios)
      .catch(() => setEnvios([]));
  }, [sessao]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const salvar = async () => {
    if (!sessao) return;
    setSalvando(true);
    try {
      await atualizarRegraLembretePagamento({ ativo, diaSemana: Number(diaSemana), hora: `${hora}:00` }, sessao.accessToken);
      mostrarToast("Regra do lembrete de pagamento salva.");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível salvar a regra.");
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className="mini-panel" style={{ marginTop: 24 }}>
      <h4>
        <Icon name="money" size={15} /> Pagamento pendente
      </h4>
      <p className="section-sub" style={{ marginBottom: 12 }}>
        O aluno e o responsável recebem um e-mail com os pagamentos vencidos sem baixa. Ele se repete toda semana, no dia e
        horário abaixo, até a situação ser atualizada. Você não recebe e-mail: vê o aviso no sino.
      </p>

      {erro ? (
        <EmptyState title="Não foi possível carregar" desc={erro} />
      ) : (
        <div className="filter-bar">
          <label className="chk-pill">
            <input type="checkbox" checked={ativo} onChange={(e) => setAtivo(e.target.checked)} />
            Envio ligado
          </label>
          <select className="filter-select" value={diaSemana} onChange={(e) => setDiaSemana(e.target.value)} disabled={!ativo} title="Dia do envio">
            {DIAS.map((d, i) => (
              <option key={d} value={i}>
                {d}
              </option>
            ))}
          </select>
          <TimeInput className="filter-input" value={hora} onChange={setHora} disabled={!ativo} title="Horário do envio" />
          <button className="btn btn-primary btn-sm" type="button" onClick={salvar} disabled={salvando}>
            {salvando ? "Salvando..." : "Salvar regra"}
          </button>
        </div>
      )}

      <div className="section-title" style={{ marginTop: 18 }}>
        Histórico de envios
      </div>
      {envios === null ? (
        <p className="count-text">Carregando...</p>
      ) : envios.length === 0 ? (
        <EmptyState title="Nenhum envio ainda" desc="Os e-mails de pagamento pendente aparecem aqui assim que forem enviados." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tentativa</th>
                <th>Semana de</th>
                <th>Aluno</th>
                <th>Destinatários</th>
                <th>Resultado</th>
              </tr>
            </thead>
            <tbody>
              {envios.map((e) => (
                <tr key={e.id}>
                  <td>{fmtDataHora(e.tentadoEm)}</td>
                  <td>{fmtData(e.dataEnvio)}</td>
                  <td>{e.alunoNome}</td>
                  <td>{e.destinatarios || "—"}</td>
                  <td title={e.detalhe ?? undefined}>
                    <StatusBadge status={RESULTADO[e.resultado].badge} /> {e.resultado !== "Enviado" && RESULTADO[e.resultado].texto}
                    {e.tentativas > 1 && ` (${e.tentativas} tentativas)`}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
