# Relatório: Cobrança por Aluno e Reposição de Aulas (spec 056)

**Card Trello**: "Cobrança por aluno e reposição" (Em andamento → Revisão)
**Spec**: `specs/056-cobranca-por-aluno/` (specify, plan, tasks, analyze e implement em 2026-10-19)
**Status final**: ✅ Concluído (33/33 tarefas)

## O que foi entendido do pedido

A regra de cobrança decidida com você em 2026-10-19:

- **Mensalidade** é do aluno, sem turma, e exclusiva. Cobre todas as aulas, em turma ou individuais, e nenhuma presença gera cobrança por aula.
- **Pacote** é do aluno, sem turma, e só pode haver um ativo. Cada presença consome 1 aula, em qualquer turma ou aula individual. Falta não consome.
- **Avulsa** continua por contexto (uma por turma e uma individual) e pode conviver com o pacote. Com o pacote esgotado, a presença é cobrada pela avulsa do contexto, com um aviso.
- **Sem vínculo**, a presença é cobrada pelo "Valor da aula" do aluno. Pacote esgotado sem avulsa no contexto bloqueia a presença.
- **Reposição**: a professora coloca um aluno de fora da turma numa aula agendada.
- **Combinações antigas** que hoje são proibidas só são sinalizadas, nunca resolvidas sozinhas.

## O que foi feito

**Banco**: `database/21_cobranca_por_aluno.sql`, aplicado no banco local com backup antes.
- Mensalidades e pacotes ficaram sem turma.
- A unicidade agora só vale para Avulsa por contexto.
- Uma regra no banco impede turma em Mensalidade ou Pacote.
- A coluna nova `aula_aluno.reposicao` marca os alunos de reposição.

**Backend**
- **Regra de cobrança.** Fica num lugar só, `RegraCobrancaPresenca.Decidir`. A ordem é:
  1. mensalidade;
  2. pacote com saldo, ou pacote com saldo nunca informado;
  3. avulsa do contexto da aula;
  4. bloqueio, se houver pacote esgotado;
  5. valor da aula.
  - `AulaService` só aplica o efeito decidido e não tem mais nenhuma checagem de modalidade.
- **Regras de combinação** no cadastro, na edição (sempre) e na reativação de vínculos. São quatro recusas:
  - mensalidade com outros vínculos ativos;
  - qualquer vínculo com uma mensalidade já ativa;
  - segundo pacote ativo;
  - avulsa repetida no mesmo contexto.
- **Validador**: recusa turma em Mensalidade ou Pacote.
- **Código removido**: os métodos que buscavam o vínculo "do contexto" (`ExisteAtivoAsync` e `ObterAtivoPorAlunoEContextoAsync`) não têm mais uso e saíram.
- **Reposição.** Endpoints novos:
  - `POST /api/aulas/{id}/participantes`;
  - `DELETE /api/aulas/{id}/participantes/{alunoId}`.
- **Edição de aula com reposição:**
  - alunos de reposição continuam na aula;
  - se a aula vira individual, eles saem;
  - se o aluno de reposição passa a ser matriculado na turma, ele só perde a marca, sem ficar duplicado.
- **Conflitos herdados**: a listagem de vínculos do aluno preenche `conflito` (mensalidade junto com outros vínculos, ou mais de um pacote).
- **Rótulos**:
  - a cobrança mensal agora se chama "Mensalidade - MM/aaaa";
  - o painel de pacotes mostra "Todas as aulas do aluno".

**Frontend**
- **Formulário de vínculo**: o campo Turma só aparece para Avulsa; Mensalidade e Pacote mostram "Vale para: Todas as aulas do aluno".
- **Ficha do aluno**: mostra o contexto correto de cada vínculo e um aviso quando há conflito herdado.
- **Detalhe da aula agendada de turma**: lista de alunos ativos com o botão "Adicionar (reposição)"; cada aluno de reposição aparece com a marca "Reposição" e um botão para retirá-lo.

**Documentação**: as specs 037, 038, 039, 041 e 052 ganharam uma nota no topo dizendo o que a 056 mudou em cada uma.

## Decisões tomadas e por quê

- **Duas mensalidades herdadas**: o job continua gerando uma cobrança para cada uma até a professora resolver, porque o sistema não soma nem descarta valores sozinho. Na presença, vale a mensalidade mais antiga.
- **Pacote com saldo nunca informado**: continua sendo usado sem cobrar e sem bloquear, como já era antes.
- **Remover participante**: a tela tira o aluno da lista na hora, sem recarregar a aula. O backend devolve a aula atualizada, mas o cliente usa o `apiDelete` padrão.

## Testes ajustados (modelo antigo por contexto)

- `Vinculo_esgotado_em_outro_contexto_nao_bloqueia` virou `Pacote_esgotado_bloqueia_em_qualquer_turma`, porque agora o pacote vale em todas as aulas.
- **Dispatcher de mensalidade**: os testes de descrição com turma e com "Atendimento individual" viraram um único teste de "Mensalidade - 09/2026". O teste das duas mensalidades virou o caso de conflito herdado.
- **Vínculos (cadastro, edição e reativação)**: os casos de unicidade por contexto agora usam Avulsa, e os casos de Mensalidade ficaram sem turma.
- **Dashboard**: o contexto agora é sempre "Todas as aulas do aluno"; o caso de dois pacotes virou conflito herdado.

## Como foi testado

- `dotnet test`: **329/329**. São 55 testes a mais que na 055, cobrindo:
  - a regra de decisão;
  - a cobrança por aluno;
  - as combinações e os conflitos de vínculos;
  - a reposição;
  - o validador.
- Os requisitos MUST NOT têm testes próprios:
  - FR-004: mensalidade não gera cobrança por aula;
  - FR-010: falta não consome o pacote;
  - FR-016: aluno matriculado não pode ser removido da aula.
- `dotnet build` e `npm run build` sem erros; `tsc` e `eslint` limpos nos arquivos tocados.
- **Verificações MUST NOT:**
  - na migração, o único comando sobre dados é o `UPDATE ... SET turma_id = NULL`; os totais de vínculos ficaram iguais antes e depois (3 vínculos, R$ 1150,00);
  - `AulaService` não tem nenhum `Modalidade`;
  - `git status --porcelain` mostra só os arquivos novos esperados.
- **API real no localhost**:
  - backup do banco antes;
  - token de Professor gerado localmente, sem senha;
  - SMTP apontado para um host inexistente durante todo o teste, para garantir que nenhum e-mail saísse;
  - alunos sintéticos "TESTE-056", sem e-mail, apagados ao final.
  - **24/24 cenários do quickstart passaram**, em mensalidade, pacote, reposição e conflito herdado.

## Pendências e riscos

- **Produção**: antes de aplicar a migração 21, vale rodar a listagem de vínculos dos alunos para ver quantos conflitos herdados aparecem. A migração não desativa nada.
- **Navegador**: as telas novas não foram vistas num navegador real (não havia ferramenta de navegador na sessão). Foram conferidas por código, `tsc` e build.
- **Pendência da 055**: segue aberta a decisão sobre recalcular as frequências antigas.
