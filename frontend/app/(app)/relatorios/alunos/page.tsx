"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "@/components/shared/Icon";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuth } from "@/contexts/AuthContext";
import { usePageHeader } from "@/lib/usePageHeader";
import { listarAlunos, type Aluno } from "@/lib/api/alunos";
import { listarPagamentos, type Pagamento } from "@/lib/api/pagamentos";
import { currency } from "@/lib/format";

// Dashboard de negocio da base de alunos (Sprint 2 da evolucao de
// Relatorios). O protótipo (spi_prototipo_clay_final.html) tem um painel
// "Distribuição por matéria", mas Aluno nao tem materia principal no schema
// atual (materia so existe em Aula) -- por isso o painel equivalente aqui
// e "Distribuição por turma", usando aluno.turmas (dado real).
export default function DashboardAlunosPage() {
  usePageHeader("Relatórios", "Dashboard de Alunos");
  const { sessao } = useAuth();

  const [alunos, setAlunos] = useState<Aluno[] | null>(null);
  const [pagamentos, setPagamentos] = useState<Pagamento[]>([]);
  const [erro, setErro] = useState("");

  useEffect(() => {
    if (!sessao) return;
    listarAlunos(sessao.accessToken)
      .then(setAlunos)
      .catch(() => setErro("Não foi possível carregar os alunos."));
    listarPagamentos(sessao.accessToken).then(setPagamentos).catch(() => setPagamentos([]));
  }, [sessao]);

  if (erro) return <EmptyState title="Não foi possível carregar" desc={erro} />;
  if (!alunos) return <p className="count-text">Carregando...</p>;

  const total = alunos.length;
  const ativos = alunos.filter((a) => a.ativo).length;
  const inativos = total - ativos;
  const emTurma = alunos.filter((a) => a.turmas.length > 0).length;

  const idsAlunos = new Set(alunos.map((a) => a.id));
  const faturado = pagamentos
    .filter((p) => p.status === "Pago" && idsAlunos.has(p.alunoId))
    .reduce((s, p) => s + p.valorFinal, 0);
  const ticketMedio = ativos ? faturado / ativos : 0;

  const porTurma = new Map<string, number>();
  let semTurma = 0;
  alunos.forEach((a) => {
    if (a.turmas.length === 0) {
      semTurma += 1;
      return;
    }
    a.turmas.forEach((t) => porTurma.set(t.nome, (porTurma.get(t.nome) ?? 0) + 1));
  });
  const distribuicaoTurma = [...porTurma.entries()].sort((a, b) => b[1] - a[1]);
  if (semTurma > 0) distribuicaoTurma.push(["Sem turma", semTurma]);
  const maiorTurma = Math.max(1, ...distribuicaoTurma.map(([, v]) => v));

  return (
    <>
      <Link href="/relatorios" className="breadcrumb">
        <Icon name="back" size={13} /> Relatórios
      </Link>

      <div className="kpi-row kpi-row-3">
        <div className="kpi">
          <div className="label">
            <Icon name="users" size={12} /> Total de alunos
          </div>
          <div className="value turq">{total}</div>
          <div className="delta">
            {ativos} ativos · {inativos} inativos
          </div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="wallet" size={12} /> Ticket médio
          </div>
          <div className="value">{currency(ticketMedio)}</div>
          <div className="delta">por aluno ativo</div>
        </div>
        <div className="kpi">
          <div className="label">
            <Icon name="book" size={12} /> Em turma
          </div>
          <div className="value gold">{emTurma}</div>
          <div className="delta">{total ? Math.round((emTurma / total) * 100) : 0}% da base</div>
        </div>
      </div>

      <div className="grid-2">
        <div className="panel">
          <div className="section-title">Distribuição por turma</div>
          <div className="section-sub">Quantidade de alunos matriculados por turma</div>
          {distribuicaoTurma.length ? (
            <div className="bar-list">
              {distribuicaoTurma.map(([nome, valor]) => (
                <div className="bar-row" key={nome}>
                  <div className="lb">{nome}</div>
                  <div className="bar-track">
                    <div className="bar-fill" style={{ width: `${(valor / maiorTurma) * 100}%` }} />
                  </div>
                  <div className="val">{valor}</div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="Nenhum aluno cadastrado" desc="Cadastre alunos para ver a distribuição por turma." />
          )}
        </div>
        <div className="panel">
          <div className="section-title">Situação e modalidade</div>
          <div className="section-sub">Ativos x inativos e em turma, na base atual</div>
          <div className="bar-list">
            <div className="bar-row">
              <div className="lb">Ativos</div>
              <div className="bar-track">
                <div className="bar-fill" style={{ width: `${total ? (ativos / total) * 100 : 0}%` }} />
              </div>
              <div className="val">{ativos}</div>
            </div>
            <div className="bar-row">
              <div className="lb">Inativos</div>
              <div className="bar-track">
                <div className="bar-fill" style={{ width: `${total ? (inativos / total) * 100 : 0}%`, background: "var(--c-text-muted)" }} />
              </div>
              <div className="val">{inativos}</div>
            </div>
            <div className="bar-row">
              <div className="lb">Em turma</div>
              <div className="bar-track">
                <div
                  className="bar-fill"
                  style={{ width: `${total ? (emTurma / total) * 100 : 0}%`, background: "linear-gradient(90deg,var(--c-accent2),#8f6f3d)" }}
                />
              </div>
              <div className="val">{emTurma}</div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
