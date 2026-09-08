"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { AppShell } from "@/components/layout/AppShell";
import { HeaderProvider } from "@/contexts/HeaderContext";
import { useAuth } from "@/contexts/AuthContext";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { sessao } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    // Sessao vive so em memoria (AuthContext): sem login nesta aba/sessao,
    // nao ha como abrir a area logada -- volta para o login.
    if (!sessao) {
      router.replace("/");
      return;
    }

    // Admin so tem acesso as rotas /admin/*; as demais sao Authorize
    // Roles=Professor no backend e dariam 403 pra ele.
    const emRotaDeAdmin = pathname.startsWith("/admin");
    if (sessao.perfil === "Admin" && !emRotaDeAdmin) {
      router.replace("/admin/cadastrar-professora");
    } else if (sessao.perfil !== "Admin" && emRotaDeAdmin) {
      router.replace("/dashboard");
    }
  }, [sessao, pathname, router]);

  if (!sessao) {
    return null;
  }

  return (
    <HeaderProvider>
      <AppShell>{children}</AppShell>
    </HeaderProvider>
  );
}
