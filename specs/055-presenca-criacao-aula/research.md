# Research: Registrar Presença na Criação da Aula

Nenhum item do Technical Context ficou marcado como `NEEDS CLARIFICATION` — o projeto já tem stack, convenções e padrões de teste fixos (ver `.specify/memory/constitution.md`), e todas as ambiguidades de regra de negócio já foram resolvidas com o usuário antes da escrita do `spec.md`. Este documento registra, em vez disso, as decisões de design levantadas ao ler o código atual para planejar a implementação.

## R1 — Reaproveitar `RegistrarSessaoAsync` em vez de duplicar a lógica de presença

**Decision**: Extrair o corpo de `AulaService.RegistrarSessaoAsync` (validação de que toda presença foi informada, bloqueio por pacote esgotado, incremento de `Aluno.Frequencia`, troca de `Status` para "Realizada", recálculo de lembretes da turma, geração de cobrança/decremento de saldo via `GerarContasAReceberAsync`) para um novo método privado `AplicarPresencasAsync(Aula aula, Dictionary<int,bool> presencas, CancellationToken)`, retornando a lista de avisos. `RegistrarSessaoAsync` passa a só validar o status atual (`"Agendada"`) e delegar para esse método; `CadastrarAsync` chama o mesmo método quando a aula é retroativa.

**Rationale**: É exatamente o que FR-005 e o Princípio II da constituição exigem — nenhuma duplicação de regra de negócio financeira entre os dois fluxos. Qualquer mudança futura nessa regra (ex.: uma nova modalidade de vínculo) só precisa ser feita em um lugar.

**Alternatives considered**: Duplicar a lógica dentro de `CadastrarAsync` — rejeitado por violar diretamente o Princípio II e multiplicar o risco de divergência entre os dois caminhos (o mesmo tipo de bug que o Princípio III já documenta ter acontecido antes no projeto, para canais de Lembrete).

## R2 — Comparação de data "retroativa" usa só a data, não o horário

**Decision**: `var retroativa = request.DataInicio <= DateOnly.FromDateTime(DateTime.Now);` — compara só a parte de data, ignorando `HoraInicio`.

**Rationale**: Já confirmado como a interpretação correta na seção Assumptions do `spec.md`, decidido com o usuário antes desta spec.

**Alternatives considered**: Comparar `DataInicio + HoraInicio` como `DateTime` contra `DateTime.Now` — rejeitado pela Assumption já registrada no spec (uma aula marcada para mais tarde no mesmo dia já é elegível assim que a data chega).

## R3 — Validação de "presença obrigatória quando retroativa" fica no serviço, não no FluentValidation compartilhado

**Decision**: `AulaRequestValidator` (usado tanto por `Cadastrar` quanto por `Atualizar`) **não** recebe nenhuma regra nova sobre `Presencas`. A exigência "se retroativa, `Presencas` não pode ser nulo" é verificada dentro de `AulaService.CadastrarAsync`, lançando `ConflitoException` (já mapeada para 409 no controller).

**Rationale**: `AulaRequest` é compartilhado entre criação e edição. Uma regra de validador amarrada a `DataInicio <= hoje` dispararia incorretamente também em edições de aulas já passadas (onde `Presencas` nunca é relevante, já que `AtualizarAsync` tem sua própria lógica para aula "Realizada" — ver R5). Manter a regra específica de criação dentro de `CadastrarAsync` evita esse acoplamento indesejado entre os dois fluxos.

**Alternatives considered**: Criar um DTO de request separado só para criação (`AulaCreateRequest`) — rejeitado por aumentar a superfície de mudança (contrato de API, frontend, documentação) para um ganho pequeno; o DTO compartilhado já é o padrão estabelecido no projeto para Aula.

## R4 — Lista de alunos ativos da turma no frontend, sem nova chamada de API

**Decision**: No formulário de criação, a lista de presença (quando a aula é de turma) é calculada cruzando `turma.alunos` (todos os vinculados, já incluído na resposta de `listarTurmas`) com a lista `alunos` que o próprio `AulaForm` já busca (`listarAlunos({ ativo: true })`, usada hoje para o campo "Aluno" de aula individual) — intersecção por `id`. Para aula individual, a lista de presença é só o aluno selecionado no campo "Aluno".

**Rationale**: `TurmaResponse.Alunos` (`AlunoResumoResponse`) não tem campo `Ativo` e inclui alunos inativos também — usar essa lista sozinha mostraria um checkbox para alunos que o backend nem vai vincular à aula (`ObterAlunosAtivosDaTurmaAsync` já filtra só ativos ao criar a aula de verdade). Cruzar com a lista de alunos ativos, que o formulário já carrega por outro motivo, resolve isso sem precisar de nenhum endpoint novo.

**Alternatives considered**: Criar um endpoint novo "alunos ativos de uma turma" — rejeitado como desnecessário; a informação já está disponível no cliente a partir de duas chamadas que já existem.

## R5 — Edição de aula "Realizada": bloquear tudo exceto Descrição, sem duplicar a validação de conflito

**Decision**: `AtualizarAsync` passa a verificar, quando `aula.Status == "Realizada"`, se o request tenta mudar `MateriaId`, `TurmaId`, `AlunoId` (comparado ao aluno atual da aula, quando individual), `DataInicio`, `HoraInicio` ou `HoraFim` — se qualquer um mudou, lança `ConflitoException` (mesmo contrato de hoje); se nenhum mudou, atualiza só `Descricao` e retorna, sem rodar `ValidarConflitoAsync` (conflito de horário é irrelevante já que nada de horário/data pode mudar) nem a redefinição de alunos vinculados.

**Rationale**: Atende FR-008/FR-009 com o menor diff possível, sem introduzir um novo endpoint "editar descrição" — reaproveita o mesmo `PUT /api/aulas/{id}` já existente, com o formulário de edição no frontend simplesmente desabilitando os demais campos quando a aula está "Realizada" (decisão de UI, não de regra de negócio).

**Alternatives considered**: Endpoint dedicado `PATCH /api/aulas/{id}/descricao` — rejeitado como complexidade desnecessária para um único campo; o padrão já estabelecido no projeto (`PUT` substituindo o recurso inteiro) continua funcionando bem aqui, só com uma regra adicional de "quais campos podem mudar dado o status atual", que já é o padrão usado por `AulaService.AtualizarAsync` hoje (comparar `aula.Status == "Realizada"` antes de aceitar qualquer mudança).

## R6 — Componente de checklist de presença: novo e pequeno, sem refatorar a tela de detalhe existente

**Decision**: Criar `frontend/components/aulas/PresencaChecklist.tsx`, um componente de apresentação simples (lista de `{ id, nome, ra }` + estado de presença + callback de mudança), usado só pelo novo trecho de `AulaForm.tsx`. A tela de detalhe (`aulas/[id]/page.tsx`, usada por "Registrar Sessão") **não** é alterada nem refatorada para usar esse componente.

**Rationale**: A lista de presença de "Registrar Sessão" já funciona e tem um estilo visual mais rico (avatar, link pro perfil do aluno, badges de status histórico) que não se aplica ao momento de criação (ainda não há frequência/motivo a mostrar). Forçar os dois a compartilhar um componente genérico hoje aumentaria o risco de regressão numa tela estável e funcionando, para um ganho de reuso pequeno. Fica registrado aqui como uma oportunidade de unificação futura, não como parte do escopo desta spec.

**Alternatives considered**: Extrair um componente totalmente compartilhado entre as duas telas — considerado e descartado por aumentar o raio de impacto (tocar uma tela que não faz parte do pedido original) sem benefício proporcional.
