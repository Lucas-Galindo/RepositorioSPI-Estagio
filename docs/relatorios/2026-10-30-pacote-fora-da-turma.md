# Relatório: Turmas de pacote e pacote por grupo (spec 063)

**Card Trello**: "Alunos de pacote fora do cadastro fixo das turmas" (Backlog → Design → A Fazer → Em andamento)
**Spec**: `specs/063-pacote-fora-da-turma/` (specify, plan, tasks e analyze em 2026-10-29/30; implement em 2026-10-30)
**Status final**: ✅ Implementado e validado: 464 testes automatizados, quickstart pela API autenticada (T032: 49 verificações, 0 falhas) e teste visual no Chrome (T034: 16 verificações, 0 falhas). A validação visual achou dois defeitos de tela, já corrigidos (ver "Achados da validação visual"). Faltam só decisões suas e a aplicação em produção.

## O que foi pedido (decisões do usuário, 2026-10-29/30)

- Toda turma é de **Mensalidade** ou de **Pacote**. O aluno se matricula na turma de pacote e contrata o pacote dela; depois que a turma começa, **ninguém novo entra** (os alunos podem sair).
- **Todo pacote pertence a um grupo**: uma turma de pacote ou as aulas individuais. Não existe pacote que valha para qualquer aula. O pacote só é consumido em aula do seu grupo; fora dele o aluno paga como avulso, com a conta gerada em Contas a Receber.
- O aluno pode ter **0 ou N pacotes de turma** (um por turma) e no máximo **um** de aulas individuais.
- Quando a turma acaba (data de fim ou turma inativada), os **pacotes dela ficam inativos, com o saldo preservado**. Excluir ou reativar o aluno não mexe no pacote.
- Na chamada, quem está na aula por reposição aparece em **"Outros alunos"**.

## O que foi feito

**Banco (`database/25_turmas_de_pacote.sql`, aplicado só no banco local, com backup antes)**
- `turma.tipo`, `turma.data_inicio`, `turma.data_fim` e três `CHECK`; o `CHECK` do vínculo passou a aceitar turma também para Pacote.
- Migração dos pacotes existentes, sem apagar nada: só converte o pacote ativo de aluno matriculado em **uma** turma **ativa** onde todos os alunos ativos têm pacote. O resto é listado por consultas ao final do script.

**Backend**
- `RegraCobrancaPresenca`: um pacote só é aplicável quando `pacote.TurmaId == turma da aula` (nulo para aula individual). Pacote fora do grupo não consome, não barra e não avisa.
- `TurmaService`: tipo e datas; o tipo e a data de início (depois de preenchida) não mudam; a data de fim pode ser ajustada; matrícula direta em turma de pacote só para quem tem pacote ativo dela e com a turma em andamento; `fechada` calculada no servidor; inativar a turma inativa os pacotes dela.
- `VinculoCobrancaService`: o cadastro do pacote de turma valida a turma (de pacote, ativa, **não iniciada**) e matricula o aluno na mesma gravação; um pacote por turma e um de aulas individuais; turma do pacote imutável; reativar só com a turma em andamento.
- Novo `EncerramentoTurmaPacoteService` + rotina `TurmaEncerramentoDispatcherService` (roda ao iniciar e a cada `Turmas:IntervaloEncerramentoMinutos`, padrão 60): só a coluna `ativo` muda.
- `TurmasController`: o cadastro exige as datas da turma de pacote (400) e as regras de negócio devolvem 409.

**Frontend**: formulário e página da turma (tipo, datas, aviso de entrada pelo pacote); formulário do vínculo com "Turma do pacote"; ficha mostrando a turma do pacote; página da aula com a seção **"Outros alunos"** agrupando só pelo indicador `reposicao` que a API já devolve.

## O que NÃO mudou (de propósito)

Participantes das aulas, lembrete por e-mail, filtros de alunos, relatórios, `AlunoService` e `AulaRepository` (o conflito de horário continua sendo o da professora). Como o aluno de pacote continua matriculado, nada disso precisou de ajuste.

## Testes alterados e criados

