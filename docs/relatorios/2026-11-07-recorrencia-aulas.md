# Relatório: Recorrência na criação de aulas (spec 077)

**Data**: 2026-11-07 · **Card**: "Recorrência na criação de aulas" · **Spec**: [specs/077-recorrencia-aulas/](../../specs/077-recorrencia-aulas/spec.md)

## O que foi feito

Ao agendar uma aula, a professora pode marcar **"Repetir esta aula toda semana"** e criar várias aulas de uma vez, em vez de cadastrar uma por uma. A opção existe na página "Nova aula" e no pop-up "Agendar aula" da Agenda; **não aparece na edição** de uma aula.

Regras decididas com a professora (todas no servidor, Princípio II):

| Tema | Regra |
|---|---|
| Dias e horários | Um ou mais dias da semana, cada um com **seu próprio horário** de início e fim (ex.: terça 14h–15h e quinta 10h–11h). Dois horários no mesmo dia não podem se sobrepor |
| Término | Por **número de aulas** (1 a 60) **ou** por **data final** (inclusive) — nunca os dois |
| Limite | No máximo **60 aulas** por série |
| Início | A série só pode começar **de hoje em diante** (compara só a data) |
| Conflito | **Todas ou nenhuma**: qualquer data em conflito de horário recusa a série inteira, listando as datas (até 10, e "e mais N") |
| Resultado | Aulas **comuns e independentes** ("Agendada", com os alunos ativos da turma, ou o aluno do atendimento individual); editar/cancelar/excluir é uma a uma. **Sem migração** e sem "série" gravada |
| Prévia | "Ver prévia" mostra as datas calculadas pelo servidor, com o total e as datas em conflito marcadas; não grava nada |

## O que mudou no código

- **Backend**: `GeradorSerieAulas` (função pura de datas), `RecorrenciaAulaRequestValidator`, DTOs, `AulaService.PreVisualizarSerieAsync`/`CadastrarSerieAsync` (só **acréscimos**: 132 linhas adicionadas, 0 removidas) e dois endpoints novos: `POST /api/aulas/serie/previa` (200) e `POST /api/aulas/serie` (201), com 400/404/409 declarados. A criação grava tudo num **único salvamento** (transação única do EF) e recalcula o lembrete da turma uma vez.
- **Frontend**: tipos e funções em `lib/api/aulas.ts`, componente novo `components/aulas/RecorrenciaAulas.tsx`, integração em `AulaForm.tsx` e estilos em `styles/dashboard.css` (só tokens de tema). O frontend só coleta e exibe: **nenhuma conta de datas, conflitos ou contagem**.
- **Documentação**: nota na spec 064 (cada aula da série segue as regras de chamada) e ajuste do texto do limite de 60 (a mensagem cita o limite; o total exato não é calculado, a geração para ao passar de 60).

## Validação

| Verificação | Resultado |
|---|---|
| `dotnet test` | **764 aprovados** (715 anteriores + 49 novos: gerador de datas, validador, serviço com fakes) |
| `tsc`, `eslint`, `npm run build` | 0 problemas / build concluído |
| API real (banco local, dados "TESTE-077") | **27 verificações aprovadas**: prévia por quantidade e por data final (inclusive), criação (201, 10 aulas "Agendada" com o aluno), repetição das mesmas datas (409 com as datas, nada criado), 61 aulas (400) e data final distante (409 citando 60), início ontem (409), horários sobrepostos / quantidade + data final / sem término (400), matéria/turma/aluno inexistentes (404), conflito no meio da série (409, contagem igual à de antes), série individual, sem login (401), e `POST`/`PUT /api/aulas` **inalterados** |
| Chrome (login real, claro e escuro) | **27 de 28 aprovadas** + 0 erros de console: opção na página e no pop-up da Agenda (data clicada vira o início), adicionar/remover dia, término por número e por data, prévia, conflito marcado e série barrada com as datas, série agendada (toast "4 aulas agendadas com sucesso." e as 4 aulas na lista), aula única sem repetir igual, edição **sem** "Repetir", pop-up fecha e o mês mostra a aula nova |
| "MUST NOT" (CLAUDE.md) | `git diff` do `AulaService`: só acréscimos; `grep` de `new Date(`/`getDay`/`setDate`/`.sort(`/`.filter(` no frontend novo: nenhum cálculo de datas (o único `.filter(` remove uma linha de horário da tela); `git status --porcelain -- database` vazio; nenhum `package.json`/`.csproj` alterado |
| Limpeza | Dados de teste removidos; contagens de todas as tabelas iguais às do início (exceto `refresh_token`, +2 por logins de teste) |

