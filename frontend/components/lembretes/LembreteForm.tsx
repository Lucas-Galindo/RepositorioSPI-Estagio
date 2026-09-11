"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { cadastrarLembrete, atualizarLembrete, type Lembrete, type LembreteRequest, type CanalLembrete, type DestinatariosLembrete } from "@/lib/api/lembretes";

export function LembreteForm({ lembrete }: { lembrete?: Lembrete }) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();
  const editando = Boolean(lembrete);

  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [turmaId, setTurmaId] = useState(lembrete?.turmaId.toString() ?? "");
  const [canal, setCanal] = useState<CanalLembrete>(lembrete?.canal ?? "Email");
  const [antecedenciaHora, setAntecedenciaHora] = useState(lembrete?.antecedenciaHora.toString() ?? "");
  const [destinatarios, setDestinatarios] = useState<DestinatariosLembrete>(lembrete?.destinatarios ?? "Alunos");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarTurmas(sessao.accessToken, { ativo: true }).then(setTurmas).catch(() => setTurmas([]));
  }, [sessao]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    const request: LembreteRequest = {
      turmaId: Number(turmaId),
      canal,
      antecedenciaHora: Number(antecedenciaHora),
      destinatarios,
    };

    try {
      if (editando && lembrete) {
        await atualizarLembrete(lembrete.id, request, sessao.accessToken);
        mostrarToast("Lembrete atualizado com sucesso.");
      } else {
        await cadastrarLembrete(request, sessao.accessToken);
        mostrarToast("Lembrete cadastrado com sucesso.");
      }
      router.push("/lembretes");
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar o lembrete."]);
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
            <span className="fs-num">1</span> Vínculo
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>
                Turma<span className="req">*</span>
              </label>
              <select required value={turmaId} onChange={(e) => setTurmaId(e.target.value)} disabled={editando}>
                <option value="">Selecione...</option>
                {turmas.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.nome}
                  </option>
                ))}
              </select>
              {editando && <span className="hint">A turma não pode ser alterada após o cadastro.</span>}
            </div>
            <div className="field">
              <label>
                Canal<span className="req">*</span>
              </label>
              <select required value={canal} onChange={(e) => setCanal(e.target.value as CanalLembrete)}>
                <option value="Email">E-mail</option>
              </select>
            </div>
          </div>
        </div>

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">2</span> Programação
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>
                Antecedência (horas)<span className="req">*</span>
              </label>
              <input
                required
                type="number"
                min={1}
                value={antecedenciaHora}
                onChange={(e) => setAntecedenciaHora(e.target.value)}
                placeholder="Ex.: 2"
              />
            </div>
            <div className="field">
              <label>Destinatários</label>
              <select value={destinatarios} onChange={(e) => setDestinatarios(e.target.value as DestinatariosLembrete)}>
                <option value="Alunos">Alunos</option>
                <option value="Responsaveis">Responsáveis</option>
                <option value="AlunosEResponsaveis">Alunos e responsáveis</option>
              </select>
            </div>
          </div>
          <span className="hint" style={{ display: "block", marginTop: 10 }}>
            A hora programada é calculada automaticamente a partir da próxima aula da turma, menos a antecedência.
            O canal de envio é sempre E-mail.
          </span>
        </div>

        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={() => router.push("/lembretes")}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : editando ? "Salvar alterações" : "Salvar lembrete"}
          </button>
        </div>
      </form>
    </div>
  );
}
