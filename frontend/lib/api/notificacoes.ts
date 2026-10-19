import { apiGet, apiPatch } from "./client";

export type TipoNotificacao = "AulaPendente" | "AulaFutura" | "PagamentoPendente";

/** Espelha SPI.Application.Notificacoes.Dtos.NotificacaoAulaResponse. */
export interface NotificacaoAula {
  id: number;
  data: string;
  horaInicio: string;
  horaFim: string;
  materiaNome: string;
  /** Nome da turma, ou "Atendimento individual · {aluno}". */
  contexto: string;
}

/** Espelha SPI.Application.Notificacoes.Dtos.PendenciaPagamentoResponse (specs/060). */
export interface PendenciaPagamento {
  pagamentoId: number;
  descricao: string;
  valor: number;
  vencimento: string;
  diasAtraso: number;
}

/** Espelha SPI.Application.Notificacoes.Dtos.NotificacaoPagamentoResponse (specs/060). */
export interface NotificacaoPagamento {
  alunoId: number;
  alunoNome: string;
  pendencias: PendenciaPagamento[];
}

/** Espelha SPI.Application.Notificacoes.Dtos.NotificacaoResponse (specs/059/060). */
export interface Notificacao {
  id: number;
  tipo: TipoNotificacao;
  mensagem: string;
  criadaEm: string;
  lida: boolean;
  /** Tipos de aula. */
  aula: NotificacaoAula | null;
  /** Tipo PagamentoPendente. */
  pagamento: NotificacaoPagamento | null;
}

/** Notificacoes ativas da professora (a API sincroniza todos os tipos antes de listar). */
export function listarNotificacoes(accessToken: string): Promise<Notificacao[]> {
  return apiGet<Notificacao[]>("/api/notificacoes", accessToken);
}

export function marcarNotificacaoComoLida(id: number, accessToken: string): Promise<void> {
  return apiPatch<void>(`/api/notificacoes/${id}/lida`, accessToken);
}
