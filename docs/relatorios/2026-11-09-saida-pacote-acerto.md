# Relatório: Saída do aluno de turma de pacote com acerto proporcional ao consumo (spec 078)

**Data**: 2026-11-09 · **Card**: "Saída do aluno de turma de pacote: acerto proporcional ao consumo" · **Spec**: [specs/078-saida-pacote-acerto/](../../specs/078-saida-pacote-acerto/spec.md)

## O que foi feito

Na tela da turma de pacote, cada aluno com pacote ativo (turma em andamento) ganhou o botão **"Registrar saída"**, ao lado de "Remover". Ele abre um resumo do acerto e, ao confirmar, faz tudo numa operação só.

Regras decididas com a professora (todas no servidor, Princípio II):

| Tema | Regra |
|---|---|
| Valor devido | valor do pacote × aulas consumidas ÷ total de aulas, arredondado a centavos **uma vez** (R$ 100, 10 aulas, 7 consumidas = R$ 70,00; R$ 100, 3 aulas, 2 consumidas = R$ 66,67). Consumidas = as mesmas aulas dadas da ficha do aluno (spec 058) |
| Quanto já pagou | a professora **informa** no resumo (o pacote não registra pagamento; parcelas do pacote ficam para outra spec) |
| Pagou menos | uma conta a receber **Pendente** da diferença, categoria "Acerto de pacote", com o vencimento informado (de hoje em diante) |
| Pagou mais | **não devolve** (sem devolução, crédito ou saída de dinheiro) |
| Ao confirmar (atômico) | desvincula da turma, tira o aluno das aulas agendadas da turma que ainda não começaram, encerra o pacote (aulas não usadas perdidas) e cria a conta da diferença. O aluno **não volta pelo pacote** (reativar e matrícula direta são recusados) |
| "Remover" (desvincular) | continua **exatamente** como antes (reversível, sem acerto, pacote intacto) |
| Proteção | resumo com confirmação; se o consumo mudou entre abrir e confirmar, a gravação é recusada e o resumo é refeito. Sem "Desfazer saída" nesta versão |

## O que mudou no código

- **Banco**: migração `database/28_saida_pacote_acerto.sql` (só acréscimos, pode rodar mais de uma vez): coluna `vinculo_cobranca.saida_registrada_em`, regra `chk_vinculocobranca_saida` e a categoria de receita "Acerto de pacote". **Aplicada só no banco local**, com backup antes (`database/backup/spi_db_20261109_211402.sql`). **Produção continua pendente** (como as migrações 21 a 27).
- **Backend**: `AcertoSaidaPacote` (cálculo estático e puro), `SaidaPacoteService` (prévia e gravação a partir da mesma leitura), dois endpoints novos sob a turma (`POST .../alunos/{alunoId}/saida/previa` e `.../saida`, com 400/404/409 declarados), `PodeRegistrarSaida` no aluno da turma (calculado no servidor), `ReativarAsync` recusa o pacote encerrado pela saída, e duas operações novas em `IAulaRepository` (listar e remover as participações futuras, na mesma transação).
- **Frontend**: `SaidaPacoteModal` (resumo vindo do servidor, vencimento só quando há diferença, resposta atrasada de um valor antigo não sobrescreve o último), botão na turma e, na ficha do aluno, "Saída registrada em dd/mm/aaaa" sem o botão "Reativar". A tela **não calcula nada de dinheiro**.
- **Docs**: nota na spec 063 apontando para esta spec (Princípio V).

## Validação

| Verificação | Resultado |
|---|---|
| `dotnet test` | **835 aprovados** (764 anteriores + 71 novos: cálculo, validador, serviço da saída, `PodeRegistrarSaida`, reativação) |
| `tsc`, `eslint`, `npm run build` | 0 problemas / build concluído |
| API real (banco local, dados "TESTE-078") | **48 verificações aprovadas**: prévia (30 → conta de 40; 100 → excedente não devolvido; 70 → fechado; 66,67; 42,81; 0 consumidas), `valorPago` inválido (400), 404/409, vencimento ausente/de ontem (409), **consumo mudou desde a prévia (409, nada muda)**, saída com conta (Pendente de 40,00, categoria certa, ligada ao pacote, exatamente 1 conta), repetir (409, sem 2ª conta), reativar e matricular de novo (409), pagou a mais (sem conta, nenhum lançamento ≤ 0), desvincular intacto (pacote e saldo iguais, pode voltar), aulas (sai só de F1/F2; a já iniciada, as realizadas com presença, a individual e a **reposição** ficam) e **atomicidade com falha forçada** (uma conta duplicada na mesma competência faz o salvamento falhar: o aluno continua matriculado, o pacote ativo, as aulas e as contas iguais; sem a trava a mesma saída funciona) |
| Chrome (login real, claro e escuro) | **20 de 20** nos passos de tela (14 + 6): botão só nos elegíveis, resumo, resposta atrasada, fechar sem confirmar, consumo que muda com o modal aberto, saída com e sem conta, ficha com "Saída registrada em" e sem "Reativar", modal no escuro; 0 erros de console |
| "MUST NOT" (CLAUDE.md) | lidos os diffs: `TurmaService` só ganhou o preenchimento de `PodeRegistrarSaida` (desvincular intacto) e o `TurmasController` só trocou o construtor (`DesvincularAluno` intacto); o único `Pagamento` criado é `Pendente` com a diferença > 0, no ramo `Diferenca > 0`; nenhum cálculo de dinheiro no frontend (só `Number(texto)`); nenhuma ocorrência de "Desfazer"; `git status -- database` só com a migração 28; nenhum `csproj`/`package.json` alterado; consulta de aulas só com `RemoveRange` rastreado (sem `ExecuteDelete`) |
| Limpeza | dados de teste removidos; contagens iguais às do início, exceto `categoria_receita` (+1, a categoria da migração), `refresh_token` (logins de teste) e `envio_lembrete_pagamento` (+1: o registro de uma execução da rotina de lembretes enquanto a API estava no ar, sobre uma conta antiga, "aluno sem e-mail"; não é de dado de teste); `ra_controle` restaurado para 90 |

