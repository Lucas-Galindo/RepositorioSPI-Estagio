/** Uma parcela sugerida: vencimento em yyyy-MM-dd e valor em reais. */
export interface ParcelaSugerida {
  vencimento: string;
  valor: number;
}

const FORMATO_DATA = /^(\d{4})-(\d{2})-(\d{2})$/;

function doisDigitos(n: number): string {
  return String(n).padStart(2, "0");
}

/**
 * specs/062: sugere `quantidade` parcelas mensais a partir do valor total e do 1º vencimento. É só conveniência
 * de preenchimento -- o servidor revalida tudo (1 a 24 parcelas, valor > 0, um vencimento por mês) e a professora
 * ajusta data e valor de cada parcela antes de salvar. Cada parcela recebe `total / quantidade` (em centavos) e o
 * resto dos centavos fica na ÚLTIMA parcela; o vencimento de cada mês segue o dia do 1º, sem ultrapassar o fim do
 * mês (31/01 → 28/02 → 31/03).
 */
export function sugerirParcelas(total: number, quantidade: number, primeiroVencimento: string): ParcelaSugerida[] {
  const partes = FORMATO_DATA.exec(primeiroVencimento);
  if (!partes || !Number.isFinite(total) || total <= 0 || !Number.isInteger(quantidade) || quantidade < 1 || quantidade > 120) {
    return [];
  }

  const ano = Number(partes[1]);
  const mes = Number(partes[2]) - 1;
  const dia = Number(partes[3]);

  const centavos = Math.round(total * 100);
  const base = Math.floor(centavos / quantidade);
  const resto = centavos - base * quantidade;

  return Array.from({ length: quantidade }, (_, i) => {
    const indiceDoMes = mes + i;
    const anoDaParcela = ano + Math.floor(indiceDoMes / 12);
    const mesDaParcela = indiceDoMes % 12;
    const ultimoDia = new Date(anoDaParcela, mesDaParcela + 1, 0).getDate();
    const diaDaParcela = Math.min(dia, ultimoDia);

    return {
      vencimento: `${anoDaParcela}-${doisDigitos(mesDaParcela + 1)}-${doisDigitos(diaDaParcela)}`,
      valor: (i === quantidade - 1 ? base + resto : base) / 100,
    };
  });
}
