"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { MateriaForm } from "@/components/materias/MateriaForm";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterMateria, type Materia } from "@/lib/api/materias";
import { ApiError } from "@/lib/api/client";

export default function EditarMateriaPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Matérias", "Editar matéria");
  const { sessao } = useAuth();
  const [materia, setMateria] = useState<Materia | null>(null);
  const [erro, setErro] = useState("");

  useEffect(() => {
    if (!sessao) return;
    obterMateria(Number(id), sessao.accessToken)
      .then(setMateria)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Matéria não encontrada."));
  }, [sessao, id]);

  return (
    <>
      <Link href={`/materias/${id}`} className="breadcrumb">
        <Icon name="back" size={13} /> Voltar
      </Link>
      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {!erro && materia && <MateriaForm materia={materia} />}
    </>
  );
}
