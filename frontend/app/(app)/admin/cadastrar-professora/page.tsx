"use client";

import { CadastrarProfessoraForm } from "@/components/admin/CadastrarProfessoraForm";
import { usePageHeader } from "@/lib/usePageHeader";

export default function CadastrarProfessoraPage() {
  usePageHeader("Cadastrar professora", "Cadastro único — só é possível enquanto nenhuma professora existir");

  return <CadastrarProfessoraForm />;
}
