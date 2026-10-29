# Revisão de código: cards em "Revisão de Código/Conceito"

**Data**: 2026-10-29
**Critérios** (card "Revisão De Código"): lógica aplicada que não está correta; redundâncias; código morto; sintaxe complexa sem necessidade.
**Escopo**: o código dos cards 055, 056, 058, 059 e 060 (commits `7205c0d` a `b179133`, mais `57d9ab6`) e o que eles tocam. A 057 (cores) já está fechada e não entrou.
**Situação**: só leitura. Nenhum arquivo de código foi alterado, nada foi commitado.

## Resumo

| Tipo | Quantidade |
|---|---|
| Lógica incorreta ou arriscada | 3 |
| Redundância | 4 |
| Código morto | 2 |
| Complexidade desnecessária | 2 |
| Dúvidas de regra de negócio | 1 |

O compilador não emite nenhum aviso de código. O `dotnet format` não encontrou `using` sobrando. Os 386 testes passam, mas nenhum cobre os três casos de lógica abaixo.

## 1. Lógica incorreta ou arriscada

### L1 — Aula excluída ainda pode ter a sessão registrada (média)
- **Onde**: `AulaService.RegistrarSessaoAsync` (linha 273), `CancelarAsync` (440) e `AtualizarAsync` (121).
- **Problema**: `AulaRepository.ObterPorIdAsync` não filtra `Ativo`, e esses três métodos não conferem `aula.Ativo`. Só `ObterAgendadaAsync` (reposição) confere.
- **Efeito**: abrindo `/aulas/{id}` de uma aula excluída (por favorito ou chamada direta à API), ainda é possível "Registrar como realizada". Isso gera cobranças e consome o pacote de uma aula que a professora excluiu.
- **Correção sugerida**: um único ponto que busca a aula e recusa quando está inativa (já existe um parecido em `ObterAgendadaAsync`), usado pelos quatro métodos. Teste: registrar sessão de aula excluída responde 404.

### L2 — Registro de aula pode ficar pela metade (média)
- **Onde**: `AulaService.AplicarPresencasAsync` (295–357) e `GerarContasAReceberAsync` (387–438).
- **Problema**: a aula é salva como "Realizada" (linha 341) antes de gerar as cobranças, e cada cobrança faz seu próprio `SalvarAlteracoes` (linhas 418 e 437), sem transação.
- **Efeito**: se algo falhar no meio (banco, rede), a aula fica "Realizada" com parte das cobranças e parte do saldo do pacote. A tela não permite refazer, porque só aceita aula "Agendada".
- **Correção sugerida**: gravar tudo num único `SalvarAlteracoes` (a cobrança pode ligar a aula pela navegação, sem precisar do id antes) ou usar transação. É a mudança com mais risco: precisa de teste de falha simulada e confirmação sua.

### L3 — E-mail de cobrança pode ser duplicado para quem já recebeu (média)
- **Onde**: `LembretePagamentoService.TentarEnviarAsync` (linhas 141–153).
- **Problema**: se o e-mail do aluno é entregue e o do responsável falha, o resultado vira `Falha`. Na tentativa seguinte (até 3 no mesmo dia), o serviço reenvia para **todos** os destinatários.
- **Efeito**: o aluno que já recebeu pode receber até 3 cópias no mesmo dia, o que contraria a regra de "no máximo um e-mail por aluno por dia".
- **Correção sugerida**: registrar quais destinatários já receberam e, na nova tentativa, enviar só para os que falharam. Teste: um destinatário falha, outro não, e o segundo não recebe de novo.

## 2. Redundâncias

- **R1 — Lista de alunos da aula montada duas vezes.** `AulaService` linhas 92 (`CadastrarAsync`) e 148 (`AtualizarAsync`) repetem a mesma expressão "alunos ativos da turma, ou o aluno individual". Extrair para um método privado.
- **R2 — "Recarregar a aula e mapear" quatro vezes.** Linhas 110, 183, 215 e 236. Um método privado `ObterMapeadaAsync(id)` elimina a repetição.
- **R3 — Consulta duplicada na listagem de notificações.** `NotificacaoService.ListarAtivasAsync` roda `ListarPendentesVencidosAsync` duas vezes na mesma chamada (linhas 92 e 138). A segunda reaproveita o que a primeira já buscou.
- **R4 — Cinco rotinas em segundo plano com o mesmo laço.** `NotificacaoDispatcherService` e `LembretePagamentoDispatcherService` (45 linhas cada) repetem o mesmo esqueleto de temporizador, escopo e tratamento de erro das outras três. Uma classe-base pequena reduziria o trecho repetido. É opcional, pois mexe em arquivos antigos.

## 3. Código morto

- **M1 — `decodeJwtPayload`** em `frontend/lib/jwt.ts` não é usado por ninguém. É anterior a esses cards (commit `ed56083`).
- **M2 — `amostraMaisProxima`** em `frontend/lib/paleta.ts` está exportado, mas só é usado dentro do próprio arquivo. Basta tirar o `export`. É da 057 (fechada); é só higiene.

## 4. Complexidade desnecessária

- **C1 — Status de aula em texto solto.** `"Agendada"`, `"Realizada"` e `"Cancelada"` aparecem em 9 pontos só no `AulaService`, e em outros arquivos. Constantes (ou um `enum`) evitam erro de digitação. Mudança grande e transversal; sugiro tratar em card próprio.
- **C2 — `AplicarPresencasAsync`** faz seis coisas (valida, decide cobrança, avisa, grava, recalcula lembretes, gera cobranças). Fica mais fácil de manter se a parte de aviso e a de gravação forem separadas. Depende da decisão sobre L2.

## 5. Problemas que existem, mas ficam fora do que eu posso alterar agora

- **Aba Lembretes, histórico de envios** (`LembretePagamentoSection.tsx`, linha 132): para o resultado "Falha", o selo mostra "Falha" e logo depois o texto "Falha" de novo ("Falha Falha"). É defeito visual simples, mas a aba está na manutenção dos cards "Melhorar título dos lembretes de aula" e "Aumentar a largura das telas de notificações". Sugiro corrigir lá.
- **"Atrasado" calculado em vários lugares** (`PagamentoService`, `ContaPagarService`, `RelatorioRepository` e o novo `ListarPendentesVencidosAsync`). É anterior a esses cards. Vale uma regra única, mas é uma refatoração financeira que merece card próprio.

## 6. Pergunta de regra de negócio

- **Q1**: uma aula **cancelada** ainda pode ser editada (`AtualizarAsync` só barra a "Realizada"), o que recalcula participantes e lembretes de uma aula que não vai acontecer. Quer que uma aula cancelada fique somente leitura?

## Proposta de ordem

1. L1, R1, R2 e R3: baixo risco, comportamento igual, cada um com teste.
2. L3: com teste novo.
3. L2 e C2: só depois da sua aprovação, por mexerem em como a cobrança é gravada.
4. M1 e M2: limpeza rápida.
5. R4, C1 e a duplicação de "Atrasado": cards próprios, se você quiser.
