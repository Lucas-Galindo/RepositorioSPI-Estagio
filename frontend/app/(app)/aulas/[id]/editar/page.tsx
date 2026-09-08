"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { AulaForm } from "@/components/aulas/AulaForm";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterAula, type Aula } from "@/lib/api/aulas";
import { ApiError } from "@/lib/api/client";

export default function EditarAulaPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Aulas", "Editar aula");
  const { sessao } = useAuth();
  const [aula, setAula] = useState<Aula | null>(null);
  const [erro, setErro] = useState("");

  useEffect(() => {
    if (!sessao) return;
    obterAula(Number(id), sessao.accessToken)
      .then(setAula)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Aula não encontrada."));
  }, [sessao, id]);

  return (
    <>
      <Link href={`/aulas/${id}`} className="breadcrumb">
        <Icon name="back" size={13} /> Voltar
      </Link>
      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {!erro && aula && <AulaForm aula={aula} />}
    </>
  );
}
