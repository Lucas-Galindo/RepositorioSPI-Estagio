"use client";

import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { StatusPill } from "@/components/shared/StatusPill";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import type { Aluno } from "@/lib/api/alunos";
import {
  excluirVinculoCobranca,
  listarVinculosCobranca,
  reativarVinculoCobranca,
  type VinculoCobranca,
} from "@/lib/api/vinculosCobranca";
import { currency } from "@/lib/format";
import { VinculoCobrancaFormModal } from "./VinculoCobrancaFormModal";

function detalheDaModalidade(v: VinculoCobranca): string | null {
  if (v.modalidade === "Mensalidade" && v.aulasIncluidas !== null) {
    return `${v.aulasIncluidas} aula(s) incluída(s)`;
  }
  if (v.modalidade === "Pacote" && v.saldoAulas !== null) {
    return `Saldo: ${v.saldoAulas} aula(s)`;
  }
  return null;
}

export function VinculosCobrancaSection({ aluno }: { aluno: Aluno }) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const [vinculos, setVinculos] = useState<VinculoCobranca[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");
  const [mostrarExcluidos, setMostrarExcluidos] = useState(false);
  // undefined = fechado; null = novo vinculo; objeto = edicao.
  const [modal, setModal] = useState<VinculoCobranca | null | undefined>(undefined);
  const [exclusaoPendente, setExclusaoPendente] = useState<VinculoCobranca | null>(null);
  const [excluindo, setExcluindo] = useState(false);
  const [reativandoId, setReativandoId] = useState<number | null>(null);

  const turmasAtivas = aluno.turmas.filter((t) => t.ativo);

  const carregar = useCallback(() => {
    if (!sessao) return;
    // Por padrao so os vinculos ativos; "Mostrar excluidos" traz tambem os inativos (FR-015).
    listarVinculosCobranca(aluno.id, sessao.accessToken, mostrarExcluidos ? undefined : { ativo: true })
      .then((lista) => {
        setVinculos(lista);
        setErro("");
      })
      .catch((excecao) => setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível carregar os vínculos de cobrança."))
      .finally(() => setCarregando(false));
  }, [sessao, aluno.id, mostrarExcluidos]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const handleExcluir = async () => {
    if (!sessao || !exclusaoPendente) return;
    setExcluindo(true);
    try {
      await excluirVinculoCobranca(aluno.id, exclusaoPendente.id, sessao.accessToken);
      mostrarToast("Vínculo de cobrança excluído com sucesso.");
      setExclusaoPendente(null);
      carregar();
    } catch (excecao) {
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível excluir o vínculo de cobrança.");
      setExclusaoPendente(null);
    } finally {
      setExcluindo(false);
    }
  };

  const handleReativar = async (vinculo: VinculoCobranca) => {
    if (!sessao) return;
    setReativandoId(vinculo.id);
    try {
      await reativarVinculoCobranca(aluno.id, vinculo.id, sessao.accessToken);
      mostrarToast("Vínculo de cobrança reativado com sucesso.");
      carregar();
    } catch (excecao) {
      // 409 quando ja existe outro vinculo ativo para a mesma combinacao (FR-014).
      mostrarToast(excecao instanceof ApiError ? excecao.message : "Não foi possível reativar o vínculo de cobrança.");
    } finally {
      setReativandoId(null);
    }
  };

  return (
    <div className="mini-panel">
      <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 12, marginBottom: 15, flexWrap: "wrap" }}>
        <h4 style={{ marginBottom: 0 }}>
          <Icon name="money" size={15} /> Vínculos de Cobrança
        </h4>
        <div style={{ display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap" }}>
          <label className="chk-pill">
            <input type="checkbox" checked={mostrarExcluidos} onChange={(e) => setMostrarExcluidos(e.target.checked)} />
            Mostrar excluídos
          </label>
          <button className="btn btn-primary btn-sm" type="button" onClick={() => setModal(null)}>
            <Icon name="plus" size={13} /> Adicionar vínculo
          </button>
        </div>
      </div>

      {carregando ? (
        <p className="count-text">Carregando...</p>
      ) : erro ? (
        <EmptyState title="Não foi possível carregar" desc={erro} />
      ) : vinculos.length ? (
        vinculos.map((v) => {
          const detalhe = detalheDaModalidade(v);
          return (
            <div className="lesson-item" key={v.id} style={{ cursor: "default", opacity: v.ativo ? 1 : 0.7, flexWrap: "wrap" }}>
              <div className="lesson-time" style={{ width: "auto", padding: "7px 10px" }}>
                {currency(v.valor)}
              </div>
              <div className="lesson-info">
                <div className="subj">{v.turmaNome ?? "Atendimento individual"}</div>
                <div className="who">{detalhe ? `${v.modalidade} · ${detalhe}` : v.modalidade}</div>
              </div>
              {!v.ativo && <StatusPill status="Inativo" />}
              <div style={{ display: "flex", gap: 8 }}>
                {v.ativo ? (
                  <>
                    <button className="btn btn-ghost btn-sm" type="button" onClick={() => setModal(v)}>
                      <Icon name="edit" size={13} /> Editar
                    </button>
                    <button className="btn btn-danger btn-sm" type="button" onClick={() => setExclusaoPendente(v)}>
                      <Icon name="trash" size={13} /> Excluir
                    </button>
                  </>
                ) : (
                  <button className="btn btn-success btn-sm" type="button" onClick={() => handleReativar(v)} disabled={reativandoId === v.id}>
                    <Icon name="check" size={13} /> Reativar
                  </button>
                )}
              </div>
            </div>
          );
        })
      ) : (
        <EmptyState title="Nenhum vínculo de cobrança" desc="Este aluno ainda não tem vínculos de cobrança cadastrados." />
      )}

      {modal !== undefined && (
        <VinculoCobrancaFormModal
          alunoId={aluno.id}
          turmasAtivas={turmasAtivas}
          vinculo={modal ?? undefined}
          onSalvo={() => {
            setModal(undefined);
            carregar();
          }}
          onCancelar={() => setModal(undefined)}
        />
      )}

      {exclusaoPendente && (
        <ConfirmModal
          titulo="Excluir vínculo de cobrança"
          descricao="O vínculo deixa de valer para este aluno, mas o registro é preservado e pode ser reativado."
          onConfirmar={handleExcluir}
          onCancelar={() => setExclusaoPendente(null)}
          confirmando={excluindo}
        />
      )}
    </div>
  );
}
