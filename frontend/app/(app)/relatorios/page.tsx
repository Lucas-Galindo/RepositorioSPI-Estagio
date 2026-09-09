"use client";

import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { usePageHeader } from "@/lib/usePageHeader";

/**
 * Relatorios de agenda/historico do aluno/periodo da agenda/alunos/materias
 * dependem de endpoints agregados que ainda nao existem no backend (Sprint 8,
 * Estorias 12-18). Formato esperado quando existirem, ex:
 *
 *   GET /api/relatorios/agenda?periodo=...&status=...&turmaId=...
 *   GET /api/relatorios/historico-aluno/{alunoId}?periodo=...
 *   GET /api/relatorios/periodo-agenda?periodo=...&turmaId=...&materiaId=...
 *   GET /api/relatorios/alunos?turmaId=...&status=...
 *   GET /api/relatorios/materias?nivel=...&periodo=...
 *
 * Contas a Receber/Pagar e o Relatorio Financeiro ja tem dado real hoje
 * (Sprints 5, 6 e 8 da evolucao do Financeiro), entao os cards abaixo
 * apontam direto para essas telas.
 */
const RELATORIOS: {
  titulo: string;
  desc: string;
  icone: React.ComponentProps<typeof Icon>["name"];
  href?: string;
}[] = [
  { titulo: "Relatório de Agenda", desc: "Aulas previstas em um período, com filtros por status, turma e aluno.", icone: "cal" },
  { titulo: "Histórico do Aluno", desc: "Progresso e frequência de um aluno específico ao longo do tempo.", icone: "users" },
  { titulo: "Contas a Receber", desc: "Valores a receber, filtrando por aluno, período, categoria e status.", icone: "wallet", href: "/financeiro/contas-a-receber" },
  { titulo: "Contas a Pagar", desc: "Despesas, filtrando por categoria, favorecido, período e status.", icone: "money", href: "/financeiro/contas-a-pagar" },
  { titulo: "Relatório Financeiro", desc: "Receitas, despesas, saldo realizado e previsto, por período.", icone: "cal", href: "/relatorios/financeiro" },
  { titulo: "Período da Agenda", desc: "Taxa de ocupação e distribuição das aulas ao longo do tempo.", icone: "cal" },
  { titulo: "Relatório de Alunos", desc: "Lista de alunos ativos com informações de contato.", icone: "users", href: "/alunos" },
  { titulo: "Relatório de Matérias", desc: "Matérias cadastradas e quantidade de aulas ministradas.", icone: "book", href: "/materias" },
  { titulo: "Dashboard de Alunos", desc: "Distribuição por turma e situação da base de alunos.", icone: "users", href: "/relatorios/alunos" },
  { titulo: "Dashboard de Turmas", desc: "Turmas por matéria, alunos por matéria e aulas realizadas x agendadas.", icone: "users", href: "/relatorios/turmas" },
  { titulo: "Dashboard de Pagamentos", desc: "Recebidos, pendentes e atrasados, com últimos lançamentos.", icone: "wallet", href: "/relatorios/pagamentos" },
];

export default function RelatoriosPage() {
  usePageHeader("Relatórios", "Consultas e indicadores essenciais do seu negócio");

  return (
    <div className="report-grid">
      {RELATORIOS.map((r) =>
        r.href ? (
          <Link href={r.href} className="report-card" key={r.titulo}>
            <div className="report-icon chip-turq">
              <Icon name={r.icone} size={18} />
            </div>
            <h3>{r.titulo}</h3>
            <p>{r.desc}</p>
            <span className="report-link">
              Abrir <Icon name="fwd" size={12} />
            </span>
          </Link>
        ) : (
          <div className="report-card" key={r.titulo} style={{ cursor: "default", opacity: 0.7 }}>
            <div className="report-icon chip-neutral">
              <Icon name={r.icone} size={18} />
            </div>
            <h3>{r.titulo}</h3>
            <p>{r.desc}</p>
            <span className="proto-tag">Aguardando backend</span>
          </div>
        )
      )}
    </div>
  );
}
