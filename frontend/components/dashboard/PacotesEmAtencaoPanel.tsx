import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import type { PacoteEmAtencao } from "@/lib/api/dashboard";

/**
 * specs/041: painel de Pacotes em Atencao na Home. Recebe a lista ja pronta
 * (estado e ordem calculados no backend, DashboardService.MontarPacotesEmAtencao)
 * e so renderiza -- nunca reordena nem recalcula o estado (Principio II).
 */
export function PacotesEmAtencaoPanel({ pacotes }: { pacotes: PacoteEmAtencao[] }) {
  if (pacotes.length === 0) {
    return (
      <EmptyState
        title="Nenhum pacote precisando de atenção"
        desc="Quando um vínculo de cobrança por pacote chegar a 2 aulas ou menos, ele aparece aqui."
      />
    );
  }

  return (
    <div style={{ overflowX: "auto" }}>
      <div style={{ display: "flex", gap: 18 }}>
        {pacotes.map((p) => (
          <Link
            href={`/alunos/${p.alunoId}`}
            key={p.vinculoId}
            className="card card-small"
            style={{ width: 200, flexShrink: 0, textDecoration: "none", color: "inherit" }}
          >
            <div className="card-eyebrow">{p.alunoNome}</div>
            <div className="big-label" style={{ marginTop: 2 }}>
              {p.contexto}
            </div>
            <div className="big-value">{p.saldoAulas}</div>
            <div className="big-label">{p.saldoAulas === 1 ? "aula restante" : "aulas restantes"}</div>
            <span className={p.estado === "Esgotado" ? "badge badge-cancel" : "badge badge-pending"}>
              <Icon name="warn" size={10} /> {p.estado === "Esgotado" ? "Esgotado" : "Atenção"}
            </span>
          </Link>
        ))}
      </div>
    </div>
  );
}
