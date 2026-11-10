import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "./client";

/** Espelha SPI.Domain.Enums.ModalidadeCobranca (serializado como string). */
export type ModalidadeCobranca = "Avulsa" | "Mensalidade" | "Pacote" | "Semestral" | "Anual";

/** Semestral e Anual: planos com periodo e parcelas, que nao se editam nem se reativam (specs/062). */
export const ehPlano = (modalidade: string): boolean => modalidade === "Semestral" || modalidade === "Anual";

/** Espelha SPI.Application.VinculosCobranca.Dtos.ParcelaResponse (specs/062). status: Pendente, Pago, Cancelado ou Atrasado. */
export interface ParcelaPlano {
  pagamentoId: number;
  descricao: string | null;
  vencimento: string;
  valor: number;
  status: string;
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.ParcelaRequest (specs/062). */
export interface ParcelaRequest {
  vencimento: string;
  valor: number;
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.ProgressoPacoteResponse (specs/058). */
export interface ProgressoPacote {
  /** Nulo sem total informado e sem saldo para estimar. */
  total: number | null;
  totalEstimado: boolean;
  aulasDadas: number;
  aulasRestantes: number | null;
  estado: "EmDia" | "Atencao" | "Esgotado" | "SemTotal";
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.DetalheCobrancaResponse (specs/068). */
export interface DetalheCobranca {
  categoria: string;
  turmaNome: string | null;
  valor: number;
  progresso: ProgressoPacote | null;
}

/**
 * Espelha SPI.Application.VinculosCobranca.Dtos.SeloCobrancaResponse (specs/068): resumo de cobranca calculado
 * no servidor. categoria: "Mensalidade" | "Pacote" | "Avulsa" | "SemVinculo".
 */
export interface SeloCobranca {
  categoria: string;
  valor: number;
  /** So no resumo da lista. */
  turmaNome: string | null;
  /** So na categoria Pacote. */
  progresso: ProgressoPacote | null;
  /** true na Avulsa que so vale porque o pacote do grupo esgotou. */
  avisoPacoteEsgotado: boolean;
  /** So no resumo da lista. */
  outrosVinculos: number;
  /** So no resumo da lista: todos os vinculos ativos (dica). */
  detalhes: DetalheCobranca[];
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.AulaDoPacoteResponse (specs/058). */
export interface AulaDoPacote {
  aulaId: number;
  data: string;
  horaInicio: string;
  horaFim: string;
  materiaNome: string;
  contexto: string;
  status: string;
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.VinculoCobrancaResponse. */
export interface VinculoCobranca {
  id: number;
  alunoId: number;
  turmaId: number | null;
  /** Nulo quando turmaId e nulo (atendimento individual). */
  turmaNome: string | null;
  modalidade: ModalidadeCobranca;
  valor: number;
  aulasIncluidas: number | null;
  saldoAulas: number | null;
  totalAulasPacote: number | null;
  /** specs/058: so Pacote; calculado no backend (ProgressoPacote). */
  progresso: ProgressoPacote | null;
  /** specs/062: so Semestral/Anual (null nas demais); o servidor calcula o fim e o status de cada parcela. */
  dataInicio: string | null;
  dataFim: string | null;
  parcelas: ParcelaPlano[] | null;
  ativo: boolean;
  /** specs/078: quando a saida do aluno foi registrada com acerto (so pacote inativo); esse pacote nao e reativado. */
  saidaRegistradaEm: string | null;
  /** specs/056: combinacao herdada hoje proibida (so na listagem); null = sem conflito. */
  conflito: string | null;
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.VinculoCobrancaRequest. */
export interface VinculoCobrancaRequest {
  /** Avulsa: turma ou nulo (atendimento individual). Pacote (specs/063): turma de pacote ou nulo (pacote de aulas individuais). Mensalidade: sempre nulo. */
  turmaId: number | null;
  modalidade: ModalidadeCobranca;
  valor: number;
  /** Somente com Mensalidade. */
  aulasIncluidas: number | null;
  /** Somente com Pacote. */
  saldoAulas: number | null;
  /** Somente com Pacote; no cadastro sem saldo, o saldo comeca igual ao total. */
  totalAulasPacote: number | null;
  /** specs/062: so Semestral/Anual (obrigatorios nelas, omitidos nas outras). `valor` e o total do periodo. */
  dataInicio?: string | null;
  parcelas?: ParcelaRequest[];
}

const base = (alunoId: number) => `/api/alunos/${alunoId}/vinculos-cobranca`;

export function listarVinculosCobranca(
  alunoId: number,
  accessToken: string,
  filtros?: { ativo?: boolean }
): Promise<VinculoCobranca[]> {
  return apiGet<VinculoCobranca[]>(base(alunoId), accessToken, filtros);
}

export function cadastrarVinculoCobranca(
  alunoId: number,
  request: VinculoCobrancaRequest,
  accessToken: string
): Promise<VinculoCobranca> {
  return apiPost<VinculoCobranca>(base(alunoId), request, accessToken);
}

export function atualizarVinculoCobranca(
  alunoId: number,
  id: number,
  request: VinculoCobrancaRequest,
  accessToken: string
): Promise<VinculoCobranca> {
  return apiPut<VinculoCobranca>(`${base(alunoId)}/${id}`, request, accessToken);
}

export function excluirVinculoCobranca(alunoId: number, id: number, accessToken: string): Promise<void> {
  return apiDelete(`${base(alunoId)}/${id}`, accessToken);
}

export function reativarVinculoCobranca(alunoId: number, id: number, accessToken: string): Promise<VinculoCobranca> {
  return apiPatch<VinculoCobranca>(`${base(alunoId)}/${id}/reativar`, accessToken);
}

/** specs/058: aulas que contaram como dadas no pacote, mais recentes primeiro. */
export function listarAulasDoPacote(alunoId: number, id: number, accessToken: string): Promise<AulaDoPacote[]> {
  return apiGet<AulaDoPacote[]>(`${base(alunoId)}/${id}/aulas`, accessToken);
}
