# Relatório: revisão de código e de conceito dos cards em "Revisão de Código/Conceito" (2026-11-04)

**Pedido**: para cada card em revisão, procurar código morto, complexidade desnecessária, redundância e lógica ruim ou errada, **só dentro do escopo do card**, sem quebrar o programa. Decisões do usuário: bug real → corrijo com teste e reporto; o card 065 entra na revisão; commit e push por card.

**Ordem**: 066 (menor), 064, 065, 063. Cada seção abaixo é fechada no commit do respectivo card.

---

## Card 066 — Aba Lembretes dividida em "Lembretes de aula" e "Lembretes de pagamento"

**Escopo revisado**: o que a spec 066 mudou em `frontend/app/(app)/lembretes/page.tsx` (os títulos, as frases e a divisória). Nada além disso.

**Achado e ação**
| # | Tipo | Achado | Ação |
|---|---|---|---|
| 1 | Redundância | O mesmo objeto de estilo inline (`display:flex; alignItems:center; gap:8`) estava repetido nos dois títulos de seção | Extraído para a constante `TITULO_SECAO` (mesmo CSS; nenhuma mudança visual) |

**Sem achado**: nenhum código morto, nenhuma lógica nova (a mudança é só de apresentação), nenhum estado ou handler tocado. A divisória e os textos já usam só estilos e tokens existentes.

**Fora do escopo (visto, não alterado)**: o bloco "Pagamento pendente" (`LembretePagamentoSection`, spec 060) repete o ícone e fica com um título aninhado sob "Lembretes de pagamento"; é da spec 060, não do 066.

**Validação**: `tsc` e `eslint` limpos; o CSS aplicado é o mesmo de antes (a captura visual da spec 066 continua válida).
