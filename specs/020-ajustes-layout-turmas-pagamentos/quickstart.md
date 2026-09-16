# Quickstart: Validando os Ajustes de Layout — Turmas e Pagamentos

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-09-11

Guia manual de validação end-to-end para os três ajustes desta feature. Não há framework de teste de UI automatizado no projeto (sem Jest/Vitest/Playwright em `frontend/package.json`), então a validação é visual, seguindo os Acceptance Scenarios do [spec.md](./spec.md).

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`) com um banco de dados acessível.
- Frontend rodando em modo desenvolvimento: `npm run dev` dentro de `frontend/`.
- Uma sessão autenticada como Professora (ver [Autenticar Usuário](../007-autenticar-usuario/spec.md)).

## Cenário 1 — Estado vazio de Turmas centralizado (US1 / FR-001 / SC-001)

1. Garanta uma conta sem nenhuma turma cadastrada (ou aplique um filtro de busca por nome que não corresponda a nenhuma turma existente).
2. Acesse `/turmas`.
3. **Esperado**: a mensagem "Nenhuma turma encontrada" aparece centralizada horizontalmente em relação a toda a largura da área de conteúdo — não deslocada para a esquerda dentro de uma coluna estreita.
4. Cadastre (ou deixe existir) ao menos uma turma e recarregue `/turmas`.
5. **Esperado**: a grade normal de cartões de turma volta a aparecer normalmente, sem nenhuma mudança de comportamento (regressão de FR-004 / SC-004).
6. Repita o passo 3 em largura de tela estreita (~400px, modo mobile) e confirme que a centralização se mantém (Edge Case do spec).

## Cenário 2 — Espaçamento select de aluno / botão "Vincular" (US2 / FR-002 / SC-002)

1. Acesse o detalhe de uma turma que tenha ao menos um aluno disponível para vincular (`/turmas/{id}`).
2. Localize o campo "Selecione um aluno..." e o botão "Vincular" logo abaixo da lista de alunos já vinculados.
3. **Esperado**: o espaço entre os dois elementos é visivelmente maior do que o estado anterior (referência: `gap: 8` antes do ajuste) — os dois devem parecer alvos de clique claramente distintos.
4. Selecione um aluno no campo e clique em "Vincular".
5. **Esperado**: o vínculo é criado normalmente (o aluno passa a aparecer na lista de vinculados) — nenhuma mudança de comportamento além do espaçamento (FR-004 / SC-004, cenário 2 do US2 no spec).

## Cenário 3 — Seção "Métodos de pagamento" acima da tabela de pagamentos (US3 / FR-003 / SC-003)

1. Acesse `/pagamentos`.
2. Role a página de cima para baixo.
3. **Esperado**: a ordem de leitura passa a ser: barra de filtros → seção "Métodos de pagamento" (título "Métodos de pagamento", subtítulo, tabela de formas cadastradas) → tabela de registros de pagamento.
4. Use o filtro "Forma — todas" na barra de filtros (que já ficava acima da tabela antes do ajuste).
5. **Esperado**: o filtro continua funcionando exatamente como antes, restringindo a tabela de registros de pagamento — nenhuma mudança de comportamento no filtro em si (cenário 2 do US3 no spec).
6. Confirme que os dados exibidos em ambas as tabelas (pagamentos e formas de pagamento) são idênticos aos de antes do ajuste — só a ordem vertical mudou (cenário 3 do US3 / SC-004).

## Critério de conclusão

Todos os 3 cenários acima passam visualmente, sem nenhuma das telas apresentar erro no console do navegador, sem nenhuma chamada de API alterada (verificável na aba Network do navegador — mesmos endpoints, mesmos parâmetros de antes), e sem nenhuma mudança perceptível em dado exibido além da reordenação/espaçamento descritos.
