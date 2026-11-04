"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";
import {
  obterAula,
  excluirAula,
  registrarSessao,
  cancelarAula,
  adicionarParticipante,
  removerParticipante,
  definirLinkAula,
  removerLinkAula,
  type Aula,
} from "@/lib/api/aulas";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { fmtData, fmtHora, initials, avatarColor } from "@/lib/format";
import { ApiError } from "@/lib/api/client";

export default function AulaDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  usePageHeader("Aulas", "Detalhes da aula");
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();

  const [aula, setAula] = useState<Aula | null>(null);
  const [presencas, setPresencas] = useState<Record<number, boolean>>({});
  const [erro, setErro] = useState("");
  const [confirmandoExclusao, setConfirmandoExclusao] = useState(false);
  const [confirmandoCancelamento, setConfirmandoCancelamento] = useState(false);
  const [processando, setProcessando] = useState(false);
  // specs/056 (reposicao): alunos ativos que podem ser colocados nesta aula.
  const [alunosAtivos, setAlunosAtivos] = useState<Aluno[]>([]);
  const [alunoReposicao, setAlunoReposicao] = useState("");
  // specs/067: texto do campo do link (a validacao e do servidor) e se esta trocando um link ja salvo.
  const [linkDigitado, setLinkDigitado] = useState("");
  const [trocandoLink, setTrocandoLink] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    obterAula(Number(id), sessao.accessToken)
      .then((a) => {
        setAula(a);
        setPresencas(Object.fromEntries(a.alunos.map((al) => [al.alunoId, true])));
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Aula não encontrada."));
  }, [sessao, id]);

  // specs/064: quando a chamada esta bloqueada so por horario, o servidor informa quantos segundos
  // faltam para o inicio. Uma nova busca na hora certa libera a chamada sem recarregar a pagina; a
  // tela nao compara horarios nem usa o relogio do navegador -- so espera a duracao informada. O teto
  // de 1 h evita o limite do setTimeout em aulas distantes (a busca simplesmente se repete).
  useEffect(() => {
    if (!sessao || !aula || aula.segundosParaChamada === null) return;
    const espera = Math.min((aula.segundosParaChamada + 1) * 1000, 3_600_000);
    const timer = setTimeout(() => {
      obterAula(aula.id, sessao.accessToken)
        .then(setAula)
        .catch(() => {});
    }, espera);
    return () => clearTimeout(timer);
  }, [sessao, aula]);

  // Reposicao so existe em aula de turma ainda agendada (a regra e do backend;
  // aqui so evita carregar a lista quando o campo nem aparece).
  const aceitaReposicao = aula?.status === "Agendada" && aula.turmaId !== null;
  useEffect(() => {
    if (!sessao || !aceitaReposicao) return;
    listarAlunos(sessao.accessToken, { ativo: true })
      .then(setAlunosAtivos)
      .catch(() => setAlunosAtivos([]));
  }, [sessao, aceitaReposicao]);

  const handleAdicionarReposicao = async () => {
    if (!sessao || !aula || !alunoReposicao) return;
    setProcessando(true);
    try {
      setAula(await adicionarParticipante(aula.id, Number(alunoReposicao), sessao.accessToken));
      setAlunoReposicao("");
      mostrarToast("Aluno adicionado como reposição.");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível adicionar o aluno.");
    } finally {
      setProcessando(false);
    }
  };

  const handleRemoverReposicao = async (alunoId: number) => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      await removerParticipante(aula.id, alunoId, sessao.accessToken);
      setAula({ ...aula, alunos: aula.alunos.filter((al) => al.alunoId !== alunoId) });
      mostrarToast("Aluno retirado da aula.");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível retirar o aluno.");
    } finally {
      setProcessando(false);
    }
  };

  const handleSalvarLink = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      setAula(await definirLinkAula(aula.id, linkDigitado.trim(), sessao.accessToken));
      setLinkDigitado("");
      setTrocandoLink(false);
      mostrarToast("Link da aula salvo.");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível salvar o link.");
    } finally {
      setProcessando(false);
    }
  };

  const handleRemoverLink = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      setAula(await removerLinkAula(aula.id, sessao.accessToken));
      setTrocandoLink(false);
      mostrarToast("Link da aula removido.");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível remover o link.");
    } finally {
      setProcessando(false);
    }
  };

  const handleExcluir = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      await excluirAula(aula.id, sessao.accessToken);
      mostrarToast("Aula excluída com sucesso.");
      router.push("/aulas");
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir a aula.");
      setConfirmandoExclusao(false);
    } finally {
      setProcessando(false);
    }
  };

  const handleCancelar = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      const atualizada = await cancelarAula(aula.id, sessao.accessToken);
      setAula(atualizada);
      mostrarToast("Aula cancelada.");
      setConfirmandoCancelamento(false);
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível cancelar a aula.");
    } finally {
      setProcessando(false);
    }
  };

  const handleRegistrarSessao = async () => {
    if (!sessao || !aula) return;
    setProcessando(true);
    try {
      const atualizada = await registrarSessao(aula.id, presencas, sessao.accessToken);
      setAula(atualizada);
      mostrarToast(
        atualizada.avisos.length > 0
          ? `Sessão registrada. ${atualizada.avisos.join(" ")}`
          : "Sessão registrada. Frequência dos alunos presentes atualizada."
      );
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível registrar a sessão.");
    } finally {
      setProcessando(false);
    }
  };

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!aula) return <p className="count-text">Carregando...</p>;

  // specs/063: a chamada mostra os alunos da turma e, abaixo, os "Outros alunos" (reposicao). Todos
  // usam o mesmo estado de presenca e as mesmas regras de cobranca (do backend).
  const alunosDaTurma = aula.alunos.filter((al) => !al.reposicao);
  const outrosAlunos = aula.alunos.filter((al) => al.reposicao);

  const linhaDoAluno = (al: (typeof aula.alunos)[number]) => (
    <div className="lesson-item" key={al.alunoId} style={{ cursor: "default" }}>
      <div className="mini-avatar" style={{ background: avatarColor(al.alunoId) }}>
        {initials(al.nome)}
      </div>
      <div className="lesson-info">
        <Link href={`/alunos/${al.alunoId}`} className="subj" style={{ textDecoration: "none", color: "inherit" }}>
          {al.nome}
        </Link>
        <div className="who">RA {al.ra}</div>
      </div>

      {al.reposicao && aula.status === "Agendada" && (
        <button
          className="btn btn-ghost btn-sm"
          type="button"
          title="Retirar da aula"
          disabled={processando}
          onClick={() => handleRemoverReposicao(al.alunoId)}
        >
          <Icon name="x" size={13} />
        </button>
      )}

      {aula.chamadaLiberada ? (
        <label className="chk-pill">
          <input
            type="checkbox"
            checked={presencas[al.alunoId] ?? true}
            onChange={(e) => setPresencas({ ...presencas, [al.alunoId]: e.target.checked })}
          />
          Presente
        </label>
      ) : al.presente === null ? (
        <span className="status-pill status-inativo">—</span>
      ) : al.motivoNaoRegistro === "PacoteEsgotado" ? (
        <span className="status-pill status-atrasado" title="Presença não registrada: pacote de aulas esgotado">
          Pacote esgotado
        </span>
      ) : (
        <span className={`status-pill ${al.presente ? "status-ativo" : "status-atrasado"}`}>
          {al.presente ? "Presente" : "Faltou"}
        </span>
      )}
    </div>
  );

  return (
    <>
      <Link href="/aulas" className="breadcrumb">
        <Icon name="back" size={13} /> Aulas
      </Link>

      <div className="entity-hero">
        <div className="avatar-lg" style={{ background: "var(--c-accent-tint)", color: "var(--c-accent-text)" }}>
          <Icon name="cal" size={24} />
        </div>
        <div>
          <h2>{aula.materiaNome}</h2>
          <div className="meta">
            <span>{fmtData(aula.dataInicio)}</span>·
            <span>
              {fmtHora(aula.horaInicio)} — {fmtHora(aula.horaFim)}
            </span>
            ·<StatusBadge status={aula.status} />
          </div>
        </div>
        <div className="actions">
          {aula.status === "Agendada" && (
            <>
              <Link href={`/aulas/${aula.id}/editar`} className="btn btn-ghost btn-sm">
                <Icon name="edit" size={13} /> Editar
              </Link>
              <button className="btn btn-danger btn-sm" type="button" onClick={() => setConfirmandoCancelamento(true)}>
                <Icon name="x" size={13} /> Cancelar aula
              </button>
            </>
          )}
          <button className="btn btn-danger btn-sm" type="button" onClick={() => setConfirmandoExclusao(true)}>
            <Icon name="trash" size={13} /> Excluir
          </button>
        </div>
      </div>

      <div className="grid-2b">
        <div className="mini-panel">
          <h4>
            <Icon name="users" size={15} /> {aula.turmaNome ? `Turma: ${aula.turmaNome}` : "Aula individual"}
          </h4>

          {/* specs/063: quem veio por reposicao fica numa secao propria, abaixo. O agrupamento usa so o
              indicador `reposicao` que a API devolve -- a tela nao decide quem e "outro aluno". */}
          {alunosDaTurma.map(linhaDoAluno)}

          {outrosAlunos.length > 0 && (
            <>
              <div
                className="who"
                style={{ marginTop: 18, marginBottom: 6, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".04em" }}
              >
                Outros alunos
              </div>
              {outrosAlunos.map(linhaDoAluno)}
            </>
          )}

          {aceitaReposicao && (
            <div style={{ display: "flex", gap: 8, marginTop: 14, flexWrap: "wrap" }}>
              <select value={alunoReposicao} onChange={(e) => setAlunoReposicao(e.target.value)} style={{ flex: 1, minWidth: 180 }}>
                <option value="">Aluno para reposição...</option>
                {alunosAtivos
                  .filter((a) => !aula.alunos.some((al) => al.alunoId === a.id))
                  .map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.nome}
                    </option>
                  ))}
              </select>
              <button
                className="btn btn-ghost btn-sm"
                type="button"
                disabled={processando || !alunoReposicao}
                onClick={handleAdicionarReposicao}
              >
                <Icon name="plus" size={13} /> Adicionar (reposição)
              </button>
            </div>
          )}

          {/* specs/064: antes do inicio a chamada fica bloqueada; quem decide e o servidor (segundosParaChamada so vem preenchido nesse caso). */}
          {aula.segundosParaChamada !== null && (
            <p className="hint" style={{ marginTop: 14 }}>
              A chamada ficará disponível a partir das {fmtHora(aula.horaInicio)} de {fmtData(aula.dataInicio)}.
            </p>
          )}

          {aula.chamadaLiberada && aula.alunos.length > 0 && (
            <button
              className="btn btn-primary btn-sm"
              type="button"
              style={{ marginTop: 14 }}
              disabled={processando}
              onClick={handleRegistrarSessao}
            >
              <Icon name="check" size={13} /> Registrar como realizada
            </button>
          )}
        </div>

        {aula.descricao && (
          <div className="mini-panel">
            <h4>Descrição</h4>
            <p style={{ fontSize: 12.5, color: "var(--c-text-muted)" }}>{aula.descricao}</p>
          </div>
        )}

        {/* specs/067: link da reuniao, guardado so em memoria no servidor. Quem decide se a aula aceita
            link e se ele ainda vale e o servidor (aceitaLinkAula / linkAula); a tela so mostra e envia. */}
        {(aula.linkAula || aula.aceitaLinkAula) && (
          <div className="mini-panel">
            <h4>Link da aula</h4>

            {aula.linkAula && !trocandoLink ? (
              <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
                <a
                  href={aula.linkAula}
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{ fontSize: 12.5, overflowWrap: "anywhere", flex: 1, minWidth: 180, color: "var(--c-accent-text)" }}
                >
                  {aula.linkAula}
                </a>
                {aula.aceitaLinkAula && (
                  <>
                    <button className="btn btn-ghost btn-sm" type="button" disabled={processando} onClick={() => setTrocandoLink(true)}>
                      Trocar
                    </button>
                    <button className="btn btn-ghost btn-sm" type="button" disabled={processando} onClick={handleRemoverLink}>
                      Remover
                    </button>
                  </>
                )}
              </div>
            ) : (
              aula.aceitaLinkAula && (
                <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                  <input
                    type="url"
                    value={linkDigitado}
                    onChange={(e) => setLinkDigitado(e.target.value)}
                    placeholder="Cole o link da reunião"
                    aria-label="Link da aula"
                    maxLength={500}
                    style={{ flex: 1, minWidth: 180 }}
                  />
                  <button
                    className="btn btn-primary btn-sm"
                    type="button"
                    disabled={processando || !linkDigitado.trim()}
                    onClick={handleSalvarLink}
                  >
                    Salvar link
                  </button>
                  {trocandoLink && (
                    <button
                      className="btn btn-ghost btn-sm"
                      type="button"
                      disabled={processando}
                      onClick={() => {
                        setTrocandoLink(false);
                        setLinkDigitado("");
                      }}
                    >
                      Cancelar
                    </button>
                  )}
                </div>
              )
            )}

            {aula.aceitaLinkAula && (
              <p className="hint" style={{ marginTop: 10 }}>
                Este link fica guardado só até o fim da aula e se perde se o sistema reiniciar.
              </p>
            )}
          </div>
        )}
      </div>

      {confirmandoCancelamento && (
        <ConfirmModal
          titulo="Cancelar aula"
          descricao="A frequência dos alunos não será alterada. Deseja continuar?"
          onConfirmar={handleCancelar}
          onCancelar={() => setConfirmandoCancelamento(false)}
          confirmando={processando}
        />
      )}

      {confirmandoExclusao && (
        <ConfirmModal
          titulo="Excluir aula"
          descricao="Tem certeza que deseja excluir esta aula?"
          onConfirmar={handleExcluir}
          onCancelar={() => setConfirmandoExclusao(false)}
          confirmando={processando}
        />
      )}
    </>
  );
}
