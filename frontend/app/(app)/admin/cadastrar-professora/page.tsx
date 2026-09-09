"use client";

import { CadastrarProfessoraForm } from "@/components/admin/CadastrarProfessoraForm";
import { usePageHeader } from "@/lib/usePageHeader";

export default function CadastrarProfessoraPage() {
  usePageHeader("Cadastro da professora", "Cadastro único do sistema — edite os dados caso já exista, ou cadastre a primeira vez");

  return <CadastrarProfessoraForm />;
}
