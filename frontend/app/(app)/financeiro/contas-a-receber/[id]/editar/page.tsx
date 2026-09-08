"use client";

import { use, useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { ApiError } from "@/lib/api/client";
import {
  obterPagamento,
  editarPagamento,
  listarFormasPagamento,
  listarCategoriasReceita,
  type Pagamento,
  type FormaPagamento,
  type CategoriaReceita,
} from "@/lib/api/pagamentos";

export default function EditarContaAReceberPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Financeiro", "Editar conta a receber");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [pagamento, setPagamento] = useState<Pagamento | null>(null);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [categorias, setCategorias] = useState<CategoriaReceita[]>([]);
  const [erroCarregamento, setErroCarregamento] = useState("");

  const [descricao, setDescricao] = useState("");
  const [categoriaReceitaId, setCategoriaReceitaId] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [dataVencimento, setDataVencimento] = useState("");
  const [competencia, setCompetencia] = useState("");
  const [valorFinal, setValorFinal] = useState("");
  const [observacoes, setObservacoes] = useState("");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
    listarCategoriasReceita(sessao.accessToken).then(setCategorias).catch(() => setCategorias([]));
    obterPagamento(Number(id), sessao.accessToken)
      .then((p) => {
        setPagamento(p);
        setDescricao(p.descricao ?? "");
        setCategoriaReceitaId(p.categoriaReceitaId ? String(p.categoriaReceitaId) : "");
        setFormaPagamentoId(p.formaPagamentoId ? String(p.formaPagamentoId) : "");
        setDataVencimento(p.dataVencimento);
        setCompetencia(p.competencia ? p.competencia.slice(0, 7) : "");
        setValorFinal(String(p.valorFinal));
        setObservacoes(p.observacoes ?? "");
      })
      .catch((excecao) => setErroCarregamento(excecao instanceof ApiError ? excecao.message : "Conta a receber não encontrada."));
  }, [sessao, id]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao || !pagamento) return;

    setErros([]);
    setSalvando(true);

    try {
      const atualizado = await editarPagamento(
        pagamento.id,
        {
          descricao: descricao || null,
          categoriaReceitaId: categoriaReceitaId ? Number(categoriaReceitaId) : null,
          formaPagamentoId: formaPagamentoId ? Number(formaPagamentoId) : null,
          dataVencimento: dataVencimento || null,
          competencia: competencia ? `${competencia}-01` : null,
          valorFinal: valorFinal ? Number(valorFinal) : null,
          observacoes: observacoes || null,
        },
        sessao.accessToken
      );
      mostrarToast("Conta a receber atualizada com sucesso.");
      router.push(`/financeiro/contas-a-receber/${atualizado.id}`);
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível atualizar a conta a receber."]);
    } finally {
      setSalvando(false);
    }
  };

  if (erroCarregamento) return <EmptyState title="Não foi possível carregar" desc={erroCarregamento} />;
  if (!pagamento) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <Link href={`/financeiro/contas-a-receber/${id}`} className="breadcrumb">
        <Icon name="back" size={13} /> Voltar
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
              <span className="fs-num">1</span> Dados da conta
            </div>
            <div className="field-grid two">
              <div className="field">
                <label>Descrição</label>
                <input value={descricao} onChange={(e) => setDescricao(e.target.value)} placeholder="Ex: Aulas de setembro" />
              </div>
              <div className="field">
                <label>Categoria</label>
                <select value={categoriaReceitaId} onChange={(e) => setCategoriaReceitaId(e.target.value)}>
                  <option value="">Não definida</option>
                  {categorias.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.nome}
                    </option>
                  ))}
                </select>
              </div>
            </div>
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
                <input required type="number" step="0.01" min="0" value={valorFinal} onChange={(e) => setValorFinal(e.target.value)} />
              </div>
              <div className="field">
                <label>
                  Data de vencimento<span className="req">*</span>
                </label>
                <input required type="date" value={dataVencimento} onChange={(e) => setDataVencimento(e.target.value)} />
              </div>
              <div className="field">
                <label>Competência</label>
                <input type="month" value={competencia} onChange={(e) => setCompetencia(e.target.value)} />
              </div>
            </div>
            <div className="field-grid two" style={{ marginTop: 15 }}>
              <div className="field">
                <label>Forma de pagamento</label>
                <select value={formaPagamentoId} onChange={(e) => setFormaPagamentoId(e.target.value)}>
                  <option value="">Não definida</option>
                  {formas.map((f) => (
                    <option key={f.id} value={f.id}>
                      {f.forma}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="field" style={{ marginTop: 15 }}>
              <label>Observações</label>
              <textarea rows={3} value={observacoes} onChange={(e) => setObservacoes(e.target.value)} />
            </div>
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-ghost" onClick={() => router.push(`/financeiro/contas-a-receber/${id}`)}>
              Cancelar
            </button>
            <button type="submit" className="btn btn-primary" disabled={salvando}>
              {salvando ? "Salvando..." : "Salvar alterações"}
            </button>
          </div>
        </form>
      </div>
    </>
  );
}