**Reescritos por causa da regra nova (pacote deixou de valer em qualquer aula)**: `RegraCobrancaPresencaTests` (reescrito inteiro), `AulaServiceCobrancaPorAlunoTests` (o teste "mesmo pacote em turma A, B e individual" virou "o pacote da turma A só é consumido na turma A", mais três novos), `AulaServiceRegistrarSessaoPacoteEsgotadoTests` ("bloqueia em qualquer turma" virou "não bloqueia em outra turma"), `AulaServiceReposicaoTests` (o pacote do visitante é o da turma da aula), `VinculoCobrancaServiceCombinacaoTests` (mensagem do segundo pacote individual) e `VinculoCobrancaRequestValidatorTests` (a turma passou a ser permitida no Pacote).

**Novos**: `AulaServicePacoteGrupoTests`, `AulaServiceConflitoAlunoTests`, `TurmaServiceTipoTests`, `VinculoCobrancaServiceTurmaPacoteTests`, `EncerramentoTurmaPacoteServiceTests`, `AlunoServicePacoteIntactoTests`.

**Resultado**: 464 testes, 0 falhas (eram 386 antes da spec; 4 a mais depois da validação visual).

## Verificação dos "MUST NOT" (leitura do diff, `grep` e `git status`)

- **FR-010**: `grep "Modalidade =="` em `Aulas/` só acha `RegraCobrancaPresenca.cs`; o frontend não decide cobrança nem calcula "fechada" (usa o campo da API).
- **FR-012**: a divisão da chamada usa só `al.reposicao`.
- **FR-016**: `AulaService.cs` e `AulaRepository.cs` sem diff; `ExisteConflitoAsync` continua sendo o único cálculo de sobreposição.
- **FR-006, FR-015, FR-019, FR-020**: o SQL não tem `DELETE`; os únicos `UPDATE` são `turma.tipo` e `vinculo_cobranca.turma_id` dos convertidos; o serviço de encerramento só atribui `Ativo`; `AlunoService` e `DesvincularAlunoAsync` sem diff.
- **Arquivos novos**: só os previstos (SQL, `TipoTurma`, `EncerramentoTurmaPacoteService` e sua interface, `TurmasOptions`, a rotina e os seis arquivos de teste). Nenhum arquivo novo de frontend.
- **Migração no banco local**: contagens de `vinculo_cobranca` (3, 2 ativos, soma de valor 1150,00, saldo 1), `alunos_turma` (2), `aula` (15), `aula_aluno` (13), `pagamento` (7) e `turma` (3) **idênticas** antes e depois. No banco local nada foi convertido (o único aluno com pacote está em duas turmas); as consultas listaram o caso. O caminho de conversão foi testado numa transação com `ROLLBACK`: a Turma A virou Pacote, o pacote apontou para ela e o pacote inativo ficou intacto.

## Verificações gerais

- `dotnet build` sem erros; `dotnet test` 460/460; `npx tsc --noEmit` e `eslint` limpos nos arquivos tocados; `npm run verificar-contraste` sem par abaixo de 4.5:1.
- `npm run build` passa com 2 workers. Com 11 workers o processo caiu por falta de memória livre da máquina (cerca de 850 MB livres), sem relação com o código (compilou e passou no TypeScript antes de cair). O `next.config.ts` ficou como estava.

## Decisões menores a confirmar

1. O cadastro do pacote de turma **matricula** o aluno; a matrícula direta só vale para quem já tem o pacote ativo dela (a volta).
2. Turma de pacote exige turma ativa e **não iniciada** no cadastro; turma migrada sem data nunca é "iniciada".
3. Tipo da turma, turma do pacote e data de início (depois de preenchida) não mudam.
4. Sair da turma não altera o pacote enquanto ela está em andamento.
5. A verificação de fim de turma roda ao iniciar e a cada 60 minutos (atraso máximo de uma hora depois da virada do dia).
6. A migração só converte aluno matriculado em **uma** turma **ativa** onde todos têm pacote; o resto fica na lista.
7. A professora dona da turma não é conferida no cadastro do pacote, igual ao que a Avulsa já fazia hoje; a regra de multi-professora não faz parte desta spec.

## Pendências

- **Decisões menores** (acima) e **produção** (abaixo).
- **Produção**: a migração 25 **não** foi aplicada fora do banco local. Aplicar só com backup e conferindo as contagens do quickstart.
- **Casos listados pela migração** (aluno 1: pacote ativo em duas turmas, uma inativa; turma 4 mista): nada foi alterado; decidir caso a caso.
- Cards no Backlog: "Recorrência na criação de aulas" e "Saída do aluno de turma de pacote" (acerto proporcional, que depende de o pacote registrar pagamento).

