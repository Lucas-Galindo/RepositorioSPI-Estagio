"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

// Rota antiga (pre-evolucao do Financeiro): pagamentos virou "Contas a
// Receber" dentro da secao Financeiro. Mantido como redirect para nao
// quebrar links/favoritos ja salvos.
export default function PagamentosRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/financeiro/contas-a-receber");
  }, [router]);

  return null;
}
