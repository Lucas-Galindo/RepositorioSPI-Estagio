"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { listarMaterias, type Materia } from "@/lib/api/materias";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { cadastrarAula, atualizarAula, type Aula, type AulaRequest } from "@/lib/api/aulas";

export function AulaForm({ aula }: { aula?: Aula }) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();
  const searchParams = useSearchParams();
  const editando = Boolean(aula);
  // Vinda da Agenda (clique num dia do calendario): pre-preenche a data,
  // so ao criar -- editando sempre usa a data ja salva na aula.
  const dataPreSelecionada = searchParams.get("data");

  const [materias, setMaterias] = useState<Materia[]>([]);
  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [alunos, setAlunos] = useState<Aluno[]>([]);

  const [descricao, setDescricao] = useState(aula?.descricao ?? "");
  const [materiaId, setMateriaId] = useState(aula?.materiaId.toString() ?? "");
  const [turmaId, setTurmaId] = useState(aula?.turmaId?.toString() ?? "");
  const [alunoId, setAlunoId] = useState(aula?.alunos[0]?.alunoId.toString() ?? "");
  const [dataInicio, setDataInicio] = useState(aula?.dataInicio ?? dataPreSelecionada ?? "");
  const [horaInicio, setHoraInicio] = useState(aula?.horaInicio.slice(0, 5) ?? "");
  const [horaFim, setHoraFim] = useState(aula?.horaFim.slice(0, 5) ?? "");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarMaterias(sessao.accessToken, { ativo: true }).then(setMaterias).catch(() => setMaterias([]));
    listarTurmas(sessao.accessToken, { ativo: true }).then(setTurmas).catch(() => setTurmas([]));
    listarAlunos(sessao.accessToken, { ativo: true }).then(setAlunos).catch(() => setAlunos([]));
  }, [sessao]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    const request: AulaRequest = {
      descricao: descricao || null,
      materiaId: Number(materiaId),
      turmaId: turmaId ? Number(turmaId) : null,
      alunoId: turmaId ? null : Number(alunoId),
      dataInicio,
      horaInicio: `${horaInicio}:00`,
      horaFim: `${horaFim}:00`,
    };

    try {
      const resultado = editando && aula
        ? await atualizarAula(aula.id, request, sessao.accessToken)
        : await cadastrarAula(request, sessao.accessToken);
      mostrarToast(editando ? "Aula atualizada com sucesso." : "Aula agendada com sucesso.");
      router.push(`/aulas/${resultado.id}`);
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar a aula."]);
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
            <span className="fs-num">1</span> O que e quando
          </div>
          <div className="field-grid">
            <div className="field">
              <label>
                Matéria<span className="req">*</span>
              </label>
              <select required value={materiaId} onChange={(e) => setMateriaId(e.target.value)}>
                <option value="">Selecione...</option>
                {materias.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.nome}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label>Turma (opcional)</label>
              <select
                value={turmaId}
                onChange={(e) => {
                  setTurmaId(e.target.value);
                  if (e.target.value) setAlunoId("");
                }}
              >
                <option value="">Atendimento individual</option>
                {turmas.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.nome}
                  </option>
                ))}
              </select>
            </div>
            {!turmaId && (
              <div className="field">
                <label>
                  Aluno<span className="req">*</span>
                </label>
                <select required={!turmaId} value={alunoId} onChange={(e) => setAlunoId(e.target.value)}>
                  <option value="">Selecione...</option>
                  {alunos.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.nome} (RA {a.ra})
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>
          <div className="field" style={{ marginTop: 15 }}>
            <label>Descrição (opcional)</label>
            <input value={descricao} onChange={(e) => setDescricao(e.target.value)} />
          </div>
        </div>

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">2</span> Data e horário
          </div>
          <div className="field-grid">
            <div className="field">
              <label>
                Data<span className="req">*</span>
              </label>
              <input required type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
            </div>
            <div className="field">
              <label>
                Horário início<span className="req">*</span>
              </label>
              <input required type="time" value={horaInicio} onChange={(e) => setHoraInicio(e.target.value)} />
            </div>
            <div className="field">
              <label>
                Horário fim<span className="req">*</span>
              </label>
              <input required type="time" value={horaFim} onChange={(e) => setHoraFim(e.target.value)} />
            </div>
          </div>
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={() => router.push(editando && aula ? `/aulas/${aula.id}` : "/aulas")}
          >
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : editando ? "Salvar alterações" : "Agendar aula"}
          </button>
        </div>
      </form>
    </div>
  );
}
