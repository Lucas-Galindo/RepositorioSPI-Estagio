"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import type { TurmaResumo } from "@/lib/api/alunos";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { DateInput } from "@/components/shared/DateInput";
import { sugerirParcelas } from "@/lib/parcelas";
import { currency } from "@/lib/format";
import {
  atualizarVinculoCobranca,
  cadastrarVinculoCobranca,
  ehPlano,
  type ModalidadeCobranca,
  type VinculoCobranca,
  type VinculoCobrancaRequest,
} from "@/lib/api/vinculosCobranca";

interface VinculoCobrancaFormModalProps {
  alunoId: number;
  /** Turmas ativas em que o aluno participa (a validacao real e do backend). */
  turmasAtivas: TurmaResumo[];
  /** Quando informado, o modal edita este vinculo em vez de cadastrar um novo. */
  vinculo?: VinculoCobranca;
  onSalvo: () => void;
  onCancelar: () => void;
}

const MODALIDADES: ModalidadeCobranca[] = ["Avulsa", "Mensalidade", "Pacote"];
// specs/062: os planos so existem no cadastro (um plano salvo nao e editado).
const MODALIDADES_NO_CADASTRO: ModalidadeCobranca[] = [...MODALIDADES, "Semestral", "Anual"];
const ROTULO_DA_MODALIDADE: Partial<Record<ModalidadeCobranca, string>> = { Semestral: "Semestral (6 meses)", Anual: "Anual (12 meses)" };

interface ParcelaDoFormulario {
  vencimento: string;
  valor: string;
}

