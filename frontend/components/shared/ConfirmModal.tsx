"use client";

import { Icon } from "./Icon";

interface ConfirmModalProps {
  titulo: string;
  descricao: string;
  onConfirmar: () => void;
  onCancelar: () => void;
  confirmando?: boolean;
}

export function ConfirmModal({ titulo, descricao, onConfirmar, onCancelar, confirmando }: ConfirmModalProps) {
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
          <button className="btn btn-danger btn-sm" type="button" onClick={onConfirmar} disabled={confirmando}>
            {confirmando ? "Excluindo..." : "Confirmar exclusão"}
          </button>
        </div>
      </div>
    </div>
  );
}
