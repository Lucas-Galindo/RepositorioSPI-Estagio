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
  listarPagamentos,
  listarFormasPagamento,
  listarCategoriasReceita,
  type Pagamento,
  type FormaPagamento,
  type CategoriaReceita,
} from "@/lib/api/pagamentos";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { currency, fmtData } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function ContasAReceberPage() {
  usePageHeader("Financeiro", "Contas a Receber");
  const { sessao } = useAuth();
  const router = useRouter();

  const [pagamentos, setPagamentos] = useState<Pagamento[] | null>(null);
  const [alunos, setAlunos] = useState<Aluno[]>([]);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [categorias, setCategorias] = useState<CategoriaReceita[]>([]);
  const [erro, setErro] = useState("");

  const [status, setStatus] = useState("");
  const [alunoId, setAlunoId] = useState("");
  const [categoriaReceitaId, setCategoriaReceitaId] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [vencimentoInicio, setVencimentoInicio] = useState("");
  const [vencimentoFim, setVencimentoFim] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarAlunos(sessao.accessToken, { ativo: true }).then(setAlunos).catch(() => setAlunos([]));
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
    listarCategoriasReceita(sessao.accessToken).then(setCategorias).catch(() => setCategorias([]));
  }, [sessao]);

  // Contador de requisicoes: evita que uma resposta antiga (de um filtro ja
  // substituido) chegue depois da mais recente e sobrescreva a tela com dados
  // errados -- pode acontecer quando dois filtros mudam em sequencia rapida.
  const requisicaoAtual = useRef(0);
  useEffect(() => {
    if (!sessao) return;
    const idDestaRequisicao = ++requisicaoAtual.current;
    listarPagamentos(sessao.accessToken, {
      alunoId: alunoId ? Number(alunoId) : undefined,
      status: status || undefined,
      vencimentoInicio: vencimentoInicio || undefined,
      vencimentoFim: vencimentoFim || undefined,
    })
      .then((dados) => {
        if (idDestaRequisicao === requisicaoAtual.current) setPagamentos(dados);
      })
      .catch((excecao) => {
        if (idDestaRequisicao === requisicaoAtual.current) {
          setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar as contas a receber.");
        }
      });
  }, [sessao, alunoId, status, vencimentoInicio, vencimentoFim]);

  // Categoria e forma de pagamento sao filtradas no cliente: o volume de
  // contas de uma professora independente e pequeno, e assim evitamos
  // inflar a API com mais parametros de filtro so para isso.
  const filtrados = useMemo(() => {
    return (pagamentos ?? []).filter((p) => {
      if (categoriaReceitaId && p.categoriaReceitaId !== Number(categoriaReceitaId)) return false;
      if (formaPagamentoId && p.formaPagamentoId !== Number(formaPagamentoId)) return false;
      return true;
    });
  }, [pagamentos, categoriaReceitaId, formaPagamentoId]);

  const total = filtrados.length;
  const totalPendente = filtrados
    .filter((p) => p.status === "Pendente" || p.status === "Atrasado")
    .reduce((s, p) => s + p.valorFinal, 0);
  const totalPago = filtrados.filter((p) => p.status === "Pago").reduce((s, p) => s + p.valorFinal, 0);

  const limparFiltros = () => {
    setStatus("");
    setAlunoId("");
    setCategoriaReceitaId("");
    setFormaPagamentoId("");
    setVencimentoInicio("");
    setVencimentoFim("");
  };
  const temFiltro = Boolean(status || alunoId || categoriaReceitaId || formaPagamentoId || vencimentoInicio || vencimentoFim);

  return (
    <>
      <div className="kpi-row kpi-row-3">
        <div className="kpi">
          <div className="label">
            <Icon name="wallet" size={12} /> Recebido
          </div>
          <div className="value turq">{currency(totalPago)}</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="warn" size={12} /> Pendente + atrasado
          </div>
          <div className="value danger">{currency(totalPendente)}</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="money" size={12} /> Total de registros
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
        <select className="filter-select" value={alunoId} onChange={(e) => setAlunoId(e.target.value)}>
          <option value="">Aluno — todos</option>
          {alunos.map((a) => (
            <option key={a.id} value={a.id}>
              {a.nome}
            </option>
          ))}
        </select>
        <select className="filter-select" value={categoriaReceitaId} onChange={(e) => setCategoriaReceitaId(e.target.value)}>
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
        <span className="count-text">{total} conta(s) a receber encontrada(s)</span>
        <Link href="/financeiro/contas-a-receber/novo" className="btn btn-primary">
          <Icon name="plus" size={13} /> Nova conta a receber
        </Link>
      </div>

      {erro && <EmptyState title="Erro ao carregar" desc={erro} />}

      {!erro && (
        <div className="table-wrap">
          {pagamentos === null ? (
            <p className="count-text" style={{ padding: 24 }}>Carregando...</p>
          ) : total === 0 ? (
            <EmptyState title="Nenhuma conta a receber encontrada" desc="Registre a primeira conta a receber para começar." />
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Descrição</th>
                  <th>Aluno</th>
                  <th>Categoria</th>
                  <th>Vencimento</th>
                  <th>Recebimento</th>
                  <th>Valor</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {filtrados.map((p) => (
                  <tr className="row-link" key={p.id} onClick={() => router.push(`/financeiro/contas-a-receber/${p.id}`)}>
                    <td>{p.descricao ?? "—"}</td>
                    <td>{p.alunoNome}</td>
                    <td>{p.categoriaReceitaNome ?? "—"}</td>
                    <td>{fmtData(p.dataVencimento)}</td>
                    <td>{p.dataPagamento ? fmtData(p.dataPagamento) : "—"}</td>
                    <td>{currency(p.valorFinal)}</td>
                    <td>
                      <StatusPill status={p.status} />
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