export function VinculoCobrancaFormModal({ alunoId, turmasAtivas, vinculo, onSalvo, onCancelar }: VinculoCobrancaFormModalProps) {
  const editando = Boolean(vinculo);
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();

  // "" = Atendimento individual (mesmo padrao do campo Turma opcional de Aula).
  const [turmaId, setTurmaId] = useState(vinculo?.turmaId?.toString() ?? "");
  const [modalidade, setModalidade] = useState<ModalidadeCobranca>(vinculo?.modalidade ?? "Avulsa");
  const [valor, setValor] = useState(vinculo?.valor.toString() ?? "");
  const [aulasIncluidas, setAulasIncluidas] = useState(vinculo?.aulasIncluidas?.toString() ?? "");
  const [saldoAulas, setSaldoAulas] = useState(vinculo?.saldoAulas?.toString() ?? "");
  const [totalAulasPacote, setTotalAulasPacote] = useState(vinculo?.totalAulasPacote?.toString() ?? "");
  // specs/062: plano Semestral/Anual -- inicio, numero de parcelas, 1o vencimento e as parcelas (editaveis).
  const [dataInicio, setDataInicio] = useState("");
  const [quantidadeDeParcelas, setQuantidadeDeParcelas] = useState("1");
  const [primeiroVencimento, setPrimeiroVencimento] = useState("");
  const [parcelas, setParcelas] = useState<ParcelaDoFormulario[]>([]);

  // Se a turma atual do vinculo nao esta entre as ativas do aluno (foi desativada
  // depois), mantem-na como opcao para nao forcar a troca ao editar so o valor.
  const turmaAtualForaDasAtivas =
    vinculo?.turmaId != null && !turmasAtivas.some((t) => t.id === vinculo.turmaId)
      ? { id: vinculo.turmaId, nome: vinculo.turmaNome ?? `Turma ${vinculo.turmaId}` }
      : null;
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  // specs/063: turmas que podem receber um pacote novo. E so um filtro de exibicao -- o servidor
  // decide (turma de pacote, ativa e ainda nao iniciada) e devolve o erro quando nao serve.
  const [turmasDePacote, setTurmasDePacote] = useState<Turma[]>([]);
  useEffect(() => {
    if (!sessao || editando || modalidade !== "Pacote") return;
    listarTurmas(sessao.accessToken, { ativo: true })
      .then((lista) => setTurmasDePacote(lista.filter((t) => t.tipo === "Pacote" && !t.fechada)))
      .catch(() => setTurmasDePacote([]));
  }, [sessao, editando, modalidade]);

  // specs/062: sugere parcelas mensais iguais quando o total, o numero de parcelas ou o 1o vencimento mudam (chamado
  // pelos proprios campos). E so preenchimento: a professora ajusta data e valor de cada parcela, e o servidor
  // revalida tudo.
  const regerarParcelas = (total: string, quantidade: string, primeiro: string) =>
    setParcelas(sugerirParcelas(Number(total), Number(quantidade), primeiro).map((p) => ({ vencimento: p.vencimento, valor: p.valor.toFixed(2) })));

  // Conveniencia de UI: ao trocar a modalidade, limpa o campo que deixou de se
  // aplicar. A regra de negocio (rejeitar campo nao aplicavel) e do backend.
  // specs/063: a Mensalidade nao tem turma; a Avulsa usa as turmas do aluno e o Pacote usa as
  // turmas de pacote -- listas diferentes, entao trocar entre elas limpa a turma.
  const handleModalidade = (nova: ModalidadeCobranca) => {
    setModalidade(nova);
    setTurmaId("");
    if (ehPlano(nova)) regerarParcelas(valor, quantidadeDeParcelas, primeiroVencimento);
    else setParcelas([]);
    if (nova !== "Mensalidade") setAulasIncluidas("");
    if (nova !== "Pacote") {
      setSaldoAulas("");
      setTotalAulasPacote("");
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    const request: VinculoCobrancaRequest = {
      turmaId: turmaId ? Number(turmaId) : null,
      modalidade,
      valor: Number(valor) || 0,
      aulasIncluidas: aulasIncluidas ? Number(aulasIncluidas) : null,
      saldoAulas: saldoAulas ? Number(saldoAulas) : null,
      totalAulasPacote: totalAulasPacote ? Number(totalAulasPacote) : null,
      ...(ehPlano(modalidade)
        ? { dataInicio: dataInicio || null, parcelas: parcelas.map((p) => ({ vencimento: p.vencimento, valor: Number(p.valor) || 0 })) }
        : {}),
    };

    try {
      if (vinculo) {
        await atualizarVinculoCobranca(alunoId, vinculo.id, request, sessao.accessToken);
        mostrarToast("Vínculo de cobrança atualizado com sucesso.");
      } else {
        await cadastrarVinculoCobranca(alunoId, request, sessao.accessToken);
        mostrarToast("Vínculo de cobrança cadastrado com sucesso.");
      }
      onSalvo();
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar o vínculo de cobrança."]);
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onCancelar}>
      <div className="modal modal-vinculo" onClick={(e) => e.stopPropagation()}>
        <h3>{editando ? "Editar vínculo de cobrança" : "Novo vínculo de cobrança"}</h3>
        <div className="form-wrap">
          <form onSubmit={handleSubmit}>
            {erros.length > 0 && (
              <div className="err-banner show" style={{ marginBottom: 18 }}>
                <Icon name="warn" size={15} />
                <span>{erros[0]}</span>
              </div>
            )}

            <div className="field-grid two">
              <div className="field">
                <label>
                  Modalidade<span className="req">*</span>
                </label>
                <select required value={modalidade} onChange={(e) => handleModalidade(e.target.value as ModalidadeCobranca)}>
                  {(editando ? MODALIDADES : MODALIDADES_NO_CADASTRO).map((m) => (
                    <option key={m} value={m}>
                      {ROTULO_DA_MODALIDADE[m] ?? m}
                    </option>
                  ))}
                </select>
              </div>
              {modalidade === "Avulsa" ? (
                <div className="field">
                  <label>Turma</label>
                  <select value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
                    <option value="">Atendimento individual</option>
                    {turmaAtualForaDasAtivas && <option value={turmaAtualForaDasAtivas.id}>{turmaAtualForaDasAtivas.nome}</option>}
                    {turmasAtivas.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.nome}
                      </option>
                    ))}
                  </select>
                </div>
              ) : modalidade === "Pacote" ? (
                // specs/063: todo pacote pertence a um grupo -- uma turma de pacote ou as aulas
                // individuais. A turma de um pacote nao muda depois de salva.
                <div className="field">
                  <label>Turma do pacote</label>
                  {editando ? (
                    <input disabled value={vinculo?.turmaNome ?? "Atendimento individual"} />
                  ) : (
                    <select value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
                      <option value="">Atendimento individual</option>
                      {turmasDePacote.map((t) => (
                        <option key={t.id} value={t.id}>
                          {t.nome}
                        </option>
                      ))}
                    </select>
                  )}
                  <span className="hint">
                    {editando
                      ? "A turma de um pacote não muda. Para trocar, exclua o pacote e cadastre outro."
                      : "O pacote só vale nas aulas deste grupo. Na turma de pacote o cadastro matricula o aluno."}
                  </span>
                </div>
              ) : (
                <div className="field">
                  <label>Vale para</label>
                  <input disabled value="Todas as aulas do aluno" />
                </div>
              )}
              <div className="field">
                <label>
                  {ehPlano(modalidade) ? "Valor total do período" : "Valor"}
                  <span className="req">*</span>
                </label>
                <input
                  required
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={valor}
                  onChange={(e) => {
                    setValor(e.target.value);
                    if (ehPlano(modalidade)) regerarParcelas(e.target.value, quantidadeDeParcelas, primeiroVencimento);
                  }}
                  placeholder="80.00"
                />
              </div>
              {/* specs/062: plano Semestral/Anual. O desconto e a taxa da maquininha ja vem dentro do valor das parcelas:
                  nao ha campo para eles, e o total e so a referencia do contrato (pode diferir da soma das parcelas). */}
              {ehPlano(modalidade) && (
                <>
                  <div className="field">
                    <label>
                      Data de início<span className="req">*</span>
                    </label>
                    <DateInput
                      required
                      value={dataInicio}
                      onChange={(novaData) => {
                        setDataInicio(novaData);
                        if (!primeiroVencimento) {
                          setPrimeiroVencimento(novaData);
                          regerarParcelas(valor, quantidadeDeParcelas, novaData);
                        }
                      }}
                    />
                    <span className="hint">O plano vale para as aulas do período: {modalidade === "Semestral" ? "6" : "12"} meses a partir desta data.</span>
                  </div>
                  <div className="field">
                    <label>
                      Número de parcelas<span className="req">*</span>
                    </label>
                    <input
                      required
                      type="number"
                      step="1"
                      min="1"
                      max="24"
                      value={quantidadeDeParcelas}
                      onChange={(e) => {
                        setQuantidadeDeParcelas(e.target.value);
                        regerarParcelas(valor, e.target.value, primeiroVencimento);
                      }}
                    />
                    <span className="hint">1 = à vista. Até 24.</span>
                  </div>
                  <div className="field">
                    <label>
                      Vencimento da 1ª parcela<span className="req">*</span>
                    </label>
                    <DateInput
                      required
                      value={primeiroVencimento}
                      onChange={(novaData) => {
                        setPrimeiroVencimento(novaData);
                        regerarParcelas(valor, quantidadeDeParcelas, novaData);
                      }}
                    />
                  </div>
                </>
              )}
              {modalidade === "Mensalidade" && (
                <div className="field">
                  <label>Aulas incluídas (opcional)</label>
                  <input type="number" step="1" min="1" value={aulasIncluidas} onChange={(e) => setAulasIncluidas(e.target.value)} placeholder="8" />
                </div>
              )}
              {modalidade === "Pacote" && (
                <div className="field">
                  <label>Total de aulas do pacote</label>
                  <input type="number" step="1" min="1" value={totalAulasPacote} onChange={(e) => setTotalAulasPacote(e.target.value)} placeholder="10" />
                </div>
              )}
              {/* specs/058: no cadastro o saldo comeca igual ao total (backend). Na
                  edicao o saldo e o ajuste manual que libera/bloqueia a presenca (spec 041). */}
              {modalidade === "Pacote" && editando && (
                <div className="field">
                  <label>Saldo de aulas</label>
                  <input type="number" step="1" min="0" value={saldoAulas} onChange={(e) => setSaldoAulas(e.target.value)} placeholder="10" />
                </div>
              )}
            </div>

            {/* specs/062: parcelas sugeridas (mensais e iguais), editaveis uma a uma. A diferenca para o total e so um aviso. */}
            {ehPlano(modalidade) && parcelas.length > 0 && (
              <div className="field" style={{ marginTop: 6 }}>
                <label>Parcelas (ajuste a data e o valor de cada uma, se precisar)</label>
                {parcelas.map((p, i) => (
                  <div key={i} style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: 6, flexWrap: "wrap" }}>
                    <span className="hint" style={{ width: 48 }}>
                      {i + 1}/{parcelas.length}
                    </span>
                    <DateInput
                      required
                      ariaLabel={`Vencimento da parcela ${i + 1}`}
                      value={p.vencimento}
                      onChange={(novaData) => setParcelas((lista) => lista.map((x, j) => (j === i ? { ...x, vencimento: novaData } : x)))}
                      style={{ flex: 1, minWidth: 140 }}
                    />
                    <input
                      type="number"
                      required
                      step="0.01"
                      min="0.01"
                      aria-label={`Valor da parcela ${i + 1}`}
                      value={p.valor}
                      onChange={(e) => setParcelas((lista) => lista.map((x, j) => (j === i ? { ...x, valor: e.target.value } : x)))}
                      style={{ flex: 1, minWidth: 110 }}
                    />
                  </div>
                ))}
                {Math.abs(parcelas.reduce((soma, p) => soma + (Number(p.valor) || 0), 0) - (Number(valor) || 0)) >= 0.005 && (
                  <span className="hint">
                    A soma das parcelas ({currency(parcelas.reduce((soma, p) => soma + (Number(p.valor) || 0), 0))}) é diferente do valor total (
                    {currency(Number(valor) || 0)}). Você pode salvar assim.
                  </span>
                )}
              </div>
            )}

            <div className="form-actions">
              <button type="button" className="btn btn-ghost" onClick={onCancelar}>
                Cancelar
              </button>
              <button type="submit" className="btn btn-primary" disabled={salvando}>
                {salvando ? "Salvando..." : editando ? "Salvar alterações" : "Cadastrar vínculo"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
