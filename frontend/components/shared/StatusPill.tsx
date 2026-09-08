const CLASSES: Record<string, string> = {
  Ativo: "status-ativo",
  Inativo: "status-inativo",
  Pago: "status-ativo",
  Pendente: "status-pendente",
  Atrasado: "status-atrasado",
  Cancelado: "status-cancelado",
};

export function StatusPill({ status }: { status: string }) {
  return <span className={`status-pill ${CLASSES[status] ?? "status-inativo"}`}>{status}</span>;
}
