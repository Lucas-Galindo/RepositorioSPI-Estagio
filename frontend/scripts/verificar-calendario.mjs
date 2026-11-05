// specs/070: confere, sem navegador, a logica pura do calendario proprio (lib/calendario.ts): bissexto,
// dia da semana, grade do mes, navegacao, limites e nomes. Uso (em frontend/): npm run verificar-calendario
// Node 24 executa o .ts direto (remocao de tipos nativa), como verificar-contraste.mjs.
import {
  ANO_MAX,
  ANO_MIN,
  DIAS_ABREVIADOS,
  MESES,
  adicionarDias,
  adicionarMeses,
  anosDaPagina,
  diaDaSemana,
  diasDoMes,
  ehBissexto,
  estaBloqueado,
  lerIso,
  lerMes,
  montarGrade,
  rotuloDoDia,
  tituloDoMes,
} from "../lib/calendario.ts";

let total = 0;
const falhas = [];

function conferir(descricao, obtido, esperado) {
  total++;
  const a = JSON.stringify(obtido);
  const b = JSON.stringify(esperado);
  if (a !== b) falhas.push(`${descricao}: obtido ${a}, esperado ${b}`);
}

// Bissexto
for (const [ano, esperado] of [[1900, false], [2000, true], [2024, true], [2100, false], [2023, false], [2026, false]]) {
  conferir(`ehBissexto(${ano})`, ehBissexto(ano), esperado);
}

// Tamanho dos meses
conferir("fevereiro 2024", diasDoMes(2024, 2), 29);
conferir("fevereiro 2026", diasDoMes(2026, 2), 28);
conferir("fevereiro 1900", diasDoMes(1900, 2), 28);
conferir("fevereiro 2000", diasDoMes(2000, 2), 29);
conferir("abril", diasDoMes(2026, 4), 30);
conferir("dezembro", diasDoMes(2026, 12), 31);

// Dia da semana (0 = domingo)
conferir("2000-01-01 e sabado", diaDaSemana(2000, 1, 1), 6);
conferir("1900-01-01 e segunda", diaDaSemana(1900, 1, 1), 1);
conferir("2026-11-05 e quinta", diaDaSemana(2026, 11, 5), 4);
conferir("2024-02-29 e quinta", diaDaSemana(2024, 2, 29), 4);
conferir("2026-11-01 e domingo", diaDaSemana(2026, 11, 1), 0);
conferir("2100-12-31 e sexta", diaDaSemana(2100, 12, 31), 5);

// Grade: 6 x 7, domingo primeiro, vizinhos com doMes false
for (const [ano, mes] of [[2026, 11], [2024, 2], [2026, 2], [1900, 1], [2100, 12], [2026, 8]]) {
  const grade = montarGrade(ano, mes);
  conferir(`grade ${ano}-${mes} tem 6 semanas`, grade.length, 6);
  conferir(`grade ${ano}-${mes} semanas de 7`, grade.every((s) => s.length === 7), true);
  const doMes = grade.flat().filter((c) => c.doMes).length;
  conferir(`grade ${ano}-${mes} dias do mes`, doMes, diasDoMes(ano, mes));
  conferir(`grade ${ano}-${mes} comeca no domingo`, diaDaSemana(...grade[0][0].iso.split("-").map(Number)), 0);
  const dias = grade.flat().map((c) => c.iso);
  conferir(`grade ${ano}-${mes} dias em ordem estrita`, dias.every((d, i) => i === 0 || d > dias[i - 1]), true);
}
const nov = montarGrade(2026, 11);
conferir("nov/2026 primeira celula", nov[0][0], { iso: "2026-11-01", dia: 1, doMes: true });
conferir("nov/2026 ultima celula", nov[5][6], { iso: "2026-12-12", dia: 12, doMes: false });
const fev = montarGrade(2024, 2);
conferir("fev/2024 primeira celula", fev[0][0], { iso: "2024-01-28", dia: 28, doMes: false });
conferir("fev/2024 tem o dia 29", fev.flat().some((c) => c.iso === "2024-02-29" && c.doMes), true);
conferir("fev/2026 sem o dia 29", montarGrade(2026, 2).flat().some((c) => c.iso === "2026-02-29"), false);

// Navegacao entre meses
conferir("dez + 1", adicionarMeses(2026, 12, 1), { ano: 2027, mes: 1 });
conferir("jan - 1", adicionarMeses(2026, 1, -1), { ano: 2025, mes: 12 });
conferir("nov + 14", adicionarMeses(2026, 11, 14), { ano: 2028, mes: 1 });
conferir("mar - 15", adicionarMeses(2026, 3, -15), { ano: 2024, mes: 12 });

