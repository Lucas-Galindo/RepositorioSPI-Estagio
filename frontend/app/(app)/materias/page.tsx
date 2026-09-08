"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusPill } from "@/components/shared/StatusPill";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarMaterias, type Materia } from "@/lib/api/materias";
import { ApiError } from "@/lib/api/client";

export default function MateriasPage() {
  usePageHeader("Matérias", "Organize as matérias que você leciona");
  const { sessao } = useAuth();
  const router = useRouter();
  const [materias, setMaterias] = useState<Materia[] | null>(null);
  const [erro, setErro] = useState("");
  const [nome, setNome] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarMaterias(sessao.accessToken, { nome: nome || undefined })
      .then(setMaterias)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar as matérias."));
  }, [sessao, nome]);

  const total = materias?.length ?? 0;

  return (
    <>
      <div className="filter-bar">
        <input className="filter-input" placeholder="Buscar por nome..." value={nome} onChange={(e) => setNome(e.target.value)} />
      </div>

      <div className="row-gap">
        <span className="count-text">{total} matéria(s) encontrada(s)</span>
        <Link href="/materias/novo" className="btn btn-primary">
          <Icon name="plus" size={13} /> Nova matéria
        </Link>
      </div>

      {erro && <EmptyState title="Erro ao carregar" desc={erro} />}

      {!erro && (
        <div className="table-wrap">
          {materias === null ? (
            <p className="count-text" style={{ padding: 24 }}>Carregando...</p>
          ) : total === 0 ? (
            <EmptyState title="Nenhuma matéria encontrada" desc="Cadastre a primeira matéria para começar." />
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>Nível</th>
                  <th>Descrição</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {materias.map((m) => (
                  <tr className="row-link" key={m.id} onClick={() => router.push(`/materias/${m.id}`)}>
                    <td>{m.nome}</td>
                    <td>{m.nivel ?? "—"}</td>
                    <td>{m.descricao ?? "—"}</td>
                    <td>
                      <StatusPill status={m.ativo ? "Ativo" : "Inativo"} />
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
