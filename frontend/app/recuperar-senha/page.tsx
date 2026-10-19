"use client";

import { AuthSidePanel } from "@/components/auth/AuthSidePanel";
import { EsqueciSenhaForm } from "@/components/auth/EsqueciSenhaForm";
import { ThemeToggle } from "@/components/shared/ThemeToggle";

export default function RecuperarSenhaPage() {

  return (
    <div className="spi-login">
      <ThemeToggle className="theme-toggle" />

      <div className="auth-shell">
        <AuthSidePanel />

        <section className="auth-form-panel">
          <EsqueciSenhaForm />
        </section>
      </div>
    </div>
  );
}
