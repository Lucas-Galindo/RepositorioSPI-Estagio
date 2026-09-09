import { apiDelete, apiGet, apiPost, apiPut } from "./client";

export type CanalLembrete = "Email" | "WhatsApp" | "SMS";
export type DestinatariosLembrete = "Alunos" | "Responsaveis" | "AlunosEResponsaveis";

/** Espelha SPI.Application.Lembretes.Dtos.LembreteResponse. */
export interface Lembrete {
  id: number;
  turmaId: number;
  turmaNome: string;
  status: "Pendente" | "Enviado" | "Falha" | "Cancelado";
  /** ISO datetime. Calculada a partir da próxima aula da turma menos a antecedência. */
  horaProgramada: string;
  destinatarios: DestinatariosLembrete;
  canal: CanalLembrete;
  antecedenciaHora: number;
  ativo: boolean;
}

/** Espelha SPI.Application.Lembretes.Dtos.LembreteRequest. */
export interface LembreteRequest {
  turmaId: number;
  canal: CanalLembrete;
  antecedenciaHora: number;
  destinatarios: DestinatariosLembrete;
}

export function listarLembretes(
  accessToken: string,
  filtros?: { turmaId?: number; status?: string; ativo?: boolean }
): Promise<Lembrete[]> {
  return apiGet<Lembrete[]>("/api/lembretes", accessToken, filtros);
}

export function obterLembrete(id: number, accessToken: string): Promise<Lembrete> {
  return apiGet<Lembrete>(`/api/lembretes/${id}`, accessToken);
}

export function cadastrarLembrete(request: LembreteRequest, accessToken: string): Promise<Lembrete> {
  return apiPost<Lembrete>("/api/lembretes", request, accessToken);
}

export function atualizarLembrete(id: number, request: LembreteRequest, accessToken: string): Promise<Lembrete> {
  return apiPut<Lembrete>(`/api/lembretes/${id}`, request, accessToken);
}

export function excluirLembrete(id: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/lembretes/${id}`, accessToken);
}
