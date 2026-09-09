import { apiPost } from "./client";

/** Espelha SPI.Domain.Enums.PerfilUsuario (serializado como string). */
export type Perfil = "Professor" | "Aluno" | "Admin";

/** Espelha SPI.Application.Auth.Dtos.LoginRequest. */
export interface LoginRequest {
  login: string;
  senha: string;
}

/** Espelha SPI.Application.Auth.Dtos.LoginResponse. */
export interface LoginResponse {
  perfil: Perfil;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiraEm: string;
}

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiPost<LoginResponse>("/api/auth/login", request);
}

/** Troca um refresh token valido por um novo par de tokens (rotacao: o antigo e revogado). */
export function refresh(refreshToken: string): Promise<LoginResponse> {
  return apiPost<LoginResponse>("/api/auth/refresh", { refreshToken });
}

/** Revoga o refresh token da sessao/dispositivo atual. */
export function logout(refreshToken: string): Promise<void> {
  return apiPost<void>("/api/auth/logout", { refreshToken });
}

/** Espelha SPI.Application.Auth.Dtos.EsqueciSenhaRequest. */
export interface EsqueciSenhaRequest {
  email: string;
}

export function esqueciSenha(request: EsqueciSenhaRequest): Promise<{ mensagem: string }> {
  return apiPost<{ mensagem: string }>("/api/auth/esqueci-senha", request);
}

/** Espelha SPI.Application.Auth.Dtos.RedefinirSenhaRequest. */
export interface RedefinirSenhaRequest {
  token: string;
  novaSenha: string;
}

export function redefinirSenha(request: RedefinirSenhaRequest): Promise<void> {
  return apiPost<void>("/api/auth/redefinir-senha", request);
}
