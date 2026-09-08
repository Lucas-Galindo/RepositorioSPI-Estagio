"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const TABS = [
  { href: "/financeiro", label: "Visão Geral" },
  { href: "/financeiro/contas-a-receber", label: "Contas a Receber" },
  { href: "/financeiro/contas-a-pagar", label: "Contas a Pagar" },
];

export default function FinanceiroLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  return (
    <>
      <div className="row-gap" style={{ marginBottom: 18, gap: 8 }}>
        {TABS.map((tab) => {
          const ativo = tab.href === "/financeiro" ? pathname === "/financeiro" : pathname.startsWith(tab.href);
          return (
            <Link key={tab.href} href={tab.href} className={`btn btn-sm ${ativo ? "btn-primary" : "btn-ghost"}`}>
              {tab.label}
            </Link>
          );
        })}
      </div>
      {children}
    </>
  );
}
