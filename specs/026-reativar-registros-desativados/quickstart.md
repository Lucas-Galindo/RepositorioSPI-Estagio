# Quickstart: Validar Reativação de Turma, Aluno e Matéria

Guia de validação manual (não há framework de teste automatizado no projeto — ver plan.md
Technical Context). Rodar após a implementação (tasks.md).

## Pré-requisitos

- Backend e frontend rodando localmente, com login como Professora/Admin.
- Pelo menos um registro de cada entidade (Turma, Aluno, Matéria) disponível para desativar e
  reativar nos testes — pode reaproveitar um registro de exemplo já existente.

## Cenário 1 — Reativar uma Turma (US1)

1. Abrir uma Turma Ativa, anotar seus dados (nome, alunos vinculados) e desativá-la pelo botão
   "Excluir" já existente (confirmando a exclusão).
2. Reabrir a tela de detalhe dessa Turma (agora Inativa).
3. **Esperado**: o botão "Excluir" não aparece mais; em seu lugar, aparece um botão "Reativar"
   (FR-003).
4. Clicar em "Reativar".
5. **Esperado**: sem pedir confirmação (FR-005), a tela atualiza imediatamente — o indicador de
   status passa a "Ativo" e o botão "Reativar" some, voltando a aparecer "Excluir" (FR-004).
6. Conferir que os dados anotados no passo 1 (nome, alunos vinculados, histórico de aulas)
   continuam idênticos (FR-002, SC-003).

## Cenário 2 — Reativar um Aluno (US2)

1. Repetir os passos do Cenário 1 para um Aluno: desativar, reabrir detalhe, confirmar botão
   "Reativar" visível, clicar, confirmar atualização imediata sem confirmação extra.
2. Conferir que o histórico de aulas e pagamentos do aluno permanece idêntico ao de antes da
   desativação.

## Cenário 3 — Reativar uma Matéria (US3)

1. Repetir os passos do Cenário 1 para uma Matéria: desativar, reabrir detalhe, confirmar botão
   "Reativar" visível, clicar, confirmar atualização imediata sem confirmação extra.
2. Conferir que vínculos existentes (turmas que usam essa matéria) permanecem inalterados.

## Cenário 4 — Registro Ativo nunca mostra "Reativar"

1. Abrir a tela de detalhe de uma Turma, Aluno e Matéria já Ativos.
2. **Esperado**: nenhum dos três mostra o botão "Reativar" — só "Excluir" (contraponto da
   Decisão 6 do research.md).

## Cenário 5 — Permissão espelha a de desativação

1. Confirmar (via código ou teste com um usuário sem a role `Professor`, se existir ambiente de
   teste para isso) que o endpoint de reativação exige a mesma autorização do endpoint de
   desativação — nenhum nível de acesso novo foi criado (FR-006).

## Cenário 6 — Idempotência (chamada repetida)

1. Reativar um registro já Ativo diretamente pelo endpoint (ex.: via ferramenta de teste de API,
   chamando `PATCH /api/turmas/{id}/reativar` duas vezes seguidas).
2. **Esperado**: a segunda chamada não retorna erro — apenas confirma o estado `Ativo: true`
   (FR-007).

## Cenário 7 — Backend e frontend continuam simétricos entre as 3 entidades

1. Revisar o diff da implementação e confirmar que os 3 endpoints, os 3 métodos de serviço e os
   3 botões de frontend seguem a mesma estrutura (mesmo nome de método, mesma rota, mesmo
   posicionamento de botão) — sem divergência de comportamento entre Turma, Aluno e Matéria além
   do necessário (nome da entidade).
