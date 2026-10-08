# Research: Padronizar Botões da Zona de Risco

Decisões técnicas da Phase 0. Não havia nenhum "NEEDS CLARIFICATION" no Technical Context — a investigação de qual classe está em uso hoje e qual é o padrão já foi feita e registrada no `spec.md` (seção "Nota de investigação prévia"). O único ponto que vale registrar aqui é uma decisão de detalhe de implementação que não estava explícita na spec.

## R1. `btn-ghost` sem `btn-sm` — manter o tamanho padrão já usado na Zona de Risco

- **Decision**: trocar `className="btn"` por `className="btn btn-ghost"` nos 2 botões "Cancelar", **sem** adicionar `btn-sm`.
- **Rationale**: o componente `ConfirmModal.tsx` (de onde `btn-ghost` é o padrão de referência) usa `btn btn-ghost btn-sm`, mas isso é porque ele é um modal compacto. Dentro da própria Zona de Risco, nenhum outro botão usa `btn-sm` — "Excluir Professor" e "Reativar Professor" são `btn btn-danger`/`btn btn-primary` sem `-sm`. Adicionar `btn-sm` só aos botões "Cancelar" os tornaria menores que os botões ao lado deles na mesma linha (`display: flex; gap: 10`), criando uma nova inconsistência de tamanho dentro da própria tela — pior do que o problema que esta feature resolve. A spec já havia registrado isso como Assumption; aqui fica formalizado como decisão de plano.
- **Alternatives considered**: usar `btn-sm` para espelhar `ConfirmModal.tsx` exatamente — descartado pelo motivo acima (quebraria a consistência de tamanho local, que é mais visível e mais imediata do que a consistência com um componente usado em contexto de modal).

## R2. Nenhuma outra decisão técnica pendente

- **Decision**: a mudança é uma edição de texto em 2 linhas (`className="btn"` → `className="btn btn-ghost"`), sem nenhuma lógica, estado, ou prop nova.
- **Rationale**: confirmado por leitura direta do arquivo (`CadastrarProfessoraForm.tsx`, linhas ~380 e ~409) — ambos os botões já têm `onClick` corretos e `disabled={processandoExclusao}`; nenhum desses atributos precisa mudar.
