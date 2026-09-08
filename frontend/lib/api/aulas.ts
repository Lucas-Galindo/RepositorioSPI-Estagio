import { apiGet, apiPost, apiPut, apiDelete } from "./client";

/** Espelha SPI.Application.Aulas.Dtos.AlunoPresencaResponse. */
export interface AlunoPresenca {
  alunoId: number;
  nome: string;
  ra: string;
  /** null enquanto a aula nao foi registrada (Estoria 8). */
  presente: boolean | null;
}

/** Espelha SPI.Application.Aulas.Dtos.AulaResponse. Datas/horas como string ISO (DateOnly/TimeOnly do backend). */
export interface Aula {
  id: number;
  descricao: string | null;
  materiaId: number;
  materiaNome: string;
  professorId: number;
  turmaId: number | null;
  turmaNome: string | null;
  dataInicio: string;
  horaInicio: string;
  horaFim: string;
  status: "Agendada" | "Realizada" | "Cancelada";
  ativo: boolean;
  alunos: AlunoPresenca[];
}

/** Espelha SPI.Application.Aulas.Dtos.AulaRequest. */
export interface AulaRequest {
  descricao?: string | null;
  materiaId: number;
  turmaId?: number | null;
  alunoId?: number | null;
  dataInicio: string;
  horaInicio: string;
  horaFim: string;
}

export function listarAulas(
  accessToken: string,
  filtros?: {
    status?: string;
    turmaId?: number;
    alunoId?: number;
    dataInicio?: string;
    dataFim?: string;
  }
): Promise<Aula[]> {
  return apiGet<Aula[]>("/api/aulas", accessToken, filtros);
}

export function obterAula(id: number, accessToken: string): Promise<Aula> {
  return apiGet<Aula>(`/api/aulas/${id}`, accessToken);
}

export function cadastrarAula(request: AulaRequest, accessToken: string): Promise<Aula> {
  return apiPost<Aula>("/api/aulas", request, accessToken);
}

export function atualizarAula(id: number, request: AulaRequest, accessToken: string): Promise<Aula> {
  return apiPut<Aula>(`/api/aulas/${id}`, request, accessToken);
}

export function excluirAula(id: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/aulas/${id}`, accessToken);
}

export function registrarSessao(
  id: number,
  presencas: Record<number, boolean>,
  accessToken: string
): Promise<Aula> {
  return apiPost<Aula>(`/api/aulas/${id}/registrar-sessao`, { presencas }, accessToken);
}

export function cancelarAula(id: number, accessToken: string): Promise<Aula> {
  return apiPost<Aula>(`/api/aulas/${id}/cancelar`, {}, accessToken);
}
