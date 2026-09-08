"use client";

import { Icon } from "@/components/shared/Icon";
import { ThemeToggle } from "@/components/shared/ThemeToggle";
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
        <div className="icon-round">
          <Icon name="bell" size={15} />
          <span className="dot" />
        </div>
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
