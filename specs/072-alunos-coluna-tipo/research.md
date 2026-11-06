# Research: Alunos — coluna "Tipo" (spec 072)

## R1 — Separar a pílula do valor dentro do mesmo componente

**Decisão**: o `SeloCobranca` (`frontend/components/alunos/SeloCobranca.tsx`) troca o modo `"lista"` por dois modos novos, mantendo `"grupo"`:
- `"tipo"`: pílula (com "· restam N" no pacote), "+N" de outros vínculos e a dica (`title`) com todos os vínculos;
- `"detalhe"`: o valor, " · turma" e " · pacote esgotado" (a linha de baixo de hoje).

**Por quê**: o modo `"lista"` só é usado pela tela de Alunos (as outras duas telas usam `"grupo"`). Dividir o mesmo componente reaproveita a classe de cor (`classeDaPilula`), o rótulo e a dica sem duplicar, e não cria arquivo novo (FR-006). O modo `"grupo"` fica como está (FR-005).

**Alternativas descartadas**: criar um componente novo só para a pílula (arquivo novo e duplicação); montar a pílula na própria página (lógica de cor/rótulo no frontend, fora do componente).

## R2 — A tabela

**Decisão**: em `frontend/app/(app)/alunos/page.tsx`, um `<th>Tipo</th>` depois de "Cobrança" e uma `<td>` com `<SeloCobranca modo="tipo">` ao lado da `<td>` de Cobrança, que passa a usar `modo="detalhe"`. Sem outra mudança na página.

## R3 — Sem selo (`selo` nulo)

**Decisão**: os dois modos mostram "—" (como o componente já faz hoje quando não há selo), para as duas células ficarem iguais.

## R4 — Layout em telas estreitas

**Decisão**: nenhuma regra nova de CSS é esperada: a tabela já rola na horizontal dentro do contêiner. A validação em 360 px confere que a página não ganha rolagem horizontal; só se ocorrer algum problema entra um ajuste mínimo no CSS existente (sem classe nova).

## R5 — Testes

Não há framework de teste de interface. Validação: `tsc`, `eslint`, build e **Chrome com login real e dados simulados**: interceptar `GET /api/alunos` e trocar o `cobranca` de algumas linhas por todos os tipos (Mensalidade, Semestral, Anual, Avulsa, Sem vínculo, Pacote esgotado, aluno com "+N", sem selo), sem gravar nada no banco; capturas em claro, escuro e 360 px.

## R6 — Riscos

| Risco | Mitigação |
|---|---|
| Alterar o modo "grupo" sem querer | leitura do diff do componente (T de verificação) e conferência da página da turma e dos detalhes da aula |
| Tipo repetido nas duas colunas | verificação visual com todos os tipos simulados |
| Lógica nova no frontend | o diff só reorganiza o que já era exibido; nenhuma regra nova (FR-004) |
