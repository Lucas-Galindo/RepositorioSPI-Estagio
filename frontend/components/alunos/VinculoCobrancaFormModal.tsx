"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import type { TurmaResumo } from "@/lib/api/alunos";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import {
  atualizarVinculoCobranca,
  cadastrarVinculoCobranca,
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

  // Conveniencia de UI: ao trocar a modalidade, limpa o campo que deixou de se
  // aplicar. A regra de negocio (rejeitar campo nao aplicavel) e do backend.
  // specs/063: a Mensalidade nao tem turma; a Avulsa usa as turmas do aluno e o Pacote usa as
  // turmas de pacote -- listas diferentes, entao trocar entre elas limpa a turma.
  const handleModalidade = (nova: ModalidadeCobranca) => {
    setModalidade(nova);
    setTurmaId("");
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
                  {MODALIDADES.map((m) => (
                    <option key={m} value={m}>
                      {m}
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
                  Valor<span className="req">*</span>
                </label>
                <input required type="number" step="0.01" min="0.01" value={valor} onChange={(e) => setValor(e.target.value)} placeholder="80.00" />
              </div>
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
