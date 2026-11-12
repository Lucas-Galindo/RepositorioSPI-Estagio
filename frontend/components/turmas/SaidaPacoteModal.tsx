"use client";

import { useEffect, useRef, useState } from "react";
import { Icon } from "@/components/shared/Icon";
import { DateInput } from "@/components/shared/DateInput";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { previaSaidaPacote, registrarSaidaPacote, type AcertoSaidaPacote, type AlunoResumo } from "@/lib/api/turmas";
import { currency, dataIsoLocal } from "@/lib/format";

interface SaidaPacoteModalProps {
  turmaId: number;
  aluno: AlunoResumo;
  onCancelar: () => void;
  /** Depois da saida registrada (a tela recarrega a turma). */
  onRegistrada: () => void;
}

// A mensagem que o servidor devolve (primeira da lista de validacao ou o texto do erro).
const mensagemDe = (excecao: unknown, padrao: string) => (excecao instanceof ApiError ? (excecao.details ?? [excecao.message])[0] : padrao);

/**
 * specs/078: "Registrar saida" de um aluno de turma de pacote. Mostra o resumo do acerto -- TODO numero vem do servidor
 * (Principio II): aqui so se converte o texto digitado em numero, se envia e se exibe. Nada e gravado antes de
 * "Confirmar saida".
 */