## Ressalvas honestas

- **Filtro das aulas fora do teste unitário**: o que decide quais aulas o aluno deixa (agendada, ainda não começou, não é reposição, mesma turma) é a consulta do repositório real; os fakes dos testes devolvem o que foi semeado. Por isso essa parte foi validada **na API real** (caso 17, inclusive a reposição) e pela leitura do diff.
- Dois itens "falharam" na primeira execução dos meus roteiros e **não eram falhas do produto**: o roteiro chamou `POST` em vez de `PATCH` para reativar (refeito: 409 com a mensagem certa) e procurei a conta por um nome de campo errado e o pacote encerrado sem marcar "Mostrar excluídos" na ficha (refeito: tudo aprovado).
- **Observação fora desta spec**: uma conta com vencimento **hoje** aparece como "Atrasado" na ficha do aluno no fim da noite, porque o status "Atrasado" é calculado com a data em UTC (`DateTime.UtcNow`, regra já existente). Vale um card próprio, se incomodar.
- **Sem commit nem push**: tudo está só no diretório de trabalho, junto com a pasta `specs/078-saida-pacote-acerto/`.

## Revisão de código e correções (2026-11-12)

Depois do commit `c43041c`, o código do card foi relido à procura de lógica frágil, duplicação e código complexo. O que mudou (comportamento mantido, exceto os itens 1 e 2, que corrigem falhas):

| # | Onde | Problema | Correção |
|---|---|---|---|
| 1 | `SaidaPacoteService.RegistrarAsync` (lógica frágil) | A confirmação só comparava o consumo e o valor devido. Se uma aula agendada fosse criada ou começasse entre o resumo e a confirmação, o aluno sairia de um número de aulas **diferente do mostrado** (contra o SC-007) | A confirmação passa a ecoar também `aulasQueSairaoEsperadas`; qualquer diferença dá 409 "o resumo mudou", nada é gravado e o resumo é refeito. Comparação isolada em `ExigirResumoConfirmado` |
| 2 | `SaidaPacoteModal` (corrida) | Durante os 350 ms de espera ao digitar, a resposta de uma consulta antiga ainda valia: podia reaparecer um resumo do valor anterior (por exemplo sem o campo de vencimento) com o botão "Confirmar" ligado | Ao digitar, o pedido em andamento é invalidado na hora (e também ao fechar o modal); só vale a resposta do pedido mais novo |
| 3 | `SaidaPacoteModal` (duplicação) | Dois caminhos quase iguais de consulta (ao abrir e ao digitar) e o erro era gravado duas vezes depois de um 409 | Um só `consultar`, um só `mensagemDe`, e o erro do 409 é gravado uma vez, depois de refazer o resumo |
| 4 | `SaidaPacoteModal` | Clicar fora do modal durante o envio fechava a janela com a gravação em andamento | O clique fora é ignorado enquanto envia |
| 5 | `SaidaPacoteService` | Turma inativa recebia a mensagem "já terminou", que está errada; e o contexto da saída carregava `Hoje` e `Agora` duplicados | Mensagem "não está em andamento (inativa ou já terminou)"; `Hoje` passou a ser derivado de `Agora` |
| 6 | Docs | A spec, o contrato, o modelo de dados e o quickstart descreviam só dois ecos | Atualizados (FR-007, edge "tela desatualizada", contrato, data-model e caso 18 do quickstart) |

Revalidação: **838 testes** (3 novos: aulas que mudaram, resumo igual ao gravado, mensagem da turma inativa), `tsc`, `eslint` e build limpos; **API real 51 de 51** (o caso novo cria uma aula depois da prévia: 409 e nada muda; o novo resumo mostra 3 aulas); Chrome nos mesmos passos e no escuro, sem erros de console (duas verificações do roteiro estavam com expectativa velha: agora são 3 aulas por causa do caso novo, e uma expressão regular errada; o banco confirma a conta de R$ 47,09 Pendente). Dados de teste removidos; contagens iguais às do início, exceto as mesmas diferenças já registradas acima.

Pontos vistos e **mantidos de propósito**: a contagem de aulas consumidas é a mesma da ficha do aluno (spec 058), mesmo que ela conte presenças de aulas depois excluídas; uma corrida de dois cliques simultâneos com pagamento ≥ devido é inofensiva (a segunda é idempotente), e com conta a unicidade do banco impede a duplicada; depois da saída, numa turma que ainda **não** começou (início hoje), a professora pode cadastrar um pacote **novo** para o mesmo aluno (regra da spec 063: turma ainda aberta), o que é decisão dela.
