"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { AlunoForm } from "@/components/alunos/AlunoForm";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterAluno, type Aluno } from "@/lib/api/alunos";
import { ApiError } from "@/lib/api/client";

export default function EditarAlunoPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Alunos", "Editar dados do aluno");
  const { sessao } = useAuth();
  const [aluno, setAluno] = useState<Aluno | null>(null);
  const [erro, setErro] = useState("");

  useEffect(() => {
    if (!sessao) return;
    obterAluno(Number(id), sessao.accessToken)
      .then(setAluno)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Aluno não encontrado."));
  }, [sessao, id]);

  return (
    <>
      <Link href={`/alunos/${id}`} className="breadcrumb">
        <Icon name="back" size={13} /> Voltar
      </Link>
      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {!erro && aluno && <AlunoForm aluno={aluno} />}
    </>
  );
}
