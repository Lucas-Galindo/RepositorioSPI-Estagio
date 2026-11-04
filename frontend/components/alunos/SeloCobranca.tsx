import type { SeloCobranca as SeloCobrancaDados } from "@/lib/api/vinculosCobranca";
import { currency } from "@/lib/format";

interface SeloCobrancaProps {
  /** Resumo calculado pelo servidor; nulo quando nao veio (mostra um traco). */
  selo: SeloCobrancaDados | null | undefined;
  /** "lista" = resumo do aluno (turma, +N e dica); "grupo" = o que vale na turma ou na aula. */
  modo: "lista" | "grupo";
}

const ROTULO: Record<string, string> = { SemVinculo: "Sem vínculo" };

// Cor da pílula por estado -- so classes que ja existem (cores e contraste ja verificados).
function classeDaPilula(selo: SeloCobrancaDados): string {
  if (selo.categoria === "Pacote") {
    if (selo.progresso?.estado === "Esgotado") return "status-cancelado";
    if (selo.progresso?.estado === "Atencao") return "status-pendente";
    return "status-ativo";
  }
  if (selo.categoria === "Mensalidade" || selo.categoria === "Semestral" || selo.categoria === "Anual") return "status-ativo";
  return "status-inativo";
}

/**
 * specs/068: selo da cobranca do aluno, somente exibicao. Nao decide nada: categoria, aulas restantes,
 * "o que vale" e o aviso de pacote esgotado ja vem prontos do servidor (Principio II). Sem acao de clique.
 */
export function SeloCobranca({ selo, modo }: SeloCobrancaProps) {
  if (!selo) return <span className="who">—</span>;

  const restantes = selo.progresso?.aulasRestantes;
  const rotulo = ROTULO[selo.categoria] ?? selo.categoria;
  const dica =
    modo === "lista" && selo.detalhes.length > 1
      ? selo.detalhes
          .map((d) => {
            const resta = d.progresso?.aulasRestantes;
            return [ROTULO[d.categoria] ?? d.categoria, d.turmaNome, currency(d.valor), resta != null ? `restam ${resta}` : null]
              .filter(Boolean)
              .join(" · ");
          })
          .join("\n")
      : undefined;

  return (
    <div title={dica}>
      <span className={`status-pill ${classeDaPilula(selo)}`} style={{ whiteSpace: "nowrap" }}>
        {rotulo}
        {selo.categoria === "Pacote" && restantes != null ? ` · restam ${restantes}` : ""}
      </span>
      {selo.outrosVinculos > 0 && <span className="who"> +{selo.outrosVinculos}</span>}
      <div className="who">
        {currency(selo.valor)}
        {modo === "lista" && selo.turmaNome ? ` · ${selo.turmaNome}` : ""}
        {selo.avisoPacoteEsgotado ? " · pacote esgotado" : ""}
      </div>
    </div>
  );
}
