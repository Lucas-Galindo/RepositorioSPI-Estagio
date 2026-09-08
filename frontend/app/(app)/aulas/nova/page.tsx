"use client";

import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { AulaForm } from "@/components/aulas/AulaForm";
import { usePageHeader } from "@/lib/usePageHeader";

export default function NovaAulaPage() {
  usePageHeader("Aulas", "Agendar nova aula");

  return (
    <>
      <Link href="/aulas" className="breadcrumb">
        <Icon name="back" size={13} /> Aulas
      </Link>
      <AulaForm />
    </>
  );
}
