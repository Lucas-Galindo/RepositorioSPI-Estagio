"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarLembretes, excluirLembrete, type Lembrete } from "@/lib/api/lembretes";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { listarAulas, type Aula } from "@/lib/api/aulas";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { fmtDataHora } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

// Lembretes (Sprint 7 / Estorias 5 e 19): o backend ja existia e estava
// completo (CRUD + calculo automatico de HoraProgramada + disparo por
// e-mail via LembreteDispatcherService) -- so a tela nunca tinha sido
// ligada nele (o placeholder anterior dizia "backend nao implementado",
// o que ja nao era verdade).
//
// Reconstruida com base no protótipo (ROUTES.lembretes), com os filtros
// que ja existiam la (status, turma, canal, destinatários, antecedência,
// período) mais os pedidos agora: matéria (busca por nome), aluno e
// semestre. Lembrete so se liga a Turma no schema real -- matéria e aluno
// sao derivados via as aulas/alunos daquela turma, mesmo padrao usado nos
// dashboards de Relatórios.
//
// Diferença do protótipo: la, clicar numa linha ia pro detalhe da turma
// (nao existia edicao de lembrete). Aqui, como o backend ja suporta
// editar/excluir um lembrete especifico, a linha leva direto pra edicao.
const LABEL_DESTINATARIOS: Record<string, string> = {
  Alunos: "Alunos",
  Responsaveis: "Responsáveis",
  AlunosEResponsaveis: "Alunos e responsáveis",
};

function periodoSemestre(semestre: "1" | "2"): { inicio: string; fim: string } {
  const ano = new Date().getFullYear();
  return semestre === "1" ? { inicio: `${ano}-01-01`, fim: `${ano}-06-30` } : { inicio: `${ano}-07-01`, fim: `${ano}-12-31` };
}

