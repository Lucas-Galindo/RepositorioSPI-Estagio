"use client";

import { useRef, useState } from "react";
import { Icon } from "@/components/shared/Icon";
import { ConfirmModal } from "@/components/shared/ConfirmModal";
import { ApiError } from "@/lib/api/client";
import type { Anexo } from "@/lib/api/contasPagar";

interface AnexoComprovanteProps {
  anexo: Anexo | null;
  onAnexar: (arquivo: File) => Promise<void>;
  onVisualizar: () => Promise<{ blob: Blob; nomeArquivo: string }>;
}

const ACEITA = ".jpg,.jpeg,.png,.pdf";

function fmtTamanho(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function AnexoComprovante({ anexo, onAnexar, onVisualizar }: AnexoComprovanteProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState("");
  const [confirmandoSubstituicao, setConfirmandoSubstituicao] = useState(false);

  const handleSelecionarArquivo = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const arquivo = event.target.files?.[0];
    event.target.value = "";
    if (!arquivo) return;

    setErro("");
    setEnviando(true);
    try {
      await onAnexar(arquivo);
    } catch (excecao) {
      setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível anexar o arquivo.");
    } finally {
      setEnviando(false);
    }
  };

  const handleVisualizar = async () => {
    setErro("");
    try {
      const { blob, nomeArquivo } = await onVisualizar();
      const url = URL.createObjectURL(blob);
      window.open(url, "_blank", "noopener");
      void nomeArquivo;
    } catch (excecao) {
      setErro(excecao instanceof ApiError ? excecao.message : "Não foi possível abrir o anexo.");
    }
  };

  return (
    <div className="lesson-item" style={{ cursor: "default", paddingLeft: 0, flexDirection: "column", alignItems: "stretch", gap: 10 }}>
      <div className="lesson-info">
        <div className="subj">Comprovante</div>
        {anexo ? (
          <div className="who" style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <Icon name="file" size={14} />
            <span>{anexo.nomeOriginal}</span>
            <span className="hint">({fmtTamanho(anexo.tamanhoBytes)})</span>
          </div>
        ) : (
          <div className="who">Nenhum comprovante anexado.</div>
        )}
      </div>

      <input ref={inputRef} type="file" accept={ACEITA} style={{ display: "none" }} onChange={handleSelecionarArquivo} />

      <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
        {!anexo && (
          <button type="button" className="btn btn-sm btn-ghost" disabled={enviando} onClick={() => inputRef.current?.click()}>
            <Icon name="paperclip" size={13} /> {enviando ? "Enviando..." : "Anexar arquivo"}
          </button>
        )}
        {anexo && (
          <>
            <button type="button" className="btn btn-sm btn-ghost" onClick={handleVisualizar}>
              <Icon name="file" size={13} /> Visualizar/baixar
            </button>
            <button
              type="button"
              className="btn btn-sm btn-ghost"
              disabled={enviando}
              onClick={() => setConfirmandoSubstituicao(true)}
            >
              <Icon name="edit" size={13} /> {enviando ? "Enviando..." : "Substituir"}
            </button>
          </>
        )}
      </div>

      {erro && (
        <div className="err-banner show">
          <Icon name="warn" size={15} />
          <span>{erro}</span>
        </div>
      )}

      {confirmandoSubstituicao && (
        <ConfirmModal
          titulo="Substituir comprovante"
          descricao="Isso vai substituir o anexo atual, que não poderá ser recuperado depois."
          confirmLabel="Substituir"
          confirmandoLabel="Substituindo..."
          confirmVariant="btn-primary"
          onCancelar={() => setConfirmandoSubstituicao(false)}
          onConfirmar={() => {
            setConfirmandoSubstituicao(false);
            inputRef.current?.click();
          }}
        />
      )}
    </div>
  );
}
