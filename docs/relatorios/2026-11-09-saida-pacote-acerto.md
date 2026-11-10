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
