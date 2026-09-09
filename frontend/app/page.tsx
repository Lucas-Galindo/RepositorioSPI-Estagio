"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { AuthSidePanel } from "@/components/auth/AuthSidePanel";
import { LoginForm } from "@/components/auth/LoginForm";
import { ThemeToggle } from "@/components/shared/ThemeToggle";
import { useTheme } from "@/contexts/ThemeContext";
import { useAuth } from "@/contexts/AuthContext";

export default function LoginPage() {
  const { dark } = useTheme();
  const { sessao, carregando } = useAuth();
  const router = useRouter();

  useEffect(() => {
    // Se "Manter conectada" restaurou uma sessao valida, nao faz sentido
    // mostrar o login de novo -- manda direto pra area correspondente.
    if (carregando || !sessao) return;
    router.replace(sessao.perfil === "Admin" ? "/admin/cadastrar-professora" : "/dashboard");
  }, [sessao, carregando, router]);

  if (carregando || sessao) {
    return null;
  }

  return (
    <div className={dark ? "spi-login dark-mode" : "spi-login"}>
      <ThemeToggle className="theme-toggle" />

      <div className="auth-shell">
        <AuthSidePanel />

        <section className="auth-form-panel">
          <LoginForm />
        </section>
      </div>
    </div>
  );
}
