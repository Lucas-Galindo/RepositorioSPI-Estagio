import { apiGet, apiPatch } from "./client";

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

/** Espelha SPI.Application.Notificacoes.Dtos.NotificacaoResponse (specs/059). */
export interface Notificacao {
  id: number;
  tipo: "AulaPendente";
  mensagem: string;
  criadaEm: string;
  lida: boolean;
  aula: NotificacaoAula | null;
}

/** Notificacoes ativas da professora (a API sincroniza as de aula pendente antes de listar). */
export function listarNotificacoes(accessToken: string): Promise<Notificacao[]> {
  return apiGet<Notificacao[]>("/api/notificacoes", accessToken);
}

export function marcarNotificacaoComoLida(id: number, accessToken: string): Promise<void> {
  return apiPatch<void>(`/api/notificacoes/${id}/lida`, accessToken);
}
