import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "./client";

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
  cep: string | null;
  rua: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string | null;
  cidade: string | null;
  estado: string | null;
  responsavelMesmoEndereco: boolean;
  responsavelCep: string | null;
  responsavelRua: string | null;
  responsavelNumero: string | null;
  responsavelComplemento: string | null;
  responsavelBairro: string | null;
  responsavelCidade: string | null;
  responsavelEstado: string | null;
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
  cep?: string | null;
  rua?: string | null;
  numero?: string | null;
  complemento?: string | null;
  bairro?: string | null;
  cidade?: string | null;
  estado?: string | null;
  responsavelMesmoEndereco: boolean;
  responsavelCep?: string | null;
  responsavelRua?: string | null;
  responsavelNumero?: string | null;
  responsavelComplemento?: string | null;
  responsavelBairro?: string | null;
  responsavelCidade?: string | null;
  responsavelEstado?: string | null;
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
  cep?: string | null;
  rua?: string | null;
  numero?: string | null;
  complemento?: string | null;
  bairro?: string | null;
  cidade?: string | null;
  estado?: string | null;
  responsavelMesmoEndereco: boolean;
  responsavelCep?: string | null;
  responsavelRua?: string | null;
  responsavelNumero?: string | null;
  responsavelComplemento?: string | null;
  responsavelBairro?: string | null;
  responsavelCidade?: string | null;
  responsavelEstado?: string | null;
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

export function reativarAluno(id: number, accessToken: string): Promise<Aluno> {
  return apiPatch<Aluno>(`/api/alunos/${id}/reativar`, accessToken);
}
