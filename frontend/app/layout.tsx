import type { Metadata } from "next";
import { AuthProvider } from "@/contexts/AuthContext";
import { ThemeProvider } from "@/contexts/ThemeContext";
import { ToastProvider } from "@/contexts/ToastContext";
// Fontes hospedadas localmente (pacotes @fontsource), nao buscadas do Google
// em tempo de execucao: em redes que bloqueiam fonts.googleapis.com/
// fonts.gstatic.com (aconteceu em teste), next/font/google so consegue
// cair no fallback com aviso -- self-hosting remove essa dependencia.
import "@fontsource-variable/inter";
import "@fontsource/poppins/600.css";
import "@fontsource/poppins/700.css";
import "@fontsource/poppins/800.css";
import "../styles/tokens.css";
import "../styles/login.css";
import "../styles/dashboard.css";
import "./globals.css";

export const metadata: Metadata = {
  title: "SPI — Sistema para Professoras Independentes",
  description: "Sistema de gestão para professoras particulares independentes.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="pt-BR">
      <body>
        <ThemeProvider>
          <AuthProvider>
            <ToastProvider>{children}</ToastProvider>
          </AuthProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
