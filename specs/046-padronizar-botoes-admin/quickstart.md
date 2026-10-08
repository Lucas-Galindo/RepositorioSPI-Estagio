# Quickstart: validar a padronização dos botões da Zona de Risco

Guia de validação ponta a ponta. Decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Backend e frontend rodando; login como Admin, com uma professora já cadastrada e ativa.

## 1. Botão "Cancelar" da etapa de aviso usa a cor padrão secundária (US1, FR-001, SC-001)

| # | Passo | Esperado |
|---|---|---|
| 1 | Na tela de gerenciamento da professora, clicar em "Excluir Professor" | Aviso single-tenant aparece, com os botões "Entendi, enviar código de confirmação" (vermelho/destrutivo) e "Cancelar" |
| 2 | Observar o botão "Cancelar" | Fundo neutro com sombra sutil (mesmo estilo do botão "Cancelar" de uma confirmação de exclusão de Aula, ou do botão "Fechar" da Agenda) — não a aparência padrão cinza do navegador |
| 3 | Clicar em "Cancelar" | Volta para a etapa inicial (botão "Excluir Professor" visível de novo), sem nenhuma requisição de rede (conferir na aba Network) |

## 2. Botão "Cancelar" da etapa de código usa a mesma cor (US1, FR-001, SC-001)

| # | Passo | Esperado |
|---|---|---|
| 1 | Repetir o passo 1 acima e avançar até a etapa de inserir o código (clicar em "Entendi, enviar código de confirmação") | Campo de código e os botões "Confirmar exclusão" (vermelho) e "Cancelar" aparecem |
| 2 | Observar o botão "Cancelar" | Mesma aparência visual do botão "Cancelar" da etapa anterior (§1) |
| 3 | Clicar em "Cancelar" | Volta para a etapa inicial, sem nenhuma requisição de rede, sem consumir o código pendente |

## 3. Não-regressão (FR-003, FR-004, FR-005, SC-002/SC-003/SC-004)

- Os três botões destrutivos ("Excluir Professor", "Entendi, enviar código de confirmação", "Confirmar exclusão") continuam com a mesma aparência vermelha de antes — nenhuma mudança visual neles.
- O botão "Reativar Professor" (quando a professora está inativa) continua com a mesma aparência de antes.
- `git diff --stat` mostra só `frontend/components/admin/CadastrarProfessoraForm.tsx`, com nenhuma linha alterada em nenhum arquivo `.css` do projeto (confirma que nenhuma classe foi criada ou redefinida).
- Em qualquer outra tela que usa `btn-ghost`/`btn-danger`/`btn-primary` (ex.: Aula, Agenda, Alunos), a aparência dos botões permanece idêntica à de antes desta feature.
