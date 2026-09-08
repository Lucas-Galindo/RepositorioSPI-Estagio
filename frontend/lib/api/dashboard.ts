import { apiGet } from "./client";

/** Espelha SPI.Application.Dashboard.Dtos.GargaloCaixaResponse. */
export interface GargaloCaixa {
  diaMaiorEntrada: number | null;
  valorMaiorEntrada: number;
  diaMaiorSaida: number | null;
  valorMaiorSaida: number;
}

/** Espelha SPI.Application.Dashboard.Dtos.IndicadoresFinanceirosResponse. */
export interface IndicadoresFinanceiros {
  taxaInadimplenciaPercentual: number;
  prazoMedioAtrasoDias: number | null;
  margemSegurancaPercentual: number;
  fluxoCaixaOperacional: number;
  indiceCoberturaCustosFixos: number | null;
  gargaloCaixa: GargaloCaixa;
}

/** Espelha SPI.Application.Dashboard.Dtos.FluxoCaixaMensalItem. */
export interface FluxoCaixaMensalItem {
  ano: number;
  mes: number;
  entradas: number;
  saidas: number;
  saldo: number;
}

/** Espelha SPI.Application.Dashboard.Dtos.AulaResumoResponse. */
export interface AulaResumo {
  id: number;
  materiaNome: string;
  turmaNome: string | null;
  dataInicio: string;
  horaInicio: string;
  horaFim: string;
  status: string;
}

/** Espelha SPI.Application.Dashboard.Dtos.DashboardResponse. */
export interface Dashboard {
  totalAulasAgendadasNoPeriodo: number;
  totalAlunosAtivos: number;
  alunosAtendidosNoPeriodo: number;
  totalTurmasAtivas: number;
  valorPendenteRecebimento: number;
  valorFaturadoNoPeriodo: number;
  proximasAulasHoje: AulaResumo[];
  lembretesPendentes: number;
  indicadores: IndicadoresFinanceiros;
  fluxoCaixaMensal: FluxoCaixaMensalItem[];
}

export function obterDashboard(
  accessToken: string,
  filtros?: { periodoInicio?: string; periodoFim?: string }
): Promise<Dashboard> {
  return apiGet<Dashboard>("/api/dashboard", accessToken, filtros);
}
