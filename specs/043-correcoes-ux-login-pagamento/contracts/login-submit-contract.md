# Contrato: Submissão do formulário de login (Enter e duplo-envio)

Sem mudança de endpoint — `POST /api/auth/login` (ou equivalente em `frontend/lib/api/auth.ts`) continua exatamente igual. O contrato aqui é de **comportamento de interface**.

## Comportamento esperado

| Gatilho | Campos preenchidos | Resultado |
|---|---|---|
| Enter no campo e-mail/RA | Ambos preenchidos | Formulário submete, idêntico a clicar em "Entrar" (FR-010) |
| Enter no campo senha | Ambos preenchidos | Idem |
| Enter em qualquer campo | Um ou ambos vazios | Mensagem de validação "Preencha e-mail/RA e senha para continuar." — igual ao clique (comportamento já existente) |
| Enter repetido durante um envio em andamento | — | Segundo envio **MUST NOT** ocorrer (FR-011) — `handleSubmit` retorna cedo se `carregando === true` |
| Clique no botão durante um envio em andamento | — | Já coberto hoje pelo `disabled` do botão; a guarda nova em `handleSubmit` cobre ambos os casos com uma única checagem |

## Não-regressão

- Mensagens de erro (credencial inválida, erro de rede) continuam exatamente as mesmas, vindas do mesmo `try/catch` já existente.
- Redirecionamento pós-login (Admin → `/admin/cadastrar-professora`, demais → `/dashboard`) inalterado.
