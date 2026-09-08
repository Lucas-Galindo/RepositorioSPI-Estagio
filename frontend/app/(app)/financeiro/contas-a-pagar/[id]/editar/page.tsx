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
  obterContaPagar,
  editarContaPagar,
  listarCategoriasDespesa,
  type ContaPagar,
  type CategoriaDespesa,
} from "@/lib/api/contasPagar";
import { listarFormasPagamento, type FormaPagamento } from "@/lib/api/pagamentos";

export default function EditarContaAPagarPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Financeiro", "Editar conta a pagar");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [conta, setConta] = useState<ContaPagar | null>(null);
  const [categorias, setCategorias] = useState<CategoriaDespesa[]>([]);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [erroCarregamento, setErroCarregamento] = useState("");

  const [descricao, setDescricao] = useState("");
  const [categoriaDespesaId, setCategoriaDespesaId] = useState("");
  const [favorecido, setFavorecido] = useState("");
  const [valor, setValor] = useState("");
  const [dataVencimento, setDataVencimento] = useState("");
  const [competencia, setCompetencia] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [observacoes, setObservacoes] = useState("");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarCategoriasDespesa(sessao.accessToken).then(setCategorias).catch(() => setCategorias([]));
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
    obterContaPagar(Number(id), sessao.accessToken)
      .then((c) => {
        setConta(c);
        setDescricao(c.descricao);
        setCategoriaDespesaId(String(c.categoriaDespesaId));
        setFavorecido(c.favorecido ?? "");
        setValor(String(c.valor));
        setDataVencimento(c.dataVencimento);
        setCompetencia(c.competencia ? c.competencia.slice(0, 7) : "");
        setFormaPagamentoId(c.formaPagamentoId ? String(c.formaPagamentoId) : "");
        setObservacoes(c.observacoes ?? "");
      })
      .catch((excecao) => setErroCarregamento(excecao instanceof ApiError ? excecao.message : "Conta a pagar não encontrada."));
  }, [sessao, id]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao || !conta) return;

    setErros([]);
    setSalvando(true);

    try {
      const atualizada = await editarContaPagar(
        conta.id,
        {
          descricao: descricao || null,
          categoriaDespesaId: categoriaDespesaId ? Number(categoriaDespesaId) : null,
          favorecido: favorecido || null,
          valor: valor ? Number(valor) : null,
          competencia: competencia ? `${competencia}-01` : null,
          dataVencimento: dataVencimento || null,
          formaPagamentoId: formaPagamentoId ? Number(formaPagamentoId) : null,
          observacoes: observacoes || null,
        },
        sessao.accessToken
      );
      mostrarToast("Conta a pagar atualizada com sucesso.");
      router.push(`/financeiro/contas-a-pagar/${atualizada.id}`);
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível atualizar a conta a pagar."]);
    } finally {
      setSalvando(false);
    }
  };

  if (erroCarregamento) return <EmptyState title="Não foi possível carregar" desc={erroCarregamento} />;
  if (!conta) return <p className="count-text">Carregando...</p>;

  return (
    <>
      <Link href={`/financeiro/contas-a-pagar/${id}`} className="breadcrumb">
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
              <span className="fs-num">1</span> Dados da despesa
            </div>
            <div className="field-grid two">
              <div className="field">
                <label>
                  Descrição<span className="req">*</span>
                </label>
                <input required value={descricao} onChange={(e) => setDescricao(e.target.value)} />
              </div>
              <div className="field">
                <label>Categoria</label>
                <select value={categoriaDespesaId} onChange={(e) => setCategoriaDespesaId(e.target.value)}>
                  {categorias.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.nome}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="field" style={{ marginTop: 15 }}>
              <label>Fornecedor/favorecido</label>
              <input value={favorecido} onChange={(e) => setFavorecido(e.target.value)} />
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
                <input required type="number" step="0.01" min="0" value={valor} onChange={(e) => setValor(e.target.value)} />
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
            <button type="button" className="btn btn-ghost" onClick={() => router.push(`/financeiro/contas-a-pagar/${id}`)}>
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
