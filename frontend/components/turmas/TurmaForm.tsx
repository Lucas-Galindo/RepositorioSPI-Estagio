"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { cadastrarTurma, atualizarTurma, type Turma, type TurmaRequest, type TipoTurma } from "@/lib/api/turmas";

export function TurmaForm({ turma }: { turma?: Turma }) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();
  const editando = Boolean(turma);

  const [nome, setNome] = useState(turma?.nome ?? "");
  // specs/063: o tipo so se escolhe na criacao; depois e somente leitura (a turma "nao muda").
  const [tipo, setTipo] = useState<TipoTurma>(turma?.tipo ?? "Mensalidade");
  const [dataInicio, setDataInicio] = useState(turma?.dataInicio ?? "");
  const [dataFim, setDataFim] = useState(turma?.dataFim ?? "");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  const ehPacote = tipo === "Pacote";
  // A data de inicio nao muda depois de preenchida (o servidor recusa); aqui so a travamos na tela.
  const inicioTravado = editando && Boolean(turma?.dataInicio);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);
    const request: TurmaRequest = {
      nome,
      tipo,
      dataInicio: ehPacote && dataInicio ? dataInicio : null,
      dataFim: ehPacote && dataFim ? dataFim : null,
    };

    try {
      const resultado = editando && turma
        ? await atualizarTurma(turma.id, request, sessao.accessToken)
        : await cadastrarTurma(request, sessao.accessToken);
      mostrarToast(editando ? "Turma atualizada com sucesso." : "Turma cadastrada com sucesso.");
      router.push(`/turmas/${resultado.id}`);
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar a turma."]);
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className="form-wrap">
      <form onSubmit={handleSubmit}>
        {erros.length > 0 && (
          <div className="err-banner show" style={{ marginBottom: 20 }}>
            <Icon name="warn" size={15} />
            <span>{erros[0]}</span>
          </div>
        )}

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">1</span> Dados da turma
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>
                Nome da turma<span className="req">*</span>
              </label>
              <input required value={nome} onChange={(e) => setNome(e.target.value)} placeholder="8º Ano A" />
            </div>
            <div className="field">
              <label>Tipo da turma</label>
              <select value={tipo} disabled={editando} onChange={(e) => setTipo(e.target.value as TipoTurma)}>
                <option value="Mensalidade">Mensalidade</option>
                <option value="Pacote">Pacote</option>
              </select>
              <span className="hint">
                {editando
                  ? "O tipo não muda depois que a turma é criada."
                  : "Turma de pacote é fechada: começa e termina junto, e depois que começa ninguém novo entra."}
              </span>
            </div>
          </div>

          {ehPacote && (
            <div className="field-grid two" style={{ marginTop: 14 }}>
              <div className="field">
                <label>
                  Início da turma{!editando && <span className="req">*</span>}
                </label>
                <input
                  type="date"
                  required={!editando}
                  disabled={inicioTravado}
                  value={dataInicio}
                  onChange={(e) => setDataInicio(e.target.value)}
                />
                <span className="hint">
                  {inicioTravado
                    ? "A data de início não muda depois de preenchida."
                    : "Depois desta data a turma fica fechada para novos alunos."}
                </span>
              </div>
              <div className="field">
                <label>
                  Fim da turma{!editando && <span className="req">*</span>}
                </label>
                <input
                  type="date"
                  required={!editando}
                  min={dataInicio || undefined}
                  value={dataFim}
                  onChange={(e) => setDataFim(e.target.value)}
                />
                <span className="hint">Quando a turma acaba, os pacotes dela ficam inativos (o saldo é preservado).</span>
              </div>
            </div>
          )}
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={() => router.push(editando && turma ? `/turmas/${turma.id}` : "/turmas")}
          >
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : editando ? "Salvar alterações" : "Cadastrar turma"}
          </button>
        </div>
      </form>
    </div>
  );
}
