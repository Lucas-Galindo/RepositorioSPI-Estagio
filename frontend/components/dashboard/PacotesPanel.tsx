import Link from "next/link";
import { EmptyState } from "@/components/shared/EmptyState";
import { ProgressoPacoteBarra } from "@/components/shared/ProgressoPacoteBarra";
import type { PacotesResumo } from "@/lib/api/dashboard";

/**
 * specs/058: visao de negocio dos pacotes na Home (substitui o antigo painel de
 * "Pacotes em atencao" por saldo). Indicadores, estado e ordem da lista vem
 * prontos do backend (DashboardService.MontarResumoPacotes); aqui so renderiza.
 */
export function PacotesPanel({ resumo }: { resumo: PacotesResumo }) {
  const indicadores = [
    { rotulo: "Pacotes ativos", valor: resumo.ativos },
    { rotulo: "Próximos do fim", valor: resumo.proximosDoFim },
    { rotulo: "Aulas dadas", valor: resumo.aulasDadas },
    { rotulo: "Aulas restantes", valor: resumo.aulasRestantes },
  ];

  return (
    <>
      <div className="pacotes-indicadores">
        {indicadores.map((i) => (
          <div className="card card-small" key={i.rotulo}>
            <div className="big-value">{i.valor}</div>
            <div className="big-label">{i.rotulo}</div>
          </div>
        ))}
      </div>

      <div className="section-title" style={{ marginTop: 18 }}>
        Precisam de renovação
      </div>
      {resumo.renovacao.length === 0 ? (
        <EmptyState
          title="Nenhum pacote precisando de renovação"
          desc="Quando um pacote chegar a 2 aulas restantes ou menos, ele aparece aqui."
        />
      ) : (
        <div className="pacotes-renovacao">
          {resumo.renovacao.map((p) => (
            <Link
              href={`/alunos/${p.alunoId}`}
              key={p.vinculoId}
              className="card card-small"
              style={{ textDecoration: "none", color: "inherit" }}
            >
              <div className="card-eyebrow">{p.alunoNome}</div>
              <ProgressoPacoteBarra progresso={p.progresso} />
            </Link>
          ))}
        </div>
      )}
    </>
  );
}
