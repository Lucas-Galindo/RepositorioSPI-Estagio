"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

// Rota antiga -- ver app/(app)/pagamentos/page.tsx.
export default function PagamentosNovoRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/financeiro/contas-a-receber/novo");
  }, [router]);

  return null;
}
