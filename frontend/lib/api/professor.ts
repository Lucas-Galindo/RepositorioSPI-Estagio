import { apiGet, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.Professores.Dtos.ProfessorResponse. */
export interface Professor {
  id: number;
  nome: string;
  cpf: string;
  email: string;
  telefone: string | null;
  ativo: boolean;
}

/** Espelha SPI.Application.Professores.Dtos.AtualizarProfessorRequest. */
export interface AtualizarProfessorRequest {
  nome: string;
  email: string;
  telefone?: string | null;
}

/** Espelha SPI.Application.Professores.Dtos.AlterarSenhaProfessorRequest. */
export interface AlterarSenhaProfessorRequest {
  senhaAtual: string;
  novaSenha: string;
}

/** Espelha SPI.Application.Professores.Dtos.CadastroInicialProfessorRequest. */
export interface CadastroInicialProfessorRequest {
  nome: string;
  cpf: string;
  email: string;
  senha: string;
  telefone?: string | null;
}

/** Executado pelo Admin (POST /api/professor/cadastro-inicial exige Authorize Admin). */
export function cadastrarProfessorInicial(
  request: CadastroInicialProfessorRequest,
  accessToken: string
): Promise<Professor> {
  return apiPost<Professor>("/api/professor/cadastro-inicial", request, accessToken);
}

export function obterMeuPerfil(accessToken: string): Promise<Professor> {
  return apiGet<Professor>("/api/professor/me", accessToken);
}

/** Executado pelo Admin. Lanca ApiError com status 404 se nenhuma professora existir ainda. */
export function obterProfessorComoAdmin(accessToken: string): Promise<Professor> {
  return apiGet<Professor>("/api/professor/admin", accessToken);
}

/** Executado pelo Admin, para editar a professora ja cadastrada (Nome/Email/Telefone). */
export function atualizarProfessorComoAdmin(request: AtualizarProfessorRequest, accessToken: string): Promise<Professor> {
  return apiPut<Professor>("/api/professor/admin", request, accessToken);
}

export function atualizarMeuPerfil(request: AtualizarProfessorRequest, accessToken: string): Promise<Professor> {
  return apiPut<Professor>("/api/professor/me", request, accessToken);
}

export function alterarMinhaSenha(request: AlterarSenhaProfessorRequest, accessToken: string): Promise<void> {
  return apiPut<void>("/api/professor/me/senha", request, accessToken);
}