// Navegacao entre dias
conferir("31/12 + 1", adicionarDias("2026-12-31", 1), "2027-01-01");
conferir("01/01 - 1", adicionarDias("2026-01-01", -1), "2025-12-31");
conferir("28/02/2024 + 1", adicionarDias("2024-02-28", 1), "2024-02-29");
conferir("28/02/2026 + 1", adicionarDias("2026-02-28", 1), "2026-03-01");
conferir("01/03/2024 - 1", adicionarDias("2024-03-01", -1), "2024-02-29");
conferir("+7", adicionarDias("2026-11-28", 7), "2026-12-05");
conferir("-7", adicionarDias("2026-11-03", -7), "2026-10-27");
conferir("+0", adicionarDias("2026-11-03", 0), "2026-11-03");

// Bloqueio por minimo (comparacao de texto)
conferir("antes do min", estaBloqueado("2026-11-04", "2026-11-05"), true);
conferir("no min", estaBloqueado("2026-11-05", "2026-11-05"), false);
conferir("depois do min", estaBloqueado("2026-11-06", "2026-11-05"), false);
conferir("sem min", estaBloqueado("1900-01-01"), false);
conferir("min vazio", estaBloqueado("2026-11-04", ""), false);
conferir("depois do max", estaBloqueado("2026-11-06", undefined, "2026-11-05"), true);
conferir("virada de ano no min", estaBloqueado("2025-12-31", "2026-01-01"), true);

// Leitura: formato e limites
conferir("lerIso valido", lerIso("2026-11-05"), { ano: 2026, mes: 11, dia: 5 });
conferir("lerIso 29/02 bissexto", lerIso("2024-02-29"), { ano: 2024, mes: 2, dia: 29 });
conferir("lerIso 29/02 nao bissexto", lerIso("2026-02-29"), null);
conferir("lerIso dia 31 em mes de 30", lerIso("2026-04-31"), null);
conferir("lerIso mes 13", lerIso("2026-13-01"), null);
conferir("lerIso dia 0", lerIso("2026-11-00"), null);
conferir("lerIso vazio", lerIso(""), null);
conferir("lerIso parcial", lerIso("2026-11"), null);
conferir("lerIso ano 1899", lerIso("1899-12-31"), null);
conferir("lerIso ano 1900", lerIso("1900-01-01"), { ano: 1900, mes: 1, dia: 1 });
conferir("lerIso ano 2100", lerIso("2100-12-31"), { ano: 2100, mes: 12, dia: 31 });
conferir("lerIso ano 2101", lerIso("2101-01-01"), null);
conferir("lerIso lixo", lerIso("abc"), null);
conferir("lerMes valido", lerMes("2026-11"), { ano: 2026, mes: 11 });
conferir("lerMes mes 00", lerMes("2026-00"), null);
conferir("lerMes mes 13", lerMes("2026-13"), null);
conferir("lerMes ano 1899", lerMes("1899-12"), null);
conferir("lerMes ano 2101", lerMes("2101-01"), null);
conferir("lerMes com dia", lerMes("2026-11-05"), null);
conferir("lerMes vazio", lerMes(""), null);

// Nomes em portugues
conferir("tituloDoMes", tituloDoMes(2026, 11), "novembro de 2026");
conferir("tituloDoMes marco", tituloDoMes(2024, 3), "março de 2024");
conferir("rotuloDoDia", rotuloDoDia("2026-11-05"), "5 de novembro de 2026");
conferir("rotuloDoDia invalido", rotuloDoDia("2026-02-30"), "2026-02-30");
conferir("12 meses", MESES.length, 12);
conferir("dias abreviados", DIAS_ABREVIADOS.join(""), "DSTQQSS");

// Pagina de anos
conferir("anos de 2026", anosDaPagina(2026).length, 12);
conferir("anos de 2026 contem 2026", anosDaPagina(2026).includes(2026), true);
conferir("anos de 1900 comecam em 1900", anosDaPagina(1900)[0], ANO_MIN);
conferir("anos antes do minimo", anosDaPagina(1800)[0], ANO_MIN);
conferir("anos de 2100 terminam em 2100", anosDaPagina(2100).at(-1), ANO_MAX);
conferir("anos depois do maximo", anosDaPagina(3000).at(-1), ANO_MAX);
conferir("nenhum ano fora da faixa", [1900, 1911, 1912, 2000, 2099, 2100].every((a) => anosDaPagina(a).every((x) => x >= ANO_MIN && x <= ANO_MAX)), true);
conferir("paginas vizinhas nao se repetem", anosDaPagina(1911).includes(1912), false);

if (falhas.length > 0) {
  console.error(`${falhas.length} de ${total} verificações falharam:`);
  for (const f of falhas) console.error(`  - ${f}`);
  process.exit(1);
}
console.log(`${total} verificações da lógica do calendário ok`);
