# Research: Melhoria do Código sem Mudar o Comportamento (spec 065)

## R1 — O que a investigação mediu

| Verificação | Resultado |
|---|---|
| Analisadores do .NET (`IDE0005`, `IDE0051`, `IDE0052`, `IDE0059`, `IDE0060`) | nada encontrado |
| Tipos e métodos C# referenciados uma única vez | 45, **todos** ganchos do framework (ações de controller, `IEntityTypeConfiguration`, extensões): não são código morto |
| Propriedades e constantes referenciadas uma única vez (C# + TS) | nenhuma |
| Exports do frontend usados só no próprio arquivo (em `.ts`/`.tsx`) | 24; 20 são tipos de requisição/resposta da API (inofensivos, ficam); 4 são utilitários: `decodeJwtPayload`, `misturar`, `amostraMaisProxima`, `CONTRASTE_MINIMO`. **Falso positivo achado na revisão**: `misturar` e `CONTRASTE_MINIMO` são importados por `frontend/scripts/verificar-contraste.mjs` (a busca só olhava `.ts`/`.tsx`); ficam. Sobram `decodeJwtPayload` e `amostraMaisProxima` |
| Arquivos de frontend que ninguém importa | nenhum |
| Classes CSS sem uso em `.ts`/`.tsx` | 18: `btn-dark`, `chip-neutral`, `err`, `form-divider`, `icon-btn`, `k`, `page-btn`, `pagination`, `profile-card`, `proto-tag`, `search-box`, `switch`, `table-foot`, `t-info`, `toggle-row`, `toolbar`, `toolbar-actions`, e a de `login.css` |
| Dependências do `package.json` sem import | só tipos e plugins carregados por configuração (falsos positivos) |
| Avisos do build | `NU1510` (`PackageReference` desnecessário); `NU1902`/`NU1903` (pacote com aviso de segurança, fora de escopo) |

**Decisão**: o código morto real é pequeno e fica na US1. O resto do ganho vem de redundância (US2, US3 e US5) e de comentário (US4).

## R2 — Tratamento de erro nos controllers

**Hoje** (87 ações, todas com `catch (Exception)`): 53 capturam `NaoEncontradoException` → `NotFound(e.Message)`; 24 capturam `ConflitoException` → `Conflict(e.Message)` (uma delas, o `Auth/RedefinirSenha`, responde `BadRequest`); 73 devolvem `Problem(title: "Erro inesperado", detail: e.Message, statusCode: 500)` (14 desses em formato de várias linhas, no `AuthController` e no `ProfessorController`). Três ações declaram 404 sem capturar, porque devolvem `NotFound()` direto: `ContasPagar/ObterAnexo`, `Pagamentos/ObterAnexo` e `Professor/ObterComoAdmin`.

**Decisão**: `ErrosDeNegocioFilter : IExceptionFilter`, registrado em `AddControllers`. Para a exceção da ação:

| Exceção | A ação declara no `ProducesResponseType` | Resposta |
|---|---|---|
| `NaoEncontradoException` | 404 | `NotFoundObjectResult(e.Message)` (igual ao `NotFound(e.Message)`) |
| `ConflitoException` | 409 | `ConflictObjectResult(e.Message)` (igual ao `Conflict(e.Message)`) |
| qualquer outra, ou as duas acima **sem** a declaração | — | `ObjectResult(ProblemDetails)` 500 com título "Erro inesperado" e detalhe da mensagem, montado com o `ProblemDetailsFactory` do MVC (o mesmo que `ControllerBase.Problem` usa) |

**Por quê pela declaração**: é o que reproduz o comportamento atual sem inventar atributo novo. As 84 ações que já declaram o que capturam passam a ter o mesmo resultado; as 3 que declaram 404 sem capturar não lançam essas exceções (os serviços devolvem `null`); as 3 ações de 409 sem declaração (`Lembretes/Atualizar`, `Turmas/Cadastrar`, `Turmas/Atualizar`) ganham `[ProducesResponseType(409)]`, que também corrige a documentação da API (Swagger).

**Fica no controller**: `Auth/Login` (401 + registro `LogWarning` do login), `Auth/Refresh` e `Auth/Logout` (401 + registro), e `Auth/RedefinirSenha` (`ConflitoException` → `BadRequest`). Nesses, só sai o `catch (Exception)` final; os `catch` específicos continuam.

**Registro (log)**: o `catch (Exception)` de hoje **não registra** nada, então o filtro também não registra (mudar isso seria mudança de comportamento observável nos logs; fica como sugestão no relatório).

**Alternativas descartadas**: (1) mapear sempre 404 e 409: muda respostas de ações que hoje devolvem 500; (2) `UseExceptionHandler`/middleware: não enxerga os metadados da ação; (3) atributo novo por ação: mais ruído do que a declaração que já existe.

**Transformação dos controllers**: um script remove, em cada ação, o `try { corpo } catch…` quando **todos** os `catch` forem os padronizados, e dedenta o corpo; os `catch` especiais são mantidos. O resultado é conferido por build, pelo instantâneo da API e pela leitura do diff.

## R3 — Instantâneo da API (rede de segurança)

Script PowerShell fora do repositório, com login de teste e dados sintéticos "TESTE-065":
- **Antes** e **depois**, as mesmas chamadas: listagens de todos os recursos, `GET` de id inexistente (404) em cada controller, criações inválidas (400), conflitos conhecidos (409: turma, vínculo, matrícula direta), login inválido (401) e um erro inesperado conhecido (cadastro de aluno com e-mail, que hoje responde 500).
- Normalização: remove `traceId`, datas de criação e ids gerados antes de comparar o corpo; o **status** é comparado exato.
- Resultado esperado: 100% idêntico (SC-002).

## R4 — Capturas de tela (rede de segurança)

Chrome headless (como na validação da spec 063) com login de teste: Home, Alunos (lista e ficha), Turmas (lista e detalhes), Aulas (lista e detalhes), Pagamentos, Contas a pagar e a receber (lista, detalhes e edição), Lembretes, Relatórios, Configurações, nos temas claro e escuro. A comparação é por **hash do PNG** de antes e depois; diferença exige olhar a imagem.

## R5 — Código morto e comentários

- **CSS**: antes de remover cada classe, buscar também o nome em texto montado (`` `status-${...}` ``, `clsx`) e em estilos de módulo. O instantâneo visual confirma.
- **Exports**: `decodeJwtPayload` e os três de `paleta.ts` deixam de ser exportados se continuam usados no arquivo; só se remove a função se não for usada em lugar nenhum.
- **`PackageReference`**: remover do `SPI.Application.csproj` e rodar build e testes; se algo quebrar, volta.
- **Comentário obsoleto**: `AulaServiceRegistrarSessaoPacoteRenovacaoTests.cs` aponta para `DashboardService.MontarPacotesEmAtencao`; o método atual é `MontarResumoPacotes`.
- **Comentários**: lista na pesquisa de métodos longos (R6); estilo do projeto (português sem acento, explica o porquê, cita a spec só quando a regra nasceu dela).

## R6 — Métodos a comentar (mais de 35 linhas e até 2 de comentário)

`PagamentoService.RegistrarAsync`, `AlunoService.CadastrarAsync`, `AlunoRepository.CadastrarViaProcedureAsync`, `FinanceiroService.ObterVisaoGeralAsync`, `RelatorioService.ObterIndicadoresFinanceirosAsync`, `LembreteDispatcherService.ProcessarLembretesVencidosAsync`, `MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync`, `AulaRepository.ListarAsync`, `LembretePagamentoService.TentarEnviarAsync`, `ContaPagarRepository.ListarAsync`, `PagamentoRepository.ListarAsync`, `RelatorioRepository.ListarPagosNoPeriodoAsync`, `TurmaService.CadastrarAsync` e `AtualizarAsync`, `VinculoCobrancaService.AtualizarAsync` e `GarantirRegrasDeCombinacaoAsync`.
**Fora** (zona da spec 064): `AulaService.CadastrarAsync`, `AplicarPresencasAsync`, `RegistrarSessaoAsync`.

## R7 — Lógica repetida a simplificar

- `VinculoCobrancaService`: o trecho "turma de pacote ou turma comum" aparece no cadastro e na atualização → `ValidarTurmaDoVinculoAsync(alunoId, modalidade, turmaId)`.
- `Alunos`: `CadastrarAlunoRequest` × `AtualizarAlunoRequest` e os dois validadores repetem campos e regras (7 e 5 trechos iguais). Só se extrai o que **não mudar nenhuma mensagem nem a ordem de validação**; o resto fica como está.
- `DateTime.Now` (22 pontos): **não** entra aqui; a spec 064 introduz o relógio injetável e deve tratar isso.

## R8 — Telas copiadas (P3, opcional)

Contas a pagar × contas a receber: detalhes (19 trechos iguais) e edição (14). Estratégia: extrair um componente por bloco repetido **só se** as capturas de tela de antes e depois saírem idênticas; senão a tela fica como está e vira card próprio. Relatórios de alunos × turmas (10 trechos): mesma regra.
