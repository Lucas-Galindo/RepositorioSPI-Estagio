export interface AlunoDaChecklist {
  id: number;
  nome: string;
  ra: string;
}

interface PresencaChecklistProps {
  alunos: AlunoDaChecklist[];
  /** AlunoId -> presente. Aluno ausente do mapa conta como presente (padrao marcado). */
  presencas: Record<number, boolean>;
  onChange: (alunoId: number, presente: boolean) => void;
}

/**
 * Lista de presenca usada ao criar uma aula retroativa (specs/055). Mesmo padrao
 * visual (chk-pill, "Presente" marcado por padrao) da tela de Registrar Sessao.
 */
export function PresencaChecklist({ alunos, presencas, onChange }: PresencaChecklistProps) {
  if (alunos.length === 0) {
    return <p className="hint">Nenhum aluno ativo nesta turma.</p>;
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      {alunos.map((aluno) => (
        <div key={aluno.id} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
          <div>
            <div style={{ fontWeight: 600, fontSize: 13 }}>{aluno.nome}</div>
            <div className="hint">RA {aluno.ra}</div>
          </div>
          <label className="chk-pill">
            <input type="checkbox" checked={presencas[aluno.id] ?? true} onChange={(e) => onChange(aluno.id, e.target.checked)} />
            Presente
          </label>
        </div>
      ))}
    </div>
  );
}
