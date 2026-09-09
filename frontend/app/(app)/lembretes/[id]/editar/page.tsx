"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { LembreteForm } from "@/components/lembretes/LembreteForm";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { obterLembrete, type Lembrete } from "@/lib/api/lembretes";
import { ApiError } from "@/lib/api/client";

export default function EditarLembretePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Lembretes", "Editar lembrete");
  const { sessao } = useAuth();
  const [lembrete, setLembrete] = useState<Lembrete | null>(null);
  const [erro, setErro] = useState("");

  useEffect(() => {
    if (!sessao) return;
    obterLembrete(Number(id), sessao.accessToken)
      .then(setLembrete)
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Lembrete não encontrado."));
  }, [sessao, id]);

  return (
    <>
      <Link href="/lembretes" className="breadcrumb">
        <Icon name="back" size={13} /> Lembretes
      </Link>
      {erro && <EmptyState title="Não foi possível carregar" desc={erro} />}
      {!erro && lembrete && <LembreteForm lembrete={lembrete} />}
    </>
  );
}
