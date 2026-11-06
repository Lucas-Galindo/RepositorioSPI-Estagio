"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { ApiError } from "@/lib/api/client";

export default function TurmasPage() {
  usePageHeader("Turmas", "Organize seus alunos em grupos de atendimento");
  const { sessao } = useAuth();
  const router = useRouter();
  const [turmas, setTurmas] = useState<Turma[] | null>(null);
  const [erro, setErro] = useState("");
  const [nome, setNome] = useState("");
  // specs/073: o servidor filtra (parametro `ativo`); aqui so o estado do seletor.
  const [status, setStatus] = useState<"" | "Ativo" | "Inativo">("");

  useEffect(() => {
    if (!sessao) return;
    listarTurmas(sessao.accessToken, { nome: nome || undefined, ativo: status === "" ? undefined : status === "Ativo" })
      .then(setTurmas)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar as turmas."));
  }, [sessao, nome, status]);

  const total = turmas?.length ?? 0;
  const semFiltros = !nome && !status;

  return (
    <>
      <div className="filter-bar">
        <select className="filter-select" value={status} onChange={(e) => setStatus(e.target.value as "" | "Ativo" | "Inativo")}>
          <option value="">Status — todos</option>
          <option value="Ativo">Ativo</option>
          <option value="Inativo">Inativo</option>
        </select>
        <input className="filter-input" placeholder="Buscar por nome..." value={nome} onChange={(e) => setNome(e.target.value)} />
        {!semFiltros && (
          <button
            type="button"
            className="filter-clear"
            onClick={() => {
              setNome("");
              setStatus("");
            }}
          >
            <Icon name="x" size={12} /> Limpar filtros
          </button>
        )}
      </div>

      <div className="row-gap">
        <span className="count-text">{total} turma(s) encontrada(s)</span>
        <Link href="/turmas/novo" className="btn btn-primary">
          <Icon name="plus" size={13} /> Nova turma
        </Link>
      </div>

      {erro && <EmptyState title="Erro ao carregar" desc={erro} />}

      {!erro && (
        <div className="report-grid">
          {turmas === null ? (
            <p className="count-text">Carregando...</p>
          ) : total === 0 ? (
            <div style={{ gridColumn: "1 / -1" }}>
              <EmptyState title="Nenhuma turma encontrada" desc="Cadastre a primeira turma para organizar seus alunos." />
            </div>
          ) : (
            turmas.map((t) => (
              <div className="report-card" key={t.id} onClick={() => router.push(`/turmas/${t.id}`)}>
                <div className="report-icon chip-turq">
                  <Icon name="users" size={18} />
                </div>
                <h3>{t.nome}</h3>
                <p>{t.alunos.length} aluno(s) vinculado(s)</p>
                <StatusPill status={t.ativo ? "Ativo" : "Inativo"} />
              </div>
            ))
          )}
        </div>
      )}
    </>
  );
}