## Teste de fumaça da rotina de fim de turma (banco local, dado sintético)

1. A API subiu em modo Desenvolvimento com o servidor de e-mail apontado para um endereço inexistente, sem erros no log.
2. Para provar a consulta e a inativação contra o MySQL real, inseri **um pacote sintético** ("TESTE-063": aluno 1, Turma 3 inativa, valor 123,45, saldo 7, total 10, ativo).
3. Ao subir de novo, a rotina registrou `Fim de turma: 1 pacote(s) inativado(s), saldo preservado.` O pacote ficou `ativo = 0` com **saldo 7, valor 123,45 e total 10 intactos**; os pacotes 20, 21 e 22 não mudaram.
4. Apaguei o registro sintético. As contagens finais voltaram às iniciais (vínculos 3/2 ativos, soma de valor 1150,00, saldo 1; matrículas 2; aulas 15; presenças 13; pagamentos 7; turmas 3).

Isso valida o mapeamento das colunas novas, a consulta de pacotes de turmas encerradas e a regra "só `ativo` muda". Não substitui o quickstart autenticado.

## Validação em 2026-10-31 (login de teste informado pelo usuário)

**API autenticada (T032)**, com dados sintéticos "TESTE-063" (alunos, turmas, aulas, vínculos), apagados no fim:
- **Fase 1: 44 verificações, 0 falhas**: cadastro e recusas de turma e de pacote (cenários 1 a 12), cobrança por grupo (13 a 24: cada turma consome o seu pacote; aula individual consome o pacote de aulas individuais; pacote de turma em outra turma ou em aula individual cobra avulso e gera conta; pacote esgotado barra só na própria turma; mensalidade e avulso como "outros alunos"), saída e volta (21 a 24), aluno excluído e reativado (25), inativar a turma e reativar (26), datas da turma (27), conflito de horário da professora (29) e reposição (30).
- **Fase 2: 5 verificações, 0 falhas**: depois de mover a data de fim da turma para o passado e reiniciar a API, a rotina inativou o pacote com o saldo 6 preservado; reativar foi recusado (409 com a orientação de prorrogar); prorrogar não reativou sozinho; reativar depois de prorrogar funcionou.
- O login de teste só funcionou **sem o ponto final** que veio na mensagem (era pontuação da frase). A credencial não foi gravada em arquivo, relatório, log nem Trello: ela vai por variável de ambiente só durante a execução.

**Visual (T034)**: Chrome headless contra o frontend em modo de desenvolvimento, com capturas de tela conferidas: formulário da turma de pacote (datas só em Pacote), página da turma aberta e fechada, ficha do aluno e modal do pacote (lista só turmas de pacote não fechadas e "Atendimento individual"), chamada com a seção "Outros alunos" e fluxo de voltar à turma pela tela. **16 verificações, 0 falhas** (depois das correções abaixo).

### Achados da validação visual (corrigidos)

1. **Aviso falso na ficha do aluno.** A ficha mostrava "Este aluno tem vínculos que não combinam mais pela regra atual (Mais de um pacote ativo.)" para quem tem pacotes de turmas diferentes, o que agora é permitido. Era o aviso de "combinação herdada" da spec 056. Passou a valer só para mais de um pacote ativo **do mesmo grupo** (mesma turma, ou vários de aulas individuais). Ajustados o serviço, o texto da tela e os testes.
2. **Turma fechada oferecia todos os alunos para "Vincular".** O seletor listava qualquer aluno ativo e deixava o erro para o servidor. Agora o servidor devolve `alunosComPacoteForaDaTurma` (quem saiu e ainda tem o pacote ativo dela) e a tela oferece só esse grupo; sem ninguém, não mostra o seletor.

Nenhum dos dois foi pego pelos testes automatizados, porque dependiam de como a tela combina as respostas da API.

### Observação fora do escopo desta spec

Em 360px de largura, páginas que já existiam antes (como `/turmas` e `/alunos`) têm o conteúdo com 1260px de largura: o layout geral do app não é responsivo para celular. A turma de pacote se comporta igual às demais. Se quiser celular, é um card próprio.

### Estado do banco local depois dos testes

Idêntico ao que estava antes da validação: as linhas dos seus testes pela tela (turma 15 "Matematica basica 1", o vínculo 34 e os vínculos 20, 21 e 22 inativados) ficaram **intactas**; só apaguei registros com o prefixo "TESTE-063".
