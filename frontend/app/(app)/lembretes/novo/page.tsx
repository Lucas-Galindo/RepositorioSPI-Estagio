"use client";

import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { LembreteForm } from "@/components/lembretes/LembreteForm";
import { usePageHeader } from "@/lib/usePageHeader";

export default function NovoLembretePage() {
  usePageHeader("Lembretes", "Configure uma notificação automática por turma");

  return (
    <>
      <Link href="/lembretes" className="breadcrumb">
        <Icon name="back" size={13} /> Lembretes
      </Link>
      <LembreteForm />
    </>
  );
}
