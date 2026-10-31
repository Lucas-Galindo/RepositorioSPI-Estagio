# Contrato: respostas de erro da API (preservado pela spec 065)

Este contrato **não muda**; ele descreve o que o filtro `ErrosDeNegocioFilter` precisa reproduzir. A prova é o instantâneo da API (status e corpo) de antes e depois.

## Mapeamento

| Situação | Status | Corpo | Observação |
|---|---|---|---|
| Serviço lança `NaoEncontradoException` e a ação declara `ProducesResponseType(404)` | **404** | texto puro: a mensagem da exceção | igual a `NotFound(e.Message)` |
| Serviço lança `ConflitoException` e a ação declara `ProducesResponseType(409)` | **409** | texto puro: a mensagem da exceção | igual a `Conflict(e.Message)` |
| Qualquer outra exceção (inclusive as duas acima quando a ação **não** declara o status) | **500** | `ProblemDetails` com `title = "Erro inesperado"`, `detail = <mensagem>`, `status = 500` e o `traceId` do MVC | igual ao `Problem(title: "Erro inesperado", detail: e.Message, statusCode: 500)` |
| Validação do corpo (FluentValidation) | 400 | lista de mensagens | **continua no controller** (não é exceção) |
| Credencial inválida (login) | 401 | mensagem | continua no `AuthController`, com o mesmo `LogWarning` |
| Refresh token inválido (refresh e logout) | 401 | mensagem | continua no `AuthController`, com o mesmo `LogWarning` |
| `ConflitoException` em `Auth/RedefinirSenha` | 400 | mensagem | continua no controller (resposta própria) |

## Ações cuja documentação (Swagger) ganha o 409

`Lembretes/Atualizar`, `Turmas/Cadastrar`, `Turmas/Atualizar`: já respondiam 409 (têm o `catch`), mas não declaravam. O filtro depende da declaração, então ela é acrescentada; o comportamento não muda.

## Ações que declaram 404 e devolvem `NotFound()` direto (sem exceção)

`ContasPagar/ObterAnexo`, `Pagamentos/ObterAnexo`, `Professor/ObterComoAdmin`: os serviços devolvem `null`; nenhuma mudança.

## Registro (log)

Hoje o tratamento genérico **não registra** a exceção. O filtro também não registra. (Registrar erros inesperados é uma melhoria possível, mas muda os logs; fica como sugestão no relatório.)
