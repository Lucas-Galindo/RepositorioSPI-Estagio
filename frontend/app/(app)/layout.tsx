"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { AppShell } from "@/components/layout/AppShell";
import { HeaderProvider } from "@/contexts/HeaderContext";
import { useAuth } from "@/contexts/AuthContext";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { sessao, carregando } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    // Enquanto o AuthContext tenta restaurar a sessao a partir do refresh
    // token salvo ("Manter conectada"), ainda nao sabemos se ha sessao ou
    // nao -- so decide redirecionar depois que essa checagem terminar.
    if (carregando) return;

    // Sem sessao (nem em memoria, nem restaurada), nao ha como abrir a area
    // logada -- volta para o login.
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
  }, [sessao, carregando, pathname, router]);

  if (carregando || !sessao) {
    return null;
  }

  return (
    <HeaderProvider>
      <AppShell>{children}</AppShell>
    </HeaderProvider>
  );
}
