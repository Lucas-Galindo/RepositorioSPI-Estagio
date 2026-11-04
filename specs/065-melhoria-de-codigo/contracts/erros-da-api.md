# Contrato: respostas de erro da API (preservado pela spec 065)

Este contrato **não muda**; ele descreve o que o filtro `ErrosDeNegocioFilter` precisa reproduzir. A prova é o instantâneo da API (status e corpo) de antes e depois.

## Mapeamento

| Situação | Status | Corpo | Observação |
|---|---|---|---|
| Serviço lança `NaoEncontradoException` e a ação declara `ProducesResponseType(404)` | **404** | texto puro: a mensagem da exceção | igual a `NotFound(e.Message)` |
| Serviço lança `ConflitoException` e a ação declara `ProducesResponseType(409)` | **409** | texto puro: a mensagem da exceção | igual a `Conflict(e.Message)` |
| Qualquer outra exceção (inclusive as duas acima quando a ação **não** declara o status) | **500** | `ProblemDetails` com `title = "Erro inesperado"`, `status = 500` e o `traceId` do MVC; **`detail`: em Development, a mensagem da exceção; nos demais ambientes, um texto genérico com o código de rastreio** (nota de 2026-11-04: antes devolvia sempre a mensagem da exceção, que pode trazer texto interno) | a exceção completa vai para o **log** do servidor com o mesmo `traceId` |
| Validação do corpo (FluentValidation) | 400 | lista de mensagens | **continua no controller** (não é exceção) |
| Credencial inválida (login) | 401 | mensagem | continua no `AuthController`, com o mesmo `LogWarning` |
| Refresh token inválido (refresh e logout) | 401 | mensagem | continua no `AuthController`, com o mesmo `LogWarning` |
| `ConflitoException` em `Auth/RedefinirSenha` | 400 | mensagem | continua no controller (resposta própria) |

## Ações cuja documentação (Swagger) ganha o 409

`Lembretes/Atualizar`, `Turmas/Cadastrar`, `Turmas/Atualizar`: já respondiam 409 (têm o `catch`), mas não declaravam. O filtro depende da declaração, então ela é acrescentada; o comportamento não muda.

## Ações que declaram 404 e devolvem `NotFound()` direto (sem exceção)

`ContasPagar/ObterAnexo`, `Pagamentos/ObterAnexo`, `Professor/ObterComoAdmin`: os serviços devolvem `null`; nenhuma mudança.

## Registro (log)

Desde 2026-11-04 o 500 **registra a exceção completa** (nível Error, com o `traceId`) no log do servidor; os logs ficam fora do git (`src/SPI.Api/logs/` está no `.gitignore`). A resposta ao cliente nunca traz a mensagem da exceção fora de Development.
