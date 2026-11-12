import type { SeloCobranca } from "./vinculosCobranca";
import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.Common.Dtos.AlunoResumoResponse. */
export interface AlunoResumo {
  id: number;
  ra: string;
  nome: string;
  /** specs/068: a cobranca que vale nesta turma (alunos matriculados); null quando nao calculada. */
  cobranca?: SeloCobranca | null;
  /** specs/078: o servidor diz se a saida com acerto esta disponivel (turma de pacote em andamento e pacote ativo). */
  podeRegistrarSaida?: boolean;
}

/** Espelha SPI.Domain.Enums.TipoTurma (serializado como string). */
export type TipoTurma = "Mensalidade" | "Pacote";

/** Espelha SPI.Application.Turmas.Dtos.TurmaResponse. */
export interface Turma {
  id: number;
  nome: string;
  professorId: number;
  ativo: boolean;
  /** specs/063: Mensalidade (comum) ou Pacote (fechada: ninguem novo entra depois que comeca). */
  tipo: TipoTurma;
  /** Somente turma de Pacote (YYYY-MM-DD). Turma migrada pode estar sem datas. */
  dataInicio: string | null;
  dataFim: string | null;
  /** Calculado no servidor: turma de pacote ja iniciada. A tela so exibe; nao recalcula. */
  fechada: boolean;
  /** specs/075: materia da turma (opcional); o nome vale mesmo se a materia foi inativada depois. */
  materiaId: number | null;
  materiaNome: string | null;
  alunos: AlunoResumo[];
  /** Turma de pacote: quem saiu mas ainda tem o pacote ativo dela (so esses podem voltar pela matricula direta). */
  alunosComPacoteForaDaTurma: AlunoResumo[];
}

/** Espelha SPI.Application.Turmas.Dtos.TurmaRequest. */
export interface TurmaRequest {
  nome: string;
  /** Opcional: ausente no cadastro = Mensalidade; na edicao = mantem o tipo atual (nao muda). */
  tipo?: TipoTurma;
  /** Somente Pacote; obrigatorias no cadastro. A data de inicio nao muda depois de preenchida. */
  dataInicio?: string | null;
  dataFim?: string | null;
  /** specs/075: opcional; null/ausente na edicao REMOVE a materia (o formulario sempre envia o valor atual). */
  materiaId?: number | null;
}

export function listarTurmas(accessToken: string, filtros?: { nome?: string; ativo?: boolean; materiaId?: number; semMateria?: boolean }): Promise<Turma[]> {
  return apiGet<Turma[]>("/api/turmas", accessToken, filtros);
}

export function obterTurma(id: number, accessToken: string): Promise<Turma> {
  return apiGet<Turma>(`/api/turmas/${id}`, accessToken);
}

export function cadastrarTurma(request: TurmaRequest, accessToken: string): Promise<Turma> {
  return apiPost<Turma>("/api/turmas", request, accessToken);
}

export function atualizarTurma(id: number, request: TurmaRequest, accessToken: string): Promise<Turma> {
  return apiPut<Turma>(`/api/turmas/${id}`, request, accessToken);
}

export function excluirTurma(id: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/turmas/${id}`, accessToken);
}

export function reativarTurma(id: number, accessToken: string): Promise<Turma> {
  return apiPatch<Turma>(`/api/turmas/${id}/reativar`, accessToken);
}

export function vincularAluno(turmaId: number, alunoId: number, accessToken: string): Promise<Turma> {
  return apiPost<Turma>(`/api/turmas/${turmaId}/alunos/${alunoId}`, {}, accessToken);
}

export function desvincularAluno(turmaId: number, alunoId: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/turmas/${turmaId}/alunos/${alunoId}`, accessToken);
}

// ---- specs/078: saida do aluno de turma de pacote com acerto proporcional ao consumo ----

/** Espelha SPI.Application.VinculosCobranca.Dtos.SaidaPacoteRequest. */
export interface SaidaPacoteRequest {
  /** Quanto o aluno ja pagou pelo pacote (o pacote nao registra pagamentos). */
  valorPago: number;
  /** Vencimento da conta da diferenca; obrigatorio so quando ha diferenca a receber. */
  vencimento?: string | null;
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.ConfirmarSaidaPacoteRequest: os ecos sao os numeros da previa. */
export interface ConfirmarSaidaPacoteRequest extends SaidaPacoteRequest {
  consumidasEsperadas: number;
  valorDevidoEsperado: number;
  aulasQueSairaoEsperadas: number;
}

/** Espelha SPI.Application.VinculosCobranca.Services.ConsequenciaAcerto (serializado como string). */
export type ConsequenciaAcerto = "GerarConta" | "Fechado" | "ExcedenteNaoDevolvido";

/** Espelha SPI.Application.VinculosCobranca.Dtos.AcertoSaidaPacoteResponse: todo numero vem do servidor. */
export interface AcertoSaidaPacote {
  alunoId: number;
  alunoNome: string;
  turmaId: number;
  turmaNome: string;
  valorPacote: number;
  totalAulas: number;
  totalEstimado: boolean;
  aulasConsumidas: number;
  valorPorAula: number;
  valorDevido: number;
  valorPago: number;
  diferenca: number;
  excedente: number;
  consequencia: ConsequenciaAcerto;
  precisaVencimento: boolean;
  aulasAgendadasQueSairao: number;
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.SaidaPacoteRegistradaResponse. */
export interface SaidaPacoteRegistrada {
  acerto: AcertoSaidaPacote;
  /** A conta a receber da diferenca; null quando nada foi gerado. */
  pagamentoId: number | null;
}

export function previaSaidaPacote(turmaId: number, alunoId: number, request: SaidaPacoteRequest, accessToken: string): Promise<AcertoSaidaPacote> {
  return apiPost<AcertoSaidaPacote>(`/api/turmas/${turmaId}/alunos/${alunoId}/saida/previa`, request, accessToken);
}

export function registrarSaidaPacote(turmaId: number, alunoId: number, request: ConfirmarSaidaPacoteRequest, accessToken: string): Promise<SaidaPacoteRegistrada> {
  return apiPost<SaidaPacoteRegistrada>(`/api/turmas/${turmaId}/alunos/${alunoId}/saida`, request, accessToken);
}
