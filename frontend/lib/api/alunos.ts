import { apiDelete, apiGet, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.Common.Dtos.TurmaResumoResponse. */
export interface TurmaResumo {
  id: number;
  nome: string;
}

/** Espelha SPI.Application.Alunos.Dtos.AlunoResponse. */
export interface Aluno {
  id: number;
  ra: string;
  nome: string;
  cpf: string | null;
  telefoneAluno: string | null;
  telefoneResponsavel: string | null;
  email: string | null;
  emailResponsavel: string | null;
  valorAula: number;
  frequencia: number;
  ativo: boolean;
  turmas: TurmaResumo[];
}

/** Espelha SPI.Application.Alunos.Dtos.CadastrarAlunoRequest. */
export interface CadastrarAlunoRequest {
  nome: string;
  cpf?: string | null;
  telefoneAluno?: string | null;
  telefoneResponsavel?: string | null;
  email?: string | null;
  senha?: string | null;
  emailResponsavel?: string | null;
  valorAula: number;
  turmaId?: number | null;
  ehMenorDeIdade: boolean;
}

/** Espelha SPI.Application.Alunos.Dtos.AtualizarAlunoRequest. */
export interface AtualizarAlunoRequest {
  nome: string;
  cpf?: string | null;
  telefoneAluno?: string | null;
  telefoneResponsavel?: string | null;
  email?: string | null;
  emailResponsavel?: string | null;
  valorAula: number;
  ehMenorDeIdade: boolean;
}

export function listarAlunos(
  accessToken: string,
  filtros?: { nome?: string; ra?: string; turmaId?: number; ativo?: boolean }
): Promise<Aluno[]> {
  return apiGet<Aluno[]>("/api/alunos", accessToken, filtros);
}

export function obterAluno(id: number, accessToken: string): Promise<Aluno> {
  return apiGet<Aluno>(`/api/alunos/${id}`, accessToken);
}

export function cadastrarAluno(request: CadastrarAlunoRequest, accessToken: string): Promise<Aluno> {
  return apiPost<Aluno>("/api/alunos", request, accessToken);
}

export function atualizarAluno(id: number, request: AtualizarAlunoRequest, accessToken: string): Promise<Aluno> {
  return apiPut<Aluno>(`/api/alunos/${id}`, request, accessToken);
}

export function excluirAluno(id: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/alunos/${id}`, accessToken);
}
