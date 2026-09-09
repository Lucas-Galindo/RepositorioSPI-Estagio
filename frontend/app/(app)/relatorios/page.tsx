"use client";

import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { usePageHeader } from "@/lib/usePageHeader";

// Relatorios so lista dashboards (indicadores agregados) -- telas de
// listagem/tabela (Alunos, Materias, Contas a Receber/Pagar) tem sua
// propria aba no menu principal e nao aparecem duplicadas aqui.
const DASHBOARDS: {
  titulo: string;
  desc: string;
  icone: React.ComponentProps<typeof Icon>["name"];
  href: string;
}[] = [
  { titulo: "Dashboard de Alunos", desc: "Distribuição por turma e situação da base de alunos.", icone: "users", href: "/relatorios/alunos" },
  { titulo: "Dashboard de Turmas", desc: "Turmas por matéria, alunos por matéria e aulas realizadas x agendadas.", icone: "users", href: "/relatorios/turmas" },
  { titulo: "Dashboard de Pagamentos", desc: "Recebidos, pendentes e atrasados, com últimos lançamentos.", icone: "wallet", href: "/relatorios/pagamentos" },
  { titulo: "Dashboard Financeiro", desc: "Receitas, despesas, saldo realizado e previsto, por período.", icone: "money", href: "/relatorios/financeiro" },
];

export default function RelatoriosPage() {
  usePageHeader("Relatórios", "Indicadores e dashboards interpretativos do seu negócio");

  return (
    <div className="report-grid">
      {DASHBOARDS.map((d) => (
        <Link href={d.href} className="report-card" key={d.titulo}>
          <div className="report-icon chip-turq">
            <Icon name={d.icone} size={18} />
          </div>
          <h3>{d.titulo}</h3>
          <p>{d.desc}</p>
          <span className="report-link">
            Visualizar <Icon name="fwd" size={12} />
          </span>
        </Link>
      ))}
    </div>
  );
}
