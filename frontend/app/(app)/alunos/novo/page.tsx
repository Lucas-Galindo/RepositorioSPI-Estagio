"use client";

import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { AlunoForm } from "@/components/alunos/AlunoForm";
import { usePageHeader } from "@/lib/usePageHeader";

export default function NovoAlunoPage() {
  usePageHeader("Alunos", "Cadastrar novo aluno");

  return (
    <>
      <Link href="/alunos" className="breadcrumb">
        <Icon name="back" size={13} /> Alunos
      </Link>
      <AlunoForm />
    </>
  );
}
