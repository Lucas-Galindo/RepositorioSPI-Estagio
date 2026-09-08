"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { avatarColor, initials } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function AlunosPage() {
  usePageHeader("Alunos", "Consulta e gerenciamento dos alunos cadastrados");
  const { sessao } = useAuth();
  const router = useRouter();
  const [alunos, setAlunos] = useState<Aluno[] | null>(null);
  const [erro, setErro] = useState("");
  const [nome, setNome] = useState("");
  const [ra, setRa] = useState("");
  const [status, setStatus] = useState<"" | "Ativo" | "Inativo">("");

  useEffect(() => {
    if (!sessao) return;

    listarAlunos(sessao.accessToken, {
      nome: nome || undefined,
      ra: ra || undefined,
      ativo: status === "" ? undefined : status === "Ativo",
    })
      .then((lista) => {
        setErro("");
        setAlunos(lista);
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar os alunos."));
  }, [sessao, nome, ra, status]);

  const total = alunos?.length ?? 0;
  const ativos = alunos?.filter((a) => a.ativo).length ?? 0;
  const semFiltros = !nome && !ra && !status;

  return (
    <>
      <div className="kpi-row kpi-row-3">
        <div className="kpi">
          <div className="label">
            <Icon name="users" size={12} /> Total de alunos
          </div>
          <div className="value turq">{total}</div>
          <div className="delta">
            {ativos} ativos · {total - ativos} inativos
          </div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="wallet" size={12} /> Valor médio da aula
          </div>
          <div className="value">
            {alunos?.length ? `R$ ${(alunos.reduce((s, a) => s + a.valorAula, 0) / alunos.length).toFixed(2)}` : "—"}
          </div>
          <div className="delta">entre os alunos listados</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="cal" size={12} /> Frequência média
          </div>
          <div className="value gold">
            {alunos?.length ? Math.round(alunos.reduce((s, a) => s + a.frequencia, 0) / alunos.length) : 0}
          </div>
          <div className="delta">aulas presentes acumuladas</div>
        </div>
      </div>

      <div className="filter-bar">
        <select className="filter-select" value={status} onChange={(e) => setStatus(e.target.value as "" | "Ativo" | "Inativo")}>
          <option value="">Status — todos</option>
          <option value="Ativo">Ativo</option>
          <option value="Inativo">Inativo</option>
        </select>
        <input className="filter-input" placeholder="Buscar por nome..." value={nome} onChange={(e) => setNome(e.target.value)} />
        <input className="filter-input" placeholder="Buscar por RA..." value={ra} onChange={(e) => setRa(e.target.value)} />
        {!semFiltros && (
          <button
            type="button"
            className="filter-clear"
            onClick={() => {
              setNome("");
              setRa("");
              setStatus("");
            }}
          >
            <Icon name="x" size={12} /> Limpar filtros
          </button>
        )}
      </div>

      <div className="row-gap">
        <span className="count-text">{total} aluno(s) encontrado(s)</span>
        <Link href="/alunos/novo" className="btn btn-primary">
          <Icon name="plus" size={13} /> Novo aluno
        </Link>
      </div>

      {erro && <EmptyState title="Erro ao carregar" desc={erro} />}

      {!erro && (
        <div className="table-wrap">
          {alunos === null ? (
            <div className="empty-state">
              <p>Carregando...</p>
            </div>
          ) : total === 0 ? (
            <EmptyState title="Nenhum aluno encontrado" desc="Ajuste os filtros ou cadastre um novo aluno." />
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Aluno</th>
                  <th>RA</th>
                  <th>Turma(s)</th>
                  <th>Telefone</th>
                  <th>Valor da aula</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {alunos.map((a) => (
                  <tr className="row-link" key={a.id} onClick={() => router.push(`/alunos/${a.id}`)}>
                    <td>
                      <div className="cell">
                        <div className="mini-avatar" style={{ background: avatarColor(a.id) }}>
                          {initials(a.nome)}
                        </div>
                        {a.nome}
                      </div>
                    </td>
                    <td>{a.ra}</td>
                    <td>{a.turmas.map((t) => t.nome).join(", ") || "—"}</td>
                    <td>{a.telefoneAluno ?? "—"}</td>
                    <td>R$ {a.valorAula.toFixed(2)}</td>
                    <td>
                      <StatusPill status={a.ativo ? "Ativo" : "Inativo"} />
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
