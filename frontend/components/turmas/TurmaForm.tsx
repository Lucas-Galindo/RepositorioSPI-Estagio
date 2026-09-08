"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { cadastrarTurma, atualizarTurma, type Turma, type TurmaRequest } from "@/lib/api/turmas";

export function TurmaForm({ turma }: { turma?: Turma }) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();
  const editando = Boolean(turma);

  const [nome, setNome] = useState(turma?.nome ?? "");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);
    const request: TurmaRequest = { nome };

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
          </div>
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