## Ressalvas honestas

- **Layout estreito (celular)**: o shell do app tem largura mínima de desktop e o layout para celular está **fora de escopo** no projeto; por isso o item "360 px" do quickstart foi trocado por medir o **bloco da série num contêiner de 330 px**: sem estouro de largura, linhas quebram e o campo de data ficou com 260 px depois de um ajuste de CSS. A única verificação que falhou no Chrome (bloco dentro de 360 px da tela) mediu a página inteira e não se aplica a este app.
- Erros que apareciam nos meus roteiros de teste (descobertos e corrigidos **no roteiro**, não no produto): checkbox "Repetir" contado como campo de data no teste da aula única; matéria não escolhida no teste de prévia (a mensagem "O campo MateriaId e obrigatorio." apareceu corretamente no aviso da prévia).
- **Sem commit nem push**, a pedido: todas as mudanças estão só no diretório de trabalho.

## Revisão de código e refatoração (2026-11-08)

Depois do commit inicial, o código da série foi relido à procura de redundâncias e inconsistências. O comportamento não mudou (764 testes, API 27/27 e Chrome repetidos); o que mudou:

| Onde | Problema | Correção |
|---|---|---|
| `AulaService` | O objeto `Aula` nascia em dois lugares (aula única e série) e os alunos da aula em dois trechos iguais | `NovaAulaAgendada` e `ObterAlunoIdsDaNovaAulaAsync`, usados pelos dois |
| `AulaService` | A série montava um `AulaRequest` falso só para validar matéria/turma/aluno | `ValidarReferenciasAsync` passou a receber os três ids; a série valida as referências **antes** de gerar as datas |
| `AulaService` | Duas listas paralelas (datas e conflitos) casadas pelo índice; contas duplicadas para o total de conflitos | Uma lista de pares (data, conflito); `PodeCriar = conflitos == 0` |
| `AulaService` | Formatação de horário com escape desnecessário (`HH\:mm`) | `HH:mm` com cultura invariável e helper da mensagem de conflito |
| `GeradorSerieAulas` | Parâmetro `limite` que ninguém usava, `break` e proteção de fim de calendário redundantes | Percorre por número do dia (sem estourar perto do fim do calendário) e usa só a constante |
| Validadores | A regra "matéria obrigatória / turma OU aluno" e as 3 mensagens estavam copiadas em `AulaRequestValidator` e no da série | Uma só fonte: `ExigirMateriaETurmaOuAluno` (interface `IReferenciasDaAula` nos dois pedidos), sem mudar nenhuma mensagem |
| `AulaForm` | Dois blocos `try/catch` quase iguais e os campos da aula copiados no pedido da série | Um só envio e `camposDaAula()` compartilhado; `repetir && !editando` virou `repetir` (a caixa não existe na edição) |
| `AulaForm` (bug) | Uma resposta **atrasada** de uma prévia antiga podia reaparecer depois de a série mudar | Número do pedido de prévia: respostas de pedidos antigos são ignoradas (testado no Chrome com a resposta atrasada em 3 s) |
| `AulaForm` | Com conflito na prévia o botão "Agendar aulas" continuava ativo (o servidor recusava) | Fica desligado quando o servidor diz `podeCriar = false` (só exibe o que o servidor decidiu); a mudança da série reabilita |

Fora do que foi alterado, de propósito: as 60 leituras por chave ao montar a resposta da série (o cadastro único faz o mesmo, e o repositório não tem consulta em lote); os `409` para as regras de negócio da série (é o padrão do projeto, como na spec 064).
