import { apiDelete, apiGet, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.Materias.Dtos.MateriaResponse. */
export interface Materia {
  id: number;
  nome: string;
  nivel: string | null;
  descricao: string | null;
  ativo: boolean;
}

/** Espelha SPI.Application.Materias.Dtos.MateriaRequest. */
export interface MateriaRequest {
  nome: string;
  nivel?: string | null;
  descricao?: string | null;
}

export function listarMaterias(
  accessToken: string,
  filtros?: { nome?: string; nivel?: string; ativo?: boolean }
): Promise<Materia[]> {
  return apiGet<Materia[]>("/api/materias", accessToken, filtros);
}

export function obterMateria(id: number, accessToken: string): Promise<Materia> {
  return apiGet<Materia>(`/api/materias/${id}`, accessToken);
}

export function cadastrarMateria(request: MateriaRequest, accessToken: string): Promise<Materia> {
  return apiPost<Materia>("/api/materias", request, accessToken);
}

export function atualizarMateria(id: number, request: MateriaRequest, accessToken: string): Promise<Materia> {
  return apiPut<Materia>(`/api/materias/${id}`, request, accessToken);
}

export function excluirMateria(id: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/materias/${id}`, accessToken);
}
