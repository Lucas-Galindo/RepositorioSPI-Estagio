"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { cadastrarMateria, atualizarMateria, type Materia, type MateriaRequest } from "@/lib/api/materias";

export function MateriaForm({ materia }: { materia?: Materia }) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();
  const editando = Boolean(materia);

  const [nome, setNome] = useState(materia?.nome ?? "");
  const [nivel, setNivel] = useState(materia?.nivel ?? "");
  const [descricao, setDescricao] = useState(materia?.descricao ?? "");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);
    const request: MateriaRequest = { nome, nivel: nivel || null, descricao: descricao || null };

    try {
      const resultado = editando && materia
        ? await atualizarMateria(materia.id, request, sessao.accessToken)
        : await cadastrarMateria(request, sessao.accessToken);
      mostrarToast(editando ? "Matéria atualizada com sucesso." : "Matéria cadastrada com sucesso.");
      router.push(`/materias/${resultado.id}`);
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar a matéria."]);
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
            <span className="fs-num">1</span> Dados da matéria
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>
                Nome<span className="req">*</span>
              </label>
              <input required value={nome} onChange={(e) => setNome(e.target.value)} placeholder="Matemática" />
            </div>
            <div className="field">
              <label>Nível de ensino</label>
              <input value={nivel} onChange={(e) => setNivel(e.target.value)} placeholder="Fundamental II" />
            </div>
          </div>
          <div className="field" style={{ marginTop: 15 }}>
            <label>Descrição</label>
            <textarea rows={3} value={descricao} onChange={(e) => setDescricao(e.target.value)} />
          </div>
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={() => router.push(editando && materia ? `/materias/${materia.id}` : "/materias")}
          >
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : editando ? "Salvar alterações" : "Cadastrar matéria"}
          </button>
        </div>
      </form>
    </div>
  );
}
