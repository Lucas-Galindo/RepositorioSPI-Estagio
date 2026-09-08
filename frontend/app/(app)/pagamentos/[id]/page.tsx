"use client";

import { use, useEffect } from "react";
import { useRouter } from "next/navigation";

// Rota antiga -- ver app/(app)/pagamentos/page.tsx.
export default function PagamentoDetailRedirect({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();

  useEffect(() => {
    router.replace(`/financeiro/contas-a-receber/${id}`);
  }, [router, id]);

  return null;
}
