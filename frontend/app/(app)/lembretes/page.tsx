"use client";

// import { useEffect, useState } from "react";
// import { useAuth } from "@/contexts/AuthContext";
// import { apiGet } from "@/lib/api/client";
import { Icon } from "@/components/shared/Icon";
import { usePageHeader } from "@/lib/usePageHeader";

/**
 * Backend de Lembretes ainda nao foi implementado (Sprint 7 / Estorias 5 e
 * 19 do ERS: cadastro por turma, canal/antecedencia, recalculo automatico
 * e disparo). Quando existir, o formato esperado e:
 *
 *   GET  /api/lembretes                -> lista de lembretes
 *   POST /api/lembretes                -> cadastrar (TurmaId, Canal, AntecedenciaHora, Destinatarios)
 *   PUT  /api/lembretes/{id}           -> atualizar
 *   DELETE /api/lembretes/{id}         -> excluir
 *
 * const { sessao } = useAuth();
 * const [lembretes, setLembretes] = useState<Lembrete[] | null>(null);
 * useEffect(() => {
 *   if (!sessao) return;
 *   apiGet<Lembrete[]>("/api/lembretes", sessao.accessToken).then(setLembretes);
 * }, [sessao]);
 */
export default function LembretesPage() {
  usePageHeader("Lembretes", "Configure avisos automáticos para os alunos");

  return (
    <div className="placeholder">
      <div className="ic">
        <Icon name="bell" size={22} />
      </div>
      <h3>Módulo em construção</h3>
      <p>
        O cadastro de lembretes por turma (canal, antecedência e destinatários) depende de um backend que ainda não
        foi implementado. A tela já está estruturada — falta só ligar na API quando ela existir.
      </p>
      <span className="proto-tag">
        <Icon name="warn" size={12} /> Aguardando backend (Sprint 7)
      </span>
    </div>
  );
}
