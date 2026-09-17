"use client";

import { Icon } from "./Icon";

interface ConfirmModalProps {
  titulo: string;
  descricao: string;
  onConfirmar: () => void;
  onCancelar: () => void;
  confirmando?: boolean;
  /** Rotulo do botao de confirmar (padrao: "Confirmar exclusão"). */
  confirmLabel?: string;
  /** Rotulo exibido enquanto `confirmando` e true (padrao: "Excluindo..."). */
  confirmandoLabel?: string;
  /** Estilo do botao de confirmar (padrao: "btn-danger", para acoes destrutivas). */
  confirmVariant?: "btn-danger" | "btn-primary";
}

export function ConfirmModal({
  titulo,
  descricao,
  onConfirmar,
  onCancelar,
  confirmando,
  confirmLabel = "Confirmar exclusão",
  confirmandoLabel = "Excluindo...",
  confirmVariant = "btn-danger",
}: ConfirmModalProps) {
  return (
    <div className="modal-overlay">
      <div className="modal">
        <div className="ic">
          <Icon name="warn" size={18} />
        </div>
        <h3>{titulo}</h3>
        <p>{descricao}</p>
        <div className="modal-actions">
          <button className="btn btn-ghost btn-sm" type="button" onClick={onCancelar}>
            Cancelar
          </button>
          <button className={`btn ${confirmVariant} btn-sm`} type="button" onClick={onConfirmar} disabled={confirmando}>
            {confirmando ? confirmandoLabel : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
