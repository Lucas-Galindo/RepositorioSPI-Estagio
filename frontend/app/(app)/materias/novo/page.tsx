"use client";

import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { MateriaForm } from "@/components/materias/MateriaForm";
import { usePageHeader } from "@/lib/usePageHeader";

export default function NovaMateriaPage() {
  usePageHeader("Matérias", "Cadastrar nova matéria");

  return (
    <>
      <Link href="/materias" className="breadcrumb">
        <Icon name="back" size={13} /> Matérias
      </Link>
      <MateriaForm />
    </>
  );
}
