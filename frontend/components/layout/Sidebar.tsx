"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { initials } from "@/lib/format";

const NAV_ITEMS: { href: string; label: string; icon: React.ComponentProps<typeof Icon>["name"] }[] = [
  { href: "/dashboard", label: "Home", icon: "home" },
  { href: "/alunos", label: "Alunos", icon: "users" },
  { href: "/materias", label: "Matérias", icon: "book" },
  { href: "/turmas", label: "Turmas", icon: "users" },
  { href: "/aulas", label: "Aulas", icon: "cal" },
  { href: "/pagamentos", label: "Pagamentos", icon: "wallet" },
  { href: "/financeiro", label: "Financeiro", icon: "money" },
  { href: "/lembretes", label: "Lembretes", icon: "bell" },
  { href: "/relatorios", label: "Relatórios", icon: "fwd" },
];

// O Admin nao usa nenhuma das telas da professora (todas sao
// [Authorize(Roles="Professor")] no backend) -- so tem a funcao de
// cadastro inicial, entao ganha um menu proprio, bem menor.
const ADMIN_NAV_ITEMS: { href: string; label: string; icon: React.ComponentProps<typeof Icon>["name"] }[] = [
  { href: "/admin/cadastrar-professora", label: "Cadastrar professora", icon: "shield" },
];

export function Sidebar() {
  const pathname = usePathname();
  const { sessao, encerrarSessao } = useAuth();
  const ehAdmin = sessao?.perfil === "Admin";
  const itens = ehAdmin ? ADMIN_NAV_ITEMS : NAV_ITEMS;

  return (
    <aside className="sidebar">
      <div className="brand">
        <div className="brand-mark">SPI</div>
        <div className="brand-text">
          <div className="name">SPI</div>
          <div className="sub">Professoras Independentes</div>
        </div>
      </div>

      <nav className="nav-group">
        <div className="nav-label">Menu</div>
        {itens.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            className={`nav-item${pathname.startsWith(item.href) ? " active" : ""}`}
          >
            <Icon name={item.icon} size={16} />
            {item.label}
          </Link>
        ))}

        {!ehAdmin && (
          <>
            <div className="nav-divider" />
            <Link href="/configuracoes" className={`nav-item${pathname.startsWith("/configuracoes") ? " active" : ""}`}>
              <Icon name="shield" size={16} />
              Configurações
            </Link>
            <Link href="/perfil" className={`nav-item${pathname.startsWith("/perfil") ? " active" : ""}`}>
              <Icon name="users" size={16} />
              Meu Perfil
            </Link>
          </>
        )}
      </nav>

      {ehAdmin ? (
        <button type="button" className="sidebar-footer" style={{ border: "none", cursor: "pointer", width: "100%" }} onClick={encerrarSessao}>
          <div className="avatar-sm">{sessao ? initials(sessao.nome) : "?"}</div>
          <div className="who">
            <div className="n">{sessao?.nome ?? "..."}</div>
            <div className="r">Sair</div>
          </div>
        </button>
      ) : (
        <Link href="/perfil" className="sidebar-footer">
          <div className="avatar-sm">{sessao ? initials(sessao.nome) : "?"}</div>
          <div className="who">
            <div className="n">{sessao?.nome ?? "..."}</div>
            <div className="r">{sessao?.perfil ?? ""}</div>
          </div>
        </Link>
      )}
    </aside>
  );
}
