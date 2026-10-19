"use client";

import { ThemeToggle } from "@/components/shared/ThemeToggle";
import { NotificacoesSino } from "./NotificacoesSino";
import { useAuth } from "@/contexts/AuthContext";
import { useHeader } from "@/contexts/HeaderContext";
import { initials } from "@/lib/format";

export function Header() {
  const { title, sub } = useHeader();
  const { sessao } = useAuth();

  return (
    <header className="header">
      <div>
        <div className="header-title">{title}</div>
        <div className="header-sub">{sub}</div>
      </div>
      <div className="header-right">
        <ThemeToggle className="icon-round" />
        {/* specs/059: so a professora tem aulas (o Admin nao ve o sino). */}
        {sessao?.perfil === "Professor" && <NotificacoesSino />}
        <div className="header-user">
          <div className="avatar-sm">{sessao ? initials(sessao.nome) : "?"}</div>
          <div>
            <div className="n">{sessao?.nome ?? "..."}</div>
            <div className="r">{sessao?.perfil ?? ""}</div>
          </div>
        </div>
      </div>
    </header>
  );
}
