"use client";

import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { TurmaForm } from "@/components/turmas/TurmaForm";
import { usePageHeader } from "@/lib/usePageHeader";

export default function NovaTurmaPage() {
  usePageHeader("Turmas", "Cadastrar nova turma");

  return (
    <>
      <Link href="/turmas" className="breadcrumb">
        <Icon name="back" size={13} /> Turmas
      </Link>
      <TurmaForm />
    </>
  );
}
