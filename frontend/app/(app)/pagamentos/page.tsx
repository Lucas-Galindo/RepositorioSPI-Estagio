"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarPagamentos, listarFormasPagamento, type Pagamento, type FormaPagamento } from "@/lib/api/pagamentos";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { listarMaterias, type Materia } from "@/lib/api/materias";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { currency, fmtData, initials, avatarColor } from "@/lib/format";

// Planilha de Pagamentos (instrucao direta do usuario): a rota /pagamentos
// tinha sido consolidada dentro de Financeiro > Contas a Receber numa
// sprint anterior e virou so um redirect. Reconstruida aqui no padrao do
// protótipo (ROUTES.pagamentos) -- filtros por matéria/RA que Contas a
// Receber nao tem -- sem remover ou alterar Contas a Receber, que continua
// existindo em paralelo. Detalhe/edicao/criacao de pagamento continuam
// reaproveitando as telas ja prontas de Contas a Receber (mesma entidade).
function inSemestre(dataIso: string, semestre: string): boolean {
  if (!semestre) return true;
  const mes = Number(dataIso.split("-")[1]);
  return semestre === "1" ? mes <= 6 : mes >= 7;
}

export default function PagamentosPage() {
  usePageHeader("Pagamentos", "Registros de pagamentos individuais dos alunos");
  const { sessao } = useAuth();
  const router = useRouter();

  const [pagamentos, setPagamentos] = useState<Pagamento[] | null>(null);
  const [alunos, setAlunos] = useState<Aluno[]>([]);
  const [materias, setMaterias] = useState<Materia[]>([]);
  const [formas, setFormas] = useState<FormaPagamento[]>([]);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [erro, setErro] = useState("");

  const [status, setStatus] = useState("");
  const [materiaId, setMateriaId] = useState("");
  const [formaPagamentoId, setFormaPagamentoId] = useState("");
  const [nome, setNome] = useState("");
  const [ra, setRa] = useState("");
  const [semestre, setSemestre] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarPagamentos(sessao.accessToken)
      .then(setPagamentos)
      .catch(() => setErro("Não foi possível carregar os pagamentos."));
    listarAlunos(sessao.accessToken).then(setAlunos).catch(() => setAlunos([]));
    listarMaterias(sessao.accessToken).then(setMaterias).catch(() => setMaterias([]));
    listarFormasPagamento(sessao.accessToken).then(setFormas).catch(() => setFormas([]));
    listarAulas(sessao.accessToken).then(setAulas).catch(() => setAulas([]));
  }, [sessao]);

  const raPorAluno = useMemo(() => new Map(alunos.map((a) => [a.id, a.ra])), [alunos]);
  const materiaIdsPorAula = useMemo(() => new Map(aulas.map((a) => [a.id, a.materiaId])), [aulas]);

  const filtrados = useMemo(() => {
    return (pagamentos ?? []).filter((p) => {
      if (status && p.status !== status) return false;
      if (formaPagamentoId && p.formaPagamentoId !== Number(formaPagamentoId)) return false;
      if (materiaId && !p.aulaIds.some((aulaId) => materiaIdsPorAula.get(aulaId) === Number(materiaId))) return false;
      if (nome && !p.alunoNome.toLowerCase().includes(nome.toLowerCase())) return false;
      if (ra && !(raPorAluno.get(p.alunoId) ?? "").includes(ra)) return false;
      if (semestre && !inSemestre(p.dataVencimento, semestre)) return false;
      return true;
    });
  }, [pagamentos, status, formaPagamentoId, materiaId, nome, ra, semestre, materiaIdsPorAula, raPorAluno]);

  const limparFiltros = () => {
    setStatus("");
    setMateriaId("");
    setFormaPagamentoId("");
    setNome("");
    setRa("");
    setSemestre("");
  };
  const temFiltro = Boolean(status || materiaId || formaPagamentoId || nome || ra || semestre);

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;

  return (
    <>
      <div className="row-gap">
        <div>
          <div className="section-title">Planilha de pagamentos</div>
          <div className="section-sub" style={{ marginBottom: 0 }}>
            Lançamentos individuais por aluno · veja os dashboards em Relatórios
          </div>
        </div>
        <Link href="/relatorios/pagamentos" className="btn btn-ghost btn-sm">
          <Icon name="money" size={13} /> Ver dashboard
        </Link>
      </div>

      <div className="filter-bar">
        <select className="filter-select" value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="">Status — todos</option>
          <option value="Pago">Pago</option>
          <option value="Pendente">Pendente</option>
          <option value="Atrasado">Atrasado</option>
          <option value="Cancelado">Cancelado</option>
        </select>
        <select className="filter-select" value={materiaId} onChange={(e) => setMateriaId(e.target.value)}>
          <option value="">Matéria — todas</option>
          {materias.map((m) => (
            <option key={m.id} value={m.id}>
              {m.nome}
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
        <select className="filter-select" value={semestre} onChange={(e) => setSemestre(e.target.value)}>
          <option value="">Semestre — todos</option>
          <option value="1">1º semestre</option>
          <option value="2">2º semestre</option>
        </select>
        <input className="filter-input" placeholder="Buscar por aluno..." value={nome} onChange={(e) => setNome(e.target.value)} />
        <input className="filter-input" placeholder="Buscar por RA..." value={ra} onChange={(e) => setRa(e.target.value)} />
        {temFiltro && (
          <button type="button" className="filter-clear" onClick={limparFiltros}>
            Limpar filtros
          </button>
        )}
      </div>

      <div className="row-gap" style={{ justifyContent: "flex-end" }}>
        <Link href="/financeiro/contas-a-receber/novo" className="btn btn-primary">
          <Icon name="plus" size={13} /> Registrar pagamento
        </Link>
      </div>

      <div className="table-wrap">
        {pagamentos === null ? (
          <p className="count-text" style={{ padding: 24 }}>
            Carregando...
          </p>
        ) : (
          <>
            <div className="table-head">
              <span className="count-text">{filtrados.length} pagamento(s) encontrado(s)</span>
            </div>
            {filtrados.length ? (
              <table>
                <thead>
                  <tr>
                    <th>Aluno</th>
                    <th>Data</th>
                    <th>Valor</th>
                    <th>Forma</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {filtrados.map((p) => (
                    <tr className="row-link" key={p.id} onClick={() => router.push(`/financeiro/contas-a-receber/${p.id}`)}>
                      <td>
                        <div className="cell">
                          <div className="mini-avatar" style={{ background: avatarColor(p.alunoId) }}>
                            {initials(p.alunoNome)}
                          </div>
                          {p.alunoNome}
                        </div>
                      </td>
                      <td>{fmtData(p.dataVencimento)}</td>
                      <td>{currency(p.valorFinal)}</td>
                      <td>{p.formaPagamentoNome ?? "—"}</td>
                      <td>
                        <StatusPill status={p.status} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <EmptyState title="Nenhum pagamento encontrado" desc="Ajuste os filtros para ver outros resultados." />
            )}
          </>
        )}
      </div>

      <div className="panel" style={{ marginTop: 18 }}>
        <div className="section-title">Métodos de pagamento</div>
        <div className="section-sub">Formas de pagamento cadastradas no sistema</div>
        {formas.length ? (
          <div className="table-wrap" style={{ boxShadow: "none", margin: 0 }}>
            <table>
              <thead>
                <tr>
                  <th>Forma</th>
                  <th>Descrição</th>
                </tr>
              </thead>
              <tbody>
                {formas.map((f) => (
                  <tr key={f.id}>
                    <td style={{ fontWeight: 600 }}>{f.forma}</td>
                    <td>{f.descricao ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState title="Nenhum método de pagamento cadastrado" desc="Cadastre formas de pagamento para vê-las aqui." />
        )}
      </div>
    </>
  );
}
