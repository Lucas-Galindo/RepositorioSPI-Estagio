"use client";

import { useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { ApiError } from "@/lib/api/client";
import { registrarContaPagar, listarCategoriasDespesa, type CategoriaDespesa } from "@/lib/api/contasPagar";
import { listarFormasPagamento, type FormaPagamento } from "@/lib/api/pagamentos";

export default function NovaContaAPagarPage() {
  usePageHeader("Financeiro", "Nova conta a pagar");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [categorias, setCategorias] = useState<CategoriaDespesa[]>([]);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);

  const [descricao, setDescricao] = useState("");
  const [categoriaDespesaId, setCategoriaDespesaId] = useState("");
  const [favorecido, setFavorecido] = useState("");
  const [valor, setValor] = useState("");
  const [dataVencimento, setDataVencimento] = useState("");
  const [competencia, setCompetencia] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [observacoes, setObservacoes] = useState("");
  const [status, setStatus] = useState<"Pendente" | "Pago">("Pendente");
  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarCategoriasDespesa(sessao.accessToken).then(setCategorias).catch(() => setCategorias([]));
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
  }, [sessao]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    try {
      const criada = await registrarContaPagar(
        {
          descricao,
          categoriaDespesaId: Number(categoriaDespesaId),
          favorecido: favorecido || null,
          valor: Number(valor) || 0,
          competencia: competencia ? `${competencia}-01` : null,
          dataVencimento,
          formaPagamentoId: formaPagamentoId ? Number(formaPagamentoId) : null,
          observacoes: observacoes || null,
          status,
        },
        sessao.accessToken
      );
      mostrarToast("Conta a pagar registrada com sucesso.");
      router.push(`/financeiro/contas-a-pagar/${criada.id}`);
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível registrar a conta a pagar."]);
    } finally {
      setSalvando(false);
    }
  };

  return (
    <>
      <Link href="/financeiro/contas-a-pagar" className="breadcrumb">
        <Icon name="back" size={13} /> Contas a Pagar
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
                <input required value={descricao} onChange={(e) => setDescricao(e.target.value)} placeholder="Ex: Internet - Setembro/2026" />
              </div>
              <div className="field">
                <label>
                  Categoria<span className="req">*</span>
                </label>
                <select required value={categoriaDespesaId} onChange={(e) => setCategoriaDespesaId(e.target.value)}>
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
              <label>Fornecedor/favorecido (opcional)</label>
              <input value={favorecido} onChange={(e) => setFavorecido(e.target.value)} placeholder="Ex: Provedor XYZ Telecom" />
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
                <input required type="number" step="0.01" min="0" value={valor} onChange={(e) => setValor(e.target.value)} placeholder="120.00" />
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
                <label>Forma de pagamento (opcional)</label>
                <select value={formaPagamentoId} onChange={(e) => setFormaPagamentoId(e.target.value)}>
                  <option value="">Não definida</option>
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
                  <option value="Pago">Pago (pago agora)</option>
                </select>
              </div>
            </div>
            <div className="field" style={{ marginTop: 15 }}>
              <label>Observações (opcional)</label>
              <textarea rows={3} value={observacoes} onChange={(e) => setObservacoes(e.target.value)} />
            </div>
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-ghost" onClick={() => router.push("/financeiro/contas-a-pagar")}>
              Cancelar
            </button>
            <button type="submit" className="btn btn-primary" disabled={salvando}>
              {salvando ? "Salvando..." : "Registrar conta a pagar"}
            </button>
          </div>
        </form>
      </div>
    </>
  );
}
