const CLASSES: Record<string, string> = {
  Agendada: "badge-confirmed",
  Realizada: "badge-done",
  Cancelada: "badge-cancel",
  Enviado: "badge-confirmed",
  Pendente: "badge-pending",
  Falha: "badge-cancel",
  Cancelado: "badge-done",
};

/** Badge de status de Aula/Lembrete (visual diferente do StatusPill de Aluno/Pagamento). */
export function StatusBadge({ status }: { status: string }) {
  return <span className={`badge ${CLASSES[status] ?? "badge-done"}`}>{status}</span>;
}