export function SaidaPacoteModal({ turmaId, aluno, onCancelar, onRegistrada }: SaidaPacoteModalProps) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  const [valorPago, setValorPago] = useState("");
  const [vencimento, setVencimento] = useState(dataIsoLocal());
  const [acerto, setAcerto] = useState<AcertoSaidaPacote | null>(null);
  // Comeca carregando: a primeira consulta sai do efeito, sem setState sincrono nele.
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  // Numero do ultimo pedido de resumo: so a resposta do pedido mais novo vale -- nem a de um valor antigo, nem a que
  // chega depois de a professora mudar o valor ou fechar o modal.
  const pedido = useRef(0);
  const atraso = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Em branco = 0. So converte o texto; o servidor valida (negativo, casas decimais).
  const comoNumero = (texto: string) => (texto.trim() === "" ? 0 : Number(texto));

  // Consulta o resumo do valor digitado. Devolve a promessa para quem precisar esperar (ex.: depois de um 409).
  const consultar = (texto: string) => {
    if (!sessao) return Promise.resolve();
    const meu = ++pedido.current;
    return previaSaidaPacote(turmaId, aluno.id, { valorPago: comoNumero(texto) }, sessao.accessToken)
      .then((resposta) => {
        if (meu !== pedido.current) return;
        setAcerto(resposta);
        setErro(null);
      })
      .catch((excecao) => {
        if (meu !== pedido.current) return;
        setAcerto(null);
        setErro(mensagemDe(excecao, "Não foi possível calcular o acerto."));
      })
      .finally(() => {
        if (meu === pedido.current) setCarregando(false);
      });
  };

  // Uma consulta ao abrir (valor pago em branco); as seguintes saem do campo (mudarValor).
  useEffect(() => {
    consultar("");
    return () => {
      pedido.current += 1;
      if (atraso.current) clearTimeout(atraso.current);
    };
    // Abre uma vez por aluno/turma; "consultar" e recriada a cada render e so le o que esta nas dependencias.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessao, turmaId, aluno.id]);

  const mudarValor = (texto: string) => {
    setValorPago(texto);
    setCarregando(true);
    // Invalida na hora o pedido em andamento: sua resposta (de um valor antigo) nao pode reaparecer enquanto o novo espera.
    pedido.current += 1;
    if (atraso.current) clearTimeout(atraso.current);
    atraso.current = setTimeout(() => consultar(texto), 350);
  };

  const confirmar = async () => {
    if (!sessao || !acerto) return;
    setEnviando(true);
    setErro(null);
    try {
      // Os ecos sao os numeros que o servidor mostrou (consumo, devido e aulas); ele recalcula e recusa se mudaram.
      const resultado = await registrarSaidaPacote(
        turmaId,
        aluno.id,
        {
          valorPago: comoNumero(valorPago),
          vencimento: acerto.precisaVencimento ? vencimento : null,
          consumidasEsperadas: acerto.aulasConsumidas,
          valorDevidoEsperado: acerto.valorDevido,
          aulasQueSairaoEsperadas: acerto.aulasAgendadasQueSairao,
        },
        sessao.accessToken
      );
      mostrarToast(
        resultado.pagamentoId
          ? `Saída registrada. Conta de ${currency(resultado.acerto.diferenca)} gerada em Contas a receber.`
          : "Saída registrada."
      );
      onRegistrada();
    } catch (excecao) {
      const mensagem = mensagemDe(excecao, "Não foi possível registrar a saída.");
      // Resumo desatualizado (409): refaz o resumo e mantem a mensagem para a professora conferir.
      if (excecao instanceof ApiError && excecao.status === 409) await consultar(valorPago);
      setErro(mensagem);
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={() => !enviando && onCancelar()}>
      <div className="modal modal-saida" onClick={(e) => e.stopPropagation()}>
        <h3>Registrar saída de {aluno.nome}</h3>
        <p className="hint" style={{ marginBottom: 16 }}>
          O aluno paga só as aulas que consumiu. Confira o resumo; nada é gravado antes de confirmar.
        </p>

        {erro && (
          <div className="err-banner show" style={{ marginBottom: 16 }}>
            <Icon name="warn" size={15} />
            <span>{erro}</span>
          </div>
        )}

        <div className="field" style={{ marginBottom: 16 }}>
          <label>Quanto o aluno já pagou pelo pacote (R$)</label>
          <input
            type="number"
            step="0.01"
            min="0"
            inputMode="decimal"
            placeholder="0.00"
            value={valorPago}
            onChange={(e) => mudarValor(e.target.value)}
            style={{ maxWidth: 220 }}
          />
        </div>

        {acerto && (
          <div className="saida-resumo" aria-live="polite" style={{ opacity: carregando ? 0.55 : 1 }}>
            <dl>
              <div>
                <dt>Aulas consumidas</dt>
                <dd>
                  {acerto.aulasConsumidas} de {acerto.totalAulas}
                  {acerto.totalEstimado ? " (total estimado)" : ""}
                </dd>
              </div>
              <div>
                <dt>Valor do pacote</dt>
                <dd>{currency(acerto.valorPacote)}</dd>
              </div>
              <div>
                <dt>Valor por aula</dt>
                <dd>{currency(acerto.valorPorAula)}</dd>
              </div>
              <div>
                <dt>Valor devido pelo consumo</dt>
                <dd>
                  <strong>{currency(acerto.valorDevido)}</strong>
                </dd>
              </div>
              <div>
                <dt>Já pago (informado)</dt>
                <dd>{currency(acerto.valorPago)}</dd>
              </div>
            </dl>

            {acerto.consequencia === "GerarConta" && (
              <p className="saida-consequencia">
                Será gerada uma conta a receber de <strong>{currency(acerto.diferenca)}</strong>.
              </p>
            )}
            {acerto.consequencia === "Fechado" && <p className="saida-consequencia">Acerto fechado: nada a gerar.</p>}
            {acerto.consequencia === "ExcedenteNaoDevolvido" && (
              <p className="saida-consequencia">
                O valor pago a mais ({currency(acerto.excedente)}) não é devolvido. Nada será gerado.
              </p>
            )}

            {acerto.precisaVencimento && (
              <div className="field" style={{ marginTop: 12 }}>
                <label>
                  Vencimento da conta<span className="req">*</span>
                </label>
                <DateInput required min={dataIsoLocal()} value={vencimento} onChange={setVencimento} />
              </div>
            )}

            <ul className="saida-avisos">
              <li>O pacote será encerrado: as aulas não usadas ficam perdidas e o aluno não volta pelo pacote.</li>
              {acerto.aulasAgendadasQueSairao > 0 && (
                <li>
                  Sai de {acerto.aulasAgendadasQueSairao} {acerto.aulasAgendadasQueSairao === 1 ? "aula agendada" : "aulas agendadas"} que ainda
                  não {acerto.aulasAgendadasQueSairao === 1 ? "começou" : "começaram"}.
                </li>
              )}
            </ul>
          </div>
        )}

        {!acerto && carregando && <p className="count-text">Calculando o acerto...</p>}

        <div className="modal-actions" style={{ marginTop: 18 }}>
          <button className="btn btn-ghost btn-sm" type="button" onClick={onCancelar} disabled={enviando}>
            Fechar
          </button>
          <button
            className="btn btn-danger btn-sm"
            type="button"
            onClick={confirmar}
            disabled={enviando || carregando || !acerto || (acerto.precisaVencimento && !vencimento)}
          >
            {enviando ? "Registrando..." : "Confirmar saída"}
          </button>
        </div>
      </div>
    </div>
  );
}
