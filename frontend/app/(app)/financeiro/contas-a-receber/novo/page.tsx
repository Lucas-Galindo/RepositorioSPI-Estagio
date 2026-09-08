"use client";

import { useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { ApiError } from "@/lib/api/client";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import {
  listarFormasPagamento,
  listarCategoriasReceita,
  registrarPagamento,
  type FormaPagamento,
  type CategoriaReceita,
} from "@/lib/api/pagamentos";
import { fmtData, fmtHora } from "@/lib/format";

export default function NovaContaAReceberPage() {
  usePageHeader("Financeiro", "Nova conta a receber");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [alunos, setAlunos] = useState<Aluno[]>([]);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [categorias, setCategorias] = useState<CategoriaReceita[]>([]);
  const [aulasDoAluno, setAulasDoAluno] = useState<Aula[]>([]);

  const [alunoId, setAlunoId] = useState("");
  const [descricao, setDescricao] = useState("");
  const [categoriaReceitaId, setCategoriaReceitaId] = useState("");
  const [aulaIds, setAulaIds] = useState<number[]>([]);
  const [dataVencimento, setDataVencimento] = useState("");
  const [competencia, setCompetencia] = useState("");
  const [valorFinal, setValorFinal] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [observacoes, setObservacoes] = useState("");
  const [status, setStatus] = useState<"Pendente" | "Pago">("Pendente");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarAlunos(sessao.accessToken, { ativo: true }).then(setAlunos).catch(() => setAlunos([]));
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
    listarCategoriasReceita(sessao.accessToken).then(setCategorias).catch(() => setCategorias([]));
  }, [sessao]);

  useEffect(() => {
    if (!sessao || !alunoId) {
      return;
    }
    listarAulas(sessao.accessToken, { alunoId: Number(alunoId), status: "Realizada" })
      .then(setAulasDoAluno)
      .catch(() => setAulasDoAluno([]));

    return () => setAulasDoAluno([]);
  }, [sessao, alunoId]);

  const toggleAula = (id: number) => {
    setAulaIds((atual) => (atual.includes(id) ? atual.filter((a) => a !== id) : [...atual, id]));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    try {
      const criado = await registrarPagamento(
        {
          alunoId: Number(alunoId),
          descricao: descricao || null,
          categoriaReceitaId: categoriaReceitaId ? Number(categoriaReceitaId) : null,
          aulaIds,
          dataVencimento,
          competencia: competencia ? `${competencia}-01` : null,
          valorFinal: Number(valorFinal) || 0,
          formaPagamentoId: Number(formaPagamentoId),
          observacoes: observacoes || null,
          status,
        },
        sessao.accessToken
      );
      mostrarToast("Conta a receber registrada com sucesso.");
      router.push(`/financeiro/contas-a-receber/${criado.id}`);
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível registrar a conta a receber."]);
    } finally {
      setSalvando(false);
    }
  };

  return (
    <>
      <Link href="/financeiro/contas-a-receber" className="breadcrumb">
        <Icon name="back" size={13} /> Contas a Receber
      </Link>

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
              <span className="fs-num">1</span> Aluno e aulas cobertas
            </div>
            <div className="field-grid two">
              <div className="field">
                <label>
                  Aluno<span className="req">*</span>
                </label>
                <select required value={alunoId} onChange={(e) => setAlunoId(e.target.value)}>
                  <option value="">Selecione...</option>
                  {alunos.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.nome} (RA {a.ra})
                    </option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label>Categoria (opcional)</label>
                <select value={categoriaReceitaId} onChange={(e) => setCategoriaReceitaId(e.target.value)}>
                  <option value="">Selecione...</option>
                  {categorias.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.nome}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="field" style={{ marginTop: 15 }}>
              <label>Descrição (opcional)</label>
              <input value={descricao} onChange={(e) => setDescricao(e.target.value)} placeholder="Ex: Aulas de setembro" />
            </div>

            {alunoId && (
              <div style={{ marginTop: 15 }}>
                <label style={{ fontSize: 11.5, fontWeight: 600, display: "block", marginBottom: 8 }}>
                  Aulas realizadas cobertas por esta conta (opcional)
                </label>
                {aulasDoAluno.length ? (
                  <div className="checkbox-list">
                    {aulasDoAluno.map((a) => (
                      <label className="chk-pill" key={a.id}>
                        <input type="checkbox" checked={aulaIds.includes(a.id)} onChange={() => toggleAula(a.id)} />
                        {fmtData(a.dataInicio)} · {fmtHora(a.horaInicio)} · {a.materiaNome}
                      </label>
                    ))}
                  </div>
                ) : (
                  <span className="hint">Nenhuma aula realizada encontrada para este aluno.</span>
                )}
              </div>
            )}
          </div>

          <div className="form-section">
            <div className="fs-title">
              <span className="fs-num">2</span> Valores
            </div>
            <div className="field-grid">
              <div className="field">
                <label>
                  Valor<span className="req">*</span>
                </label>
                <input required type="number" step="0.01" min="0" value={valorFinal} onChange={(e) => setValorFinal(e.target.value)} placeholder="160.00" />
              </div>
              <div className="field">
                <label>
                  Data de vencimento<span className="req">*</span>
                </label>
                <input required type="date" value={dataVencimento} onChange={(e) => setDataVencimento(e.target.value)} />
              </div>
              <div className="field">
                <label>Competência (opcional)</label>
                <input type="month" value={competencia} onChange={(e) => setCompetencia(e.target.value)} />
              </div>
            </div>
            <div className="field-grid two" style={{ marginTop: 15 }}>
              <div className="field">
                <label>
                  Forma de pagamento<span className="req">*</span>
                </label>
                <select required value={formaPagamentoId} onChange={(e) => setFormaPagamentoId(e.target.value)}>
                  <option value="">Selecione...</option>
                  {formas.map((f) => (
                    <option key={f.id} value={f.id}>
                      {f.forma}
                    </option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label>Status</label>
                <select value={status} onChange={(e) => setStatus(e.target.value as "Pendente" | "Pago")}>
                  <option value="Pendente">Pendente</option>
                  <option value="Pago">Pago (recebido agora)</option>
                </select>
              </div>
            </div>
            <div className="field" style={{ marginTop: 15 }}>
              <label>Observações (opcional)</label>
              <textarea rows={3} value={observacoes} onChange={(e) => setObservacoes(e.target.value)} />
            </div>
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-ghost" onClick={() => router.push("/financeiro/contas-a-receber")}>
              Cancelar
            </button>
            <button type="submit" className="btn btn-primary" disabled={salvando}>
              {salvando ? "Salvando..." : "Registrar conta a receber"}
            </button>
          </div>
        </form>
      </div>
    </>
  );
}
