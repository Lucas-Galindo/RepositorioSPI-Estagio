"use client";

import { DateInput } from "@/components/shared/DateInput";
import { TimeInput } from "@/components/shared/TimeInput";
import { Icon } from "@/components/shared/Icon";
import { dataIsoLocal, fmtData, fmtHora } from "@/lib/format";
import type { PreviaSerie } from "@/lib/api/aulas";

/** Uma linha do formulario: dia da semana (0 = domingo ... 6 = sabado) e os horarios daquele dia. */
export interface LinhaHorario {
  chave: number;
  diaSemana: string;
  horaInicio: string;
  horaFim: string;
}

export type TerminoSerie = "quantidade" | "dataFinal";

export interface DadosRecorrencia {
  dataInicial: string;
  linhas: LinhaHorario[];
  termino: TerminoSerie;
  quantidade: string;
  dataFinal: string;
}

const DIAS = ["Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado"];

interface RecorrenciaAulasProps {
  dados: DadosRecorrencia;
  onChange: (dados: DadosRecorrencia) => void;
  /** Resultado de "Ver prévia" (calculado pelo servidor); null enquanto nao pedida ou apos qualquer mudanca. */
  previa: PreviaSerie | null;
  carregandoPrevia: boolean;
  onPrevia: () => void;
  erroPrevia: string | null;
}

/**
 * specs/077: campos para repetir a aula toda semana. So coleta e exibe: as datas, o limite de 60 e os conflitos
 * vem do servidor (Principio II) -- aqui nao ha nenhuma conta de calendario.
 */
export function RecorrenciaAulas({ dados, onChange, previa, carregandoPrevia, onPrevia, erroPrevia }: RecorrenciaAulasProps) {
  const atualizarLinha = (chave: number, parcial: Partial<LinhaHorario>) =>
    onChange({ ...dados, linhas: dados.linhas.map((l) => (l.chave === chave ? { ...l, ...parcial } : l)) });

  const adicionarLinha = () =>
    onChange({
      ...dados,
      linhas: [...dados.linhas, { chave: Math.max(0, ...dados.linhas.map((l) => l.chave)) + 1, diaSemana: "", horaInicio: "", horaFim: "" }],
    });

  const removerLinha = (chave: number) => onChange({ ...dados, linhas: dados.linhas.filter((l) => l.chave !== chave) });

  return (
    <div className="recorrencia">
      <div className="field-grid">
        <div className="field">
          <label>
            A partir de<span className="req">*</span>
          </label>
          <DateInput required min={dataIsoLocal()} value={dados.dataInicial} onChange={(v) => onChange({ ...dados, dataInicial: v })} />
        </div>
      </div>

      <div className="field" style={{ marginTop: 15 }}>
        <label>
          Dias da semana e horários<span className="req">*</span>
        </label>
        {dados.linhas.map((linha) => (
          <div key={linha.chave} className="recorrencia-linha">
            <select
              required
              aria-label="Dia da semana"
              value={linha.diaSemana}
              onChange={(e) => atualizarLinha(linha.chave, { diaSemana: e.target.value })}
            >
              <option value="">Dia...</option>
              {DIAS.map((nome, i) => (
                <option key={nome} value={i}>
                  {nome}
                </option>
              ))}
            </select>
            <TimeInput required value={linha.horaInicio} onChange={(v) => atualizarLinha(linha.chave, { horaInicio: v })} title="Horário de início" />
            <TimeInput required value={linha.horaFim} onChange={(v) => atualizarLinha(linha.chave, { horaFim: v })} title="Horário de fim" />
            {dados.linhas.length > 1 && (
              <button type="button" className="btn btn-ghost btn-sm" aria-label="Remover dia" onClick={() => removerLinha(linha.chave)}>
                <Icon name="trash" size={14} />
              </button>
            )}
          </div>
        ))}
        <div>
          <button type="button" className="btn btn-ghost btn-sm" onClick={adicionarLinha}>
            <Icon name="plus" size={14} /> Adicionar dia
          </button>
        </div>
      </div>

      <div className="field" style={{ marginTop: 15 }}>
        <label>Terminar</label>
        <div className="recorrencia-termino">
          <label className="recorrencia-opcao">
            <input type="radio" name="termino" checked={dados.termino === "quantidade"} onChange={() => onChange({ ...dados, termino: "quantidade" })} />
            Após um número de aulas
          </label>
          <label className="recorrencia-opcao">
            <input type="radio" name="termino" checked={dados.termino === "dataFinal"} onChange={() => onChange({ ...dados, termino: "dataFinal" })} />
            Em uma data
          </label>
        </div>
        {dados.termino === "quantidade" ? (
          <input
            required
            type="number"
            min={1}
            max={60}
            inputMode="numeric"
            aria-label="Número de aulas"
            placeholder="Número de aulas (máx. 60)"
            value={dados.quantidade}
            onChange={(e) => onChange({ ...dados, quantidade: e.target.value })}
            style={{ maxWidth: 220 }}
          />
        ) : (
          <DateInput required min={dados.dataInicial || dataIsoLocal()} value={dados.dataFinal} onChange={(v) => onChange({ ...dados, dataFinal: v })} />
        )}
      </div>

      <div style={{ marginTop: 15 }}>
        <button type="button" className="btn btn-ghost btn-sm" disabled={carregandoPrevia} onClick={onPrevia}>
          {carregandoPrevia ? "Calculando..." : "Ver prévia"}
        </button>
      </div>

      {erroPrevia && (
        <div className="err-banner show" style={{ marginTop: 12 }}>
          <Icon name="warn" size={15} />
          <span>{erroPrevia}</span>
        </div>
      )}

      {previa && (
        <div className="recorrencia-previa" aria-live="polite">
          <p className="hint">
            {previa.total} {previa.total === 1 ? "aula" : "aulas"} (máximo {previa.limite})
            {previa.conflitos > 0
              ? ` — ${previa.conflitos} com horário já ocupado. Ajuste os dias, os horários ou o término: nenhuma aula é criada enquanto houver conflito.`
              : ". Nenhum conflito de horário."}
          </p>
          <ul className="recorrencia-lista">
            {previa.aulas.map((o) => (
              <li key={`${o.data}-${o.horaInicio}`}>
                <span>
                  {DIAS[o.diaSemana]}, {fmtData(o.data)} · {fmtHora(o.horaInicio)}–{fmtHora(o.horaFim)}
                </span>
                {o.conflito && <span className="badge badge-cancel">Horário ocupado</span>}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
