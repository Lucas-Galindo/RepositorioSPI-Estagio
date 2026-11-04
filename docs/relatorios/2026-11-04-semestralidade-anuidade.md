# Relatório — Semestralidade e Anuidade (spec 062)

**Data:** 2026-11-04 · **Spec:** [specs/062-semestralidade-anuidade](../../specs/062-semestralidade-anuidade/spec.md) · **Card Trello:** "Novas formas de cobrança: Semestralidade e Anuidade para o aluno"

## Pedido

Duas formas novas de cobrança do aluno, além de Avulsa, Mensalidade e Pacote: **Semestral** (6 meses) e **Anual** (12 meses), pagas à vista ou parceladas, com as parcelas geradas como contas a receber.

## O que foi feito

**Banco (aplicado só no banco local; produção em standby)** — `database/24_semestralidade_anuidade.sql`: `data_inicio` e `data_fim` no vínculo, modalidades `Semestral`/`Anual` aceitas, regra de período (plano exige as duas datas; as demais modalidades, nenhuma) e as categorias de receita "Semestralidade" e "Anuidade". Não altera nenhum dado existente.

**Backend**
- **Cadastro atômico:** o plano e **todas** as parcelas saem num único salvamento (ou grava tudo, ou nada). Cada parcela é uma conta a receber "Pendente", na categoria do plano, com descrição "Semestralidade - parcela X/N" e competência no mês do vencimento. O fim do plano é início + 6 ou 12 meses − 1 dia. De 1 a 24 parcelas, uma por mês; a soma pode diferir do valor total.
- **Combinações:** Semestral/Anual são exclusivos (como a Mensalidade): recusados com outro vínculo ativo e com período sobreposto; renovação antecipada (período que não se sobrepõe) é aceita. Pacote, Avulsa e Mensalidade são recusados com plano ativo.
- **Presença:** a regra de cobrança recebe a data da aula. Dentro do período do plano a presença não gera conta nem consome pacote e fica ligada ao plano; fora dele, segue as demais regras (a Mensalidade continua valendo sempre). O resultado da decisão `Mensalidade` passou a se chamar `PlanoPeriodico`.
- **Plano não se edita nem se reativa (409).** **Excluir** cancela só as parcelas **pendentes com vencimento depois de hoje**; as pagas, as vencidas e a que vence hoje ficam como estão, e nenhum pagamento é apagado.
- **Aviso de renovação no sino** (`RenovacaoPlano`): uma notificação por aluno ativo cujo plano de maior fim termina em até 15 dias (`Notificacoes:RenovacaoDiasAntes`) ou já terminou; some ao cadastrar a renovação ou excluir o plano; nunca duplica.
- **E-mail de vencimento 5 dias antes** vale também para as parcelas, com o texto "pagamento (descrição)", descrição protegida contra HTML. Em desenvolvimento continua indo só ao endereço de teste.
- Listagem de vínculos traz período e parcelas (numa consulta só), com o status calculado como em Contas a Receber ("Atrasado" = pendente e vencida).
- O selo de cobrança (spec 068) já entende os planos (ordem do principal, vigência na data da turma/aula, cor).

**Frontend**
- Modal do vínculo: ao escolher Semestral/Anual aparecem início, número de parcelas, valor total e 1º vencimento; as parcelas mensais iguais são **sugeridas** (resto dos centavos na última; dia do mês preservado sem passar do fim do mês) e editáveis uma a uma; aviso, sem bloquear, quando a soma difere do total. `lib/parcelas.ts` é só conveniência — o servidor revalida tudo.
- Ficha do aluno: período, parcelas e status; **sem botão "Editar"** para planos; confirmação da exclusão explicando o cancelamento das parcelas futuras; sem "Reativar".
- Sino: tipo "Plano" com filtro "Planos", popup com aluno, tipo e fim do plano e atalho para a ficha.

**Documentação:** notas de Princípio V nas specs 037, 039 e 056; contrato e quickstart corrigidos (a exclusão responde 200, como todo endpoint de exclusão do projeto, e não 204).

## Validação

| Verificação | Resultado |
|---|---|
| `dotnet test tests/SPI.Application.Tests` | **688 aprovados**, 0 falhas (eram 603; 85 novos) |
| Testes novos | planos (cadastro, datas de fim, atomicidade, duplicidade, combinações, edição/reativação recusadas, exclusão, listagem), validador, regra de presença por vigência (limites do período), `AulaService` (sem conta no período, cobra fora), renovação no sino, lembrete de e-mail das parcelas, extensões de modalidade |
| Testes antigos alterados | só a assinatura da regra (`Decidir` recebe a data), o nome `Mensalidade`→`PlanoPeriodico` e um teste que dizia que o valor 4 era inválido (agora é Semestral) |
| `dotnet build` | 0 erros, 12 avisos (os mesmos de antes: pacotes NuGet com vulnerabilidade conhecida) |
| `tsc`, `eslint`, `verificar-contraste`, `npm run build` | limpos; 0 pares abaixo de 4,5:1; `next.config.ts` restaurado |
| `sugerirParcelas` com casos reais | 9 de 9 (1000 em 3, 31/01→28/02, ano bissexto, 12 parcelas pela virada do ano, entradas inválidas) |
| API real (quickstart 1 a 14 e 16) | **18 de 18** com dados "TESTE-062" |
| Chrome (claro e escuro, 360 px) | 20 de 20: ficha, modal com parcelas sugeridas e aviso de diferença, sem "Editar", confirmação da exclusão, sino com filtro "Planos", popup e atalho, sem rolagem horizontal, sem erro de JavaScript |
| "MUST NOT" (CLAUDE.md) | um único ponto cria `Pagamento` a partir de um vínculo, com um único salvamento; nenhum `Remove`/`ExecuteDelete` novo; o frontend não decide cobrança nem vigência (só sugere parcelas); só os arquivos novos previstos em `git status --porcelain` |
| Banco local | depois da limpeza, contagens idênticas às de antes (só as 2 categorias novas, esperadas); backups `spi_db_20261104_115959.sql` e `spi_db_20261104_121807.sql` |

## Decisões e observações

- **Exclusão responde 200**, igual a todos os endpoints de exclusão (o contrato dizia 204; ajustei o documento, não o comportamento existente).
- **Cenário 15 do quickstart (lembrete de e-mail):** validado só por teste automatizado (assunto, corpo, descrição protegida e flag de envio); não foi disparado e-mail de verdade nesta rodada.
- O contador de "Atrasado" das parcelas usa a mesma regra de Contas a Receber (data em UTC), por consistência.
- **Produção:** a migração 24 não foi aplicada fora do banco local (standby combinado). Aplicar com backup, junto das pendentes (21 a 23, 25 e 26).
- O relógio das regras de turma e de exclusão usa a hora da máquina (`DateTime.Now`), conforme a regra de que o sistema usa o fuso de onde está instalado.

## O que ficou de fora

Taxa de maquininha e desconto como campos próprios, renovação automática e período em mês fixo (decisões da spec). Nada foi commitado nem enviado (push).
