"use client";

import { useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { StatusBadge } from "@/components/shared/StatusBadge";
import type { Aula, AlunoPresenca } from "@/lib/api/aulas";
import { fmtData, fmtHora } from "@/lib/format";

interface AulaDetalhesPopupProps {
  /** Aula ja carregada pela tela que abre o pop-up: aqui nao ha nenhuma consulta a API. */
  aula: Aula;
  onFechar: () => void;
}

// Como o aluno aparece na lista: so uma aula realizada tem presenca registrada.
function situacaoDoAluno(aula: Aula, aluno: AlunoPresenca): { texto: string; classe: string } | null {
  if (aula.status !== "Realizada" || aluno.presente === null) return null;
  if (aluno.motivoNaoRegistro === "PacoteEsgotado") return { texto: "Pacote esgotado", classe: "status-atrasado" };
  return aluno.presente ? { texto: "Presente", classe: "status-ativo" } : { texto: "Faltou", classe: "status-atrasado" };
}

/**
 * specs/061: detalhes de uma aula em pop-up, somente leitura. As acoes (presenca,
 * cancelar, editar, reposicao) ficam na pagina completa da aula, que o botao
 * "Abrir aula" abre. Reutilizavel por qualquer tela que ja tenha o objeto Aula.
 */
export function AulaDetalhesPopup({ aula, onFechar }: AulaDetalhesPopupProps) {
  const router = useRouter();
  const botaoAbrir = useRef<HTMLButtonElement>(null);
  // O efeito abaixo roda uma vez so (abrir/fechar). O callback fica numa ref para
  // um onFechar novo a cada render da tela nao refazer o efeito -- o que perderia
  // o elemento de origem do foco, ja que o foco estaria dentro do pop-up.
  const onFecharRef = useRef(onFechar);
  useEffect(() => {
    onFecharRef.current = onFechar;
  });

  useEffect(() => {
    // Guarda quem tinha o foco (a aula clicada) para devolver ao fechar, e leva o
    // foco para dentro do pop-up para o teclado funcionar sem usar o mouse.
    const origem = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    botaoAbrir.current?.focus();

    const aoTeclar = (e: KeyboardEvent) => {
      if (e.key === "Escape") onFecharRef.current();
    };
    document.addEventListener("keydown", aoTeclar);
    return () => {
      document.removeEventListener("keydown", aoTeclar);
      origem?.focus();
    };
  }, []);

  return (
    <div className="modal-overlay" onClick={onFechar}>
      <div
        className="modal aula-detalhes-popup"
        role="dialog"
        aria-modal="true"
        aria-labelledby="aula-detalhes-titulo"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 id="aula-detalhes-titulo">{aula.materiaNome}</h3>
        <div className="aula-detalhes-status">
          <StatusBadge status={aula.status} />
        </div>

        <dl className="notif-detalhes">
          <dt>Data</dt>
          <dd>
            {fmtData(aula.dataInicio)}, {fmtHora(aula.horaInicio)} — {fmtHora(aula.horaFim)}
          </dd>
          <dt>Turma</dt>
          <dd>{aula.turmaNome ?? "Atendimento individual"}</dd>
          {aula.descricao && (
            <>
              <dt>Descrição</dt>
              <dd>{aula.descricao}</dd>
            </>
          )}
        </dl>

        {aula.alunos.length > 0 && (
          <>
            <div className="aula-detalhes-subtitulo">Alunos</div>
            <ul className="aula-detalhes-alunos">
              {aula.alunos.map((aluno) => {
                const situacao = situacaoDoAluno(aula, aluno);
                return (
                  <li key={aluno.alunoId}>
                    <span>
                      {aluno.nome} <span className="aula-detalhes-ra">RA {aluno.ra}</span>
                    </span>
                    {aluno.reposicao && <span className="status-pill status-pendente">Reposição</span>}
                    {situacao && <span className={`status-pill ${situacao.classe}`}>{situacao.texto}</span>}
                  </li>
                );
              })}
            </ul>
          </>
        )}

        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onFechar}>
            Fechar
          </button>
          <button ref={botaoAbrir} type="button" className="btn btn-primary" onClick={() => router.push(`/aulas/${aula.id}`)}>
            Abrir aula
          </button>
        </div>
      </div>
    </div>
  );
}