export default function LembretesPage() {
  usePageHeader("Lembretes", "Notificações automáticas vinculadas às suas turmas");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [lembretes, setLembretes] = useState<Lembrete[] | null>(null);
  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [aulas, setAulas] = useState<Aula[]>([]);
  const [alunos, setAlunos] = useState<Aluno[]>([]);
  const [erro, setErro] = useState("");
  const [exclusaoPendente, setExclusaoPendente] = useState<Lembrete | null>(null);
  const [excluindo, setExcluindo] = useState(false);

  const [status, setStatus] = useState("");
  const [turmaId, setTurmaId] = useState("");
  const [canal, setCanal] = useState("");
  const [destinatarios, setDestinatarios] = useState("");
  const [antecedenciaHora, setAntecedenciaHora] = useState("");
  const [materiaNome, setMateriaNome] = useState("");
  const [alunoId, setAlunoId] = useState("");
  const [inicio, setInicio] = useState("");
  const [fim, setFim] = useState("");
  const [semestre, setSemestre] = useState("");

  const carregar = () => {
    if (!sessao) return;
    listarLembretes(sessao.accessToken)
      .then(setLembretes)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar os lembretes."));
  };

  useEffect(() => {
    carregar();
    if (!sessao) return;
    listarTurmas(sessao.accessToken).then(setTurmas).catch(() => setTurmas([]));
    listarAulas(sessao.accessToken).then(setAulas).catch(() => setAulas([]));
    listarAlunos(sessao.accessToken).then(setAlunos).catch(() => setAlunos([]));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessao]);

  const materiasPorTurma = useMemo(() => {
    const mapa = new Map<number, Set<string>>();
    aulas.forEach((a) => {
      if (a.turmaId === null) return;
      if (!mapa.has(a.turmaId)) mapa.set(a.turmaId, new Set());
      mapa.get(a.turmaId)!.add(a.materiaNome);
    });
    return mapa;
  }, [aulas]);

  const alunosPorTurma = useMemo(() => {
    const mapa = new Map<number, Set<number>>();
    turmas.forEach((t) => mapa.set(t.id, new Set(t.alunos.map((a) => a.id))));
    return mapa;
  }, [turmas]);

  const antecedenciasDisponiveis = useMemo(
    () => [...new Set((lembretes ?? []).map((r) => r.antecedenciaHora))].sort((a, b) => a - b),
    [lembretes]
  );

  const selecionarSemestre = (valor: string) => {
    setSemestre(valor);
    if (valor === "1" || valor === "2") {
      const periodo = periodoSemestre(valor);
      setInicio(periodo.inicio);
      setFim(periodo.fim);
    }
  };

  const limparFiltros = () => {
    setStatus("");
    setTurmaId("");
    setCanal("");
    setDestinatarios("");
    setAntecedenciaHora("");
    setMateriaNome("");
    setAlunoId("");
    setInicio("");
    setFim("");
    setSemestre("");
  };
  const temFiltro = Boolean(
    status || turmaId || canal || destinatarios || antecedenciaHora || materiaNome || alunoId || inicio || fim
  );

  const lista = (lembretes ?? []).filter((r) => {
    if (status && r.status !== status) return false;
    if (turmaId && r.turmaId !== Number(turmaId)) return false;
    if (canal && r.canal !== canal) return false;
    if (destinatarios && r.destinatarios !== destinatarios) return false;
    if (antecedenciaHora && r.antecedenciaHora !== Number(antecedenciaHora)) return false;
    if (materiaNome) {
      const materias = materiasPorTurma.get(r.turmaId);
      const encontrou = materias && [...materias].some((m) => m.toLowerCase().includes(materiaNome.toLowerCase()));
      if (!encontrou) return false;
    }
    if (alunoId && !(alunosPorTurma.get(r.turmaId)?.has(Number(alunoId)) ?? false)) return false;
    const dataProgramada = r.horaProgramada.slice(0, 10);
    if (inicio && dataProgramada < inicio) return false;
    if (fim && dataProgramada > fim) return false;
    return true;
  });

  const pendentes = lista.filter((r) => r.status === "Pendente").length;
  const enviados = lista.filter((r) => r.status === "Enviado").length;
  const falharam = lista.filter((r) => r.status === "Falha").length;
  const cancelados = lista.filter((r) => r.status === "Cancelado").length;

  const handleExcluir = async () => {
    if (!sessao || !exclusaoPendente) return;
    setExcluindo(true);
    try {
      await excluirLembrete(exclusaoPendente.id, sessao.accessToken);
      mostrarToast("Lembrete excluído com sucesso.");
      setExclusaoPendente(null);
      carregar();
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir o lembrete.");
    } finally {
      setExcluindo(false);
    }
  };

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;

  return (
    <>
      <div className="kpi-row">
        <div className="kpi">
          <div className="label">Pendentes</div>
          <div className="value gold">{pendentes}</div>
        </div>
        <div className="kpi">
          <div className="label">Enviados</div>
          <div className="value turq">{enviados}</div>
        </div>
        <div className="kpi">
          <div className="label">Falharam</div>
          <div className="value danger">{falharam}</div>
        </div>
        <div className="kpi">
          <div className="label">Cancelados</div>
          <div className="value">{cancelados}</div>
        </div>
      </div>

      <div className="filter-bar">
        <select className="filter-select" value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="">Status — todos</option>
          <option value="Enviado">Enviado</option>
          <option value="Pendente">Pendente</option>
          <option value="Falha">Falha</option>
          <option value="Cancelado">Cancelado</option>
        </select>
        <select className="filter-select" value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
          <option value="">Turma — todas</option>
          {turmas.map((t) => (
            <option key={t.id} value={t.id}>
              {t.nome}
            </option>
          ))}
        </select>
        <select className="filter-select" value={canal} onChange={(e) => setCanal(e.target.value)}>
          <option value="">Canal — todos</option>
          <option value="WhatsApp">WhatsApp</option>
          <option value="Email">E-mail</option>
          <option value="SMS">SMS</option>
        </select>
        <select className="filter-select" value={destinatarios} onChange={(e) => setDestinatarios(e.target.value)}>
          <option value="">Destinatários — todos</option>
          <option value="Alunos">Alunos</option>
          <option value="Responsaveis">Responsáveis</option>
          <option value="AlunosEResponsaveis">Alunos e responsáveis</option>
        </select>
        <select className="filter-select" value={antecedenciaHora} onChange={(e) => setAntecedenciaHora(e.target.value)}>
          <option value="">Antecedência — todas</option>
          {antecedenciasDisponiveis.map((a) => (
            <option key={a} value={a}>
              {a}h antes
            </option>
          ))}
        </select>
        <input
          className="filter-input"
          placeholder="Buscar matéria..."
          value={materiaNome}
          onChange={(e) => setMateriaNome(e.target.value)}
        />
        <select className="filter-select" value={alunoId} onChange={(e) => setAlunoId(e.target.value)}>
          <option value="">Aluno — todos</option>
          {alunos.map((a) => (
            <option key={a.id} value={a.id}>
              {a.nome}
            </option>
          ))}
        </select>
        <input className="filter-input" type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} title="Próxima notificação de" />
        <input className="filter-input" type="date" value={fim} onChange={(e) => setFim(e.target.value)} title="Próxima notificação até" />
        <select className="filter-select" value={semestre} onChange={(e) => selecionarSemestre(e.target.value)}>
          <option value="">Semestre — todos</option>
          <option value="1">1º semestre</option>
          <option value="2">2º semestre</option>
        </select>
        {temFiltro && (
          <button type="button" className="filter-clear" onClick={limparFiltros}>
            Limpar filtros
          </button>
        )}
      </div>

      <div className="row-gap">
        <span className="count-text">{lista.length} lembrete(s) encontrado(s)</span>
        <Link href="/lembretes/novo" className="btn btn-primary">
          <Icon name="plus" size={13} /> Novo lembrete
        </Link>
      </div>

      <div className="table-wrap">
        {lembretes === null ? (
          <p className="count-text" style={{ padding: 24 }}>
            Carregando...
          </p>
        ) : lista.length === 0 ? (
          <EmptyState title="Nenhum lembrete encontrado" desc="Ajuste os filtros ou cadastre um novo lembrete." />
        ) : (
          <table>
            <thead>
              <tr>
                <th>Turma</th>
                <th>Destinatários</th>
                <th>Canal</th>
                <th>Antecedência</th>
                <th>Próxima notificação</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {lista.map((r) => (
                <tr className="row-link" key={r.id} onClick={() => router.push(`/lembretes/${r.id}/editar`)}>
                  <td>{r.turmaNome}</td>
                  <td>{LABEL_DESTINATARIOS[r.destinatarios] ?? r.destinatarios}</td>
                  <td>{r.canal === "Email" ? "E-mail" : r.canal}</td>
                  <td>{r.antecedenciaHora}h antes</td>
                  <td>{fmtDataHora(r.horaProgramada)}</td>
                  <td>
                    <StatusBadge status={r.status} />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="icon-round"
                      style={{ width: 28, height: 28, borderRadius: 9, color: "var(--c-danger)" }}
                      title="Excluir lembrete"
                      onClick={(e) => {
                        e.stopPropagation();
                        setExclusaoPendente(r);
                      }}
                    >
                      <Icon name="trash" size={13} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {exclusaoPendente && (
        <ConfirmModal
          titulo="Excluir lembrete?"
          descricao="Esta ação não poderá ser desfeita."
          onConfirmar={handleExcluir}
          onCancelar={() => setExclusaoPendente(null)}
          confirmando={excluindo}
        />
      )}
    </>
  );
}
