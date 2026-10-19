import type { ProgressoPacote } from "@/lib/api/vinculosCobranca";

const ROTULO_ESTADO: Record<ProgressoPacote["estado"], { texto: string; classe: string } | null> = {
  EmDia: null,
  Atencao: { texto: "Atenção", classe: "status-pendente" },
  Esgotado: { texto: "Esgotado", classe: "status-cancelado" },
  SemTotal: { texto: "Total não informado", classe: "status-inativo" },
};

/**
 * specs/058: barra "N de M aulas" de um pacote. So exibe o progresso calculado
 * no backend (ProgressoPacote) -- a unica conta aqui e a largura da barra.
 * Usada na ficha do aluno e na Home, para as duas mostrarem o mesmo.
 */
export function ProgressoPacoteBarra({ progresso }: { progresso: ProgressoPacote }) {
  const { total, totalEstimado, aulasDadas, aulasRestantes, estado } = progresso;
  const rotulo = ROTULO_ESTADO[estado];
  const percentual = total ? Math.min(100, (aulasDadas / total) * 100) : 0;

  return (
    <div className="pacote-progresso">
      <div className="pacote-progresso-linha">
        <span>
          {total !== null ? (
            <>
              <b>{aulasDadas}</b> de {total} aulas{totalEstimado && <span title="Total estimado: aulas dadas + saldo"> (estimado)</span>}
            </>
          ) : (
            <>
              <b>{aulasDadas}</b> {aulasDadas === 1 ? "aula dada" : "aulas dadas"}
            </>
          )}
        </span>
        {aulasRestantes !== null && <span className="pacote-restantes">{aulasRestantes === 1 ? "resta 1" : `restam ${aulasRestantes}`}</span>}
        {rotulo && <span className={`status-pill ${rotulo.classe}`}>{rotulo.texto}</span>}
      </div>
      {total !== null && (
        <div className="bar-track" role="progressbar" aria-valuenow={aulasDadas} aria-valuemin={0} aria-valuemax={total}>
          <div className="bar-fill" style={{ width: `${percentual}%` }} />
        </div>
      )}
    </div>
  );
}
