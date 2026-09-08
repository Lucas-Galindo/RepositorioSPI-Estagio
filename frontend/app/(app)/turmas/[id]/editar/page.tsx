"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { TurmaForm } from "@/components/turmas/TurmaForm";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterTurma, type Turma } from "@/lib/api/turmas";
import { ApiError } from "@/lib/api/client";

export default function EditarTurmaPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Turmas", "Editar turma");
  const { sessao } = useAuth();
  const [turma, setTurma] = useState<Turma | null>(null);
  const [erro, setErro] = useState("");

  useEffect(() => {
    if (!sessao) return;
    obterTurma(Number(id), sessao.accessToken)
      .then(setTurma)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Turma não encontrada."));
  }, [sessao, id]);

  return (
    <>
      <Link href={`/turmas/${id}`} className="breadcrumb">
        <Icon name="back" size={13} /> Voltar
      </Link>
      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {!erro && turma && <TurmaForm turma={turma} />}
    </>
  );
}
