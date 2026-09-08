"use client";

import { AuthSidePanel } from "@/components/auth/AuthSidePanel";
import { LoginForm } from "@/components/auth/LoginForm";
import { ThemeToggle } from "@/components/shared/ThemeToggle";
import { useTheme } from "@/contexts/ThemeContext";

export default function LoginPage() {
  const { dark } = useTheme();

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
