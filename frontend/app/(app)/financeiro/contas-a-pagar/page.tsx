"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import {
  listarContasPagar,
  listarCategoriasDespesa,
  type ContaPagar,
  type CategoriaDespesa,
} from "@/lib/api/contasPagar";
import { listarFormasPagamento, type FormaPagamento } from "@/lib/api/pagamentos";
import { currency, fmtData } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function ContasAPagarPage() {
  usePageHeader("Financeiro", "Contas a Pagar");
  const { sessao } = useAuth();
  const router = useRouter();

  const [contas, setContas] = useState<ContaPagar[] | null>(null);
  const [categorias, setCategorias] = useState<CategoriaDespesa[]>([]);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [erro, setErro] = useState("");

  const [status, setStatus] = useState("");
  const [categoriaDespesaId, setCategoriaDespesaId] = useState("");
  const [favorecido, setFavorecido] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [vencimentoInicio, setVencimentoInicio] = useState("");
  const [vencimentoFim, setVencimentoFim] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarCategoriasDespesa(sessao.accessToken).then(setCategorias).catch(() => setCategorias([]));
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
  }, [sessao]);

  // Contador de requisicoes: evita que uma resposta antiga (de um filtro ja
  // substituido) chegue depois da mais recente e sobrescreva a tela com dados
  // errados -- pode acontecer quando dois filtros mudam em sequencia rapida.
  const requisicaoAtual = useRef(0);
  useEffect(() => {
    if (!sessao) return;
    const idDestaRequisicao = ++requisicaoAtual.current;
    listarContasPagar(sessao.accessToken, {
      categoriaDespesaId: categoriaDespesaId ? Number(categoriaDespesaId) : undefined,
      status: status || undefined,
      favorecido: favorecido || undefined,
      vencimentoInicio: vencimentoInicio || undefined,
      vencimentoFim: vencimentoFim || undefined,
    })
      .then((dados) => {
        if (idDestaRequisicao === requisicaoAtual.current) setContas(dados);
      })
      .catch((excecao) => {
        if (idDestaRequisicao === requisicaoAtual.current) {
          setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar as contas a pagar.");
        }
      });
  }, [sessao, categoriaDespesaId, status, favorecido, vencimentoInicio, vencimentoFim]);

  // Forma de pagamento e filtrada no cliente: o backend de Contas a Pagar nao
  // tem esse parametro (mesmo raciocinio da Sprint 5 para Contas a Receber).
  const filtrados = useMemo(() => {
    return (contas ?? []).filter((c) => {
      if (formaPagamentoId && c.formaPagamentoId !== Number(formaPagamentoId)) return false;
      return true;
    });
  }, [contas, formaPagamentoId]);

  const total = filtrados.length;
  const totalAPagar = filtrados
    .filter((c) => c.status === "Pendente" || c.status === "Atrasado")
    .reduce((s, c) => s + c.valor, 0);
  const totalPago = filtrados.filter((c) => c.status === "Pago").reduce((s, c) => s + c.valor, 0);

  const limparFiltros = () => {
    setStatus("");
    setCategoriaDespesaId("");
    setFavorecido("");
    setFormaPagamentoId("");
    setVencimentoInicio("");
    setVencimentoFim("");
  };
  const temFiltro = Boolean(status || categoriaDespesaId || favorecido || formaPagamentoId || vencimentoInicio || vencimentoFim);

  return (
    <>
      <div className="kpi-row kpi-row-3">
        <div className="kpi">
          <div className="label">
            <Icon name="money" size={12} /> Pago
          </div>
          <div className="value turq">{currency(totalPago)}</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="warn" size={12} /> A pagar + atrasado
          </div>
          <div className="value danger">{currency(totalAPagar)}</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="wallet" size={12} /> Total de registros
          </div>
          <div className="value">{total}</div>
        </div>
      </div>

      <div className="filter-bar">
        <select className="filter-select" value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="">Status — todos</option>
          <option value="Pendente">Pendente</option>
          <option value="Pago">Pago</option>
          <option value="Atrasado">Atrasado</option>
          <option value="Cancelado">Cancelado</option>
        </select>
        <select className="filter-select" value={categoriaDespesaId} onChange={(e) => setCategoriaDespesaId(e.target.value)}>
          <option value="">Categoria — todas</option>
          {categorias.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nome}
            </option>
          ))}
        </select>
        <select className="filter-select" value={formaPagamentoId} onChange={(e) => setFormaPagamentoId(e.target.value)}>
          <option value="">Forma — todas</option>
          {formas.map((f) => (
            <option key={f.id} value={f.id}>
              {f.forma}
            </option>
          ))}
        </select>
        <input
          className="filter-input"
          value={favorecido}
          onChange={(e) => setFavorecido(e.target.value)}
          placeholder="Fornecedor/favorecido"
        />
        <input
          className="filter-input"
          type="date"
          value={vencimentoInicio}
          onChange={(e) => setVencimentoInicio(e.target.value)}
          title="Vencimento a partir de"
        />
        <input
          className="filter-input"
          type="date"
          value={vencimentoFim}
          onChange={(e) => setVencimentoFim(e.target.value)}
          title="Vencimento até"
        />
        {temFiltro && (
          <button type="button" className="filter-clear" onClick={limparFiltros}>
            Limpar filtros
          </button>
        )}
      </div>

      <div className="row-gap">
        <span className="count-text">{total} conta(s) a pagar encontrada(s)</span>
        <Link href="/financeiro/contas-a-pagar/novo" className="btn btn-primary">
          <Icon name="plus" size={13} /> Nova conta a pagar
        </Link>
      </div>

      {erro && <EmptyState title="Erro ao carregar" desc={erro} />}

      {!erro && (
        <div className="table-wrap">
          {contas === null ? (
            <p className="count-text" style={{ padding: 24 }}>Carregando...</p>
          ) : total === 0 ? (
            <EmptyState title="Nenhuma conta a pagar encontrada" desc="Registre a primeira conta a pagar para começar." />
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Descrição</th>
                  <th>Categoria</th>
                  <th>Favorecido</th>
                  <th>Vencimento</th>
                  <th>Pagamento</th>
                  <th>Valor</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {filtrados.map((c) => (
                  <tr className="row-link" key={c.id} onClick={() => router.push(`/financeiro/contas-a-pagar/${c.id}`)}>
                    <td>{c.descricao}</td>
                    <td>{c.categoriaDespesaNome}</td>
                    <td>{c.favorecido ?? "—"}</td>
                    <td>{fmtData(c.dataVencimento)}</td>
                    <td>{c.dataPagamento ? fmtData(c.dataPagamento) : "—"}</td>
                    <td>{currency(c.valor)}</td>
                    <td>
                      <StatusPill status={c.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </>
  );
}
