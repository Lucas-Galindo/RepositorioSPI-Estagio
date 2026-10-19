# Relatório: Progresso de Pacote de Aulas (spec 058)

**Card Trello**: "Administração de aulas vencidas". Ficou bloqueado com uma dúvida de negócio, foi desbloqueado com a decisão do usuário e seguiu Design → A Fazer → Em andamento → Revisão.
**Spec**: `specs/058-progresso-pacote/` (specify, plan, tasks, analyze e implement em 2026-10-19)
**Status final**: ✅ Implementado. Falta conferir as telas num navegador (T017, parte manual).

## Decisão de negócio (do usuário)

"Contar as presenças" (opção A):
- **aulas dadas** = presenças registradas no pacote;
- **restantes** = total − dadas.

## O que foi feito

**Cálculo**
- `ProgressoPacote.Calcular` é o único cálculo, usado pela ficha do aluno e pela Home. Ele devolve total, dadas, restantes e estado (Em dia / Atenção / Esgotado / Sem total).
- As aulas dadas são contadas **em lote**, numa consulta agrupada por pacote, a partir de `aula_aluno.vinculo_cobranca_id`. Só a presença recebe esse campo, então falta e aula cancelada nunca contam.

**Cadastro e edição**
- O formulário de Pacote ganhou "Total de aulas". Antes esse campo existia só no backend, sem tela.
- No cadastro, o saldo começa igual ao total. Na edição o saldo continua disponível, como o ajuste manual do bloqueio da spec 041.
- A edição recusa um total menor que as aulas dadas, agora contadas pelas presenças.

**Ficha do aluno**
- Cada pacote mostra a barra "N de M aulas", quantas restam e o estado.
- O botão "Ver aulas" lista as aulas que contaram, com data, horário, matéria, turma e status. Cada aula tem link para a própria aula.
- O antigo "Saldo: N" saiu da ficha.

**Home**
- A seção "Pacotes de aulas" substitui o "Pacotes em atenção", que era baseado no saldo.
- **Indicadores**:
  - pacotes ativos, próximos do fim e concluídos;
  - aulas dadas e restantes;
  - aulas dadas no período.
- **Lista "Precisam de renovação"**: os mesmos números da ficha do aluno.
- **Filtros**: aluno e período.

**Endpoints**
- Novo: `GET /api/alunos/{id}/vinculos-cobranca/{vinculoId}/aulas`.
- O `GET /api/dashboard` aceita `alunoId`.

**Sem banco novo**: nenhuma tabela ou coluna nova; usa o vínculo de cobrança e as presenças que já existem.

**Código removido**:
- `AulasDadas` antigo, calculado como total − saldo e nunca exibido;
- `MontarPacotesEmAtencao`, que usava o saldo;
- `PacotesEmAtencaoPanel.tsx`;
- os testes do painel por saldo, substituídos.

**Documentação**: as specs 041 e 052 ganharam nota apontando para a 058.

## Decisões tomadas e por quê

- **Pacote sem total informado** (criado antes desta spec pela tela, que não tinha o campo): o total é **estimado** como aulas dadas + saldo e aparece marcado "(estimado)". Sem total nem saldo, mostra só as aulas dadas.
- **"Concluídos"** conta pacotes ativos ou não com 0 restantes, para incluir os já encerrados. **"Ativos"**, "próximos do fim" e a lista de renovação consideram só pacotes ativos de alunos ativos.
- **Período**: afeta o indicador "aulas dadas no período" (e o restante do dashboard, como já acontecia). Os estados dos pacotes retratam a situação atual.

## Efeitos que você precisa conhecer

- **Presenças anteriores à spec 052** não gravaram o vínculo e não entram na contagem. O progresso pode mostrar menos aulas dadas que o uso real desses pacotes antigos. Era o risco conhecido da opção A.
- **O bloqueio de presença continua no saldo** (spec 041). Se o saldo e as presenças divergirem, por saldo editado à mão ou presença antiga, o pacote pode aparecer com aulas restantes na tela e mesmo assim bloquear, ou o contrário. Ajustar o saldo na edição do pacote resolve.

## Testes ajustados ao modelo novo

- `VinculoCobrancaServiceAuditoriaTests`: aulas dadas e a validação do total passam a usar presenças.
- `AulaServiceRegistrarSessaoPacoteRenovacaoTests`: renovar o pacote agora é aumentar o total, e o painel reage às restantes.
- `DashboardPacotesEmAtencaoTests` foi substituído por `DashboardPacotesTests`.

## Como foi testado

- `dotnet test`: **352/352**. Os testes novos cobrem:
  - o cálculo (tamanhos diferentes, total estimado, sem total, limites de estado, nunca negativo);
  - a ficha (progresso em lote, saldo inicial, lista de aulas, 404 para pacote de outro aluno);
  - a Home (indicadores, ordem da renovação, mesmos números da ficha);
  - FR-001: falta e cancelada não ficam atribuídas ao pacote.
- **MUST NOT**: `git status --porcelain -- database` vazio, e nenhuma mudança em entidades ou mapeamentos (só um comentário corrigido). No frontend, a única conta é a largura da barra.
- **API real no localhost**:
  - backup antes;
  - SMTP apontado para um host inexistente;
  - dados "TESTE-058", apagados ao final;
  - os vínculos reais continuaram com 3 registros e R$ 1150,00.
  - **8/8 cenários do quickstart OK.**
- `dotnet build`, `tsc`, `eslint` nos arquivos tocados, `npm run build` e `npm run verificar-contraste` OK.

## Pendências

- **Navegador (T017).** Falta conferir a ficha do aluno e a Home num navegador real.
