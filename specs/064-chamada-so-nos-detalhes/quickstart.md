# Quickstart: validar a chamada só nos detalhes (spec 064)

Guia de validação, não de implementação. Contratos em [contracts/](contracts/); modelo em [data-model.md](data-model.md).

## Pré-requisitos

- Backup do banco **local** (`database/backup/backup-database.ps1`) antes da migração; produção nunca é acessada.
- Migração `database/26_aula_frequencia_registrada_em.sql` aplicada no banco local (conferir: `DESCRIBE aula` mostra `frequencia_registrada_em`).
- API local com `Smtp__SmtpHost=smtp.invalido.localhost` (nenhum e-mail sai) e frontend na porta 3000 (CORS). Login de teste da professora por variável de ambiente (`SPI_TESTE_SENHA`), nunca em arquivo.
- Dados sintéticos com prefixo "TESTE-064", apagados no fim (os dados do usuário não são tocados).

## 1. Testes automatizados (relógio fixo)

`dotnet test` — devem passar: `RegraChamadaTests` (hoje antes do início → bloqueada; hoje já iniciada → liberada; data futura → bloqueada; data passada → liberada; em andamento → liberada; **minuto exato** → liberada; início movido para o futuro → bloqueada de novo), `AulaServiceChamadaTests` (409 antes do início sem gravar nada; liberado depois; aula excluída → 404; `FrequenciaRegistradaEm` gravada), `AulaServiceCadastrarSemPresencaTests` (nasce "Agendada" em qualquer data, sem frequência nem conta) e `NotificacaoService…SemCorteTests`.

## 2. API autenticada (hora real de São Paulo)

| # | Passo | Esperado |
|---|---|---|
| 1 | `POST /api/aulas` com data de **ontem** | 201, `status = "Agendada"`, `chamadaLiberada = true`, sem conta a receber |
| 2 | `POST /api/aulas` com `presencas` no corpo | campo ignorado; aula "Agendada", sem frequência |
| 3 | Aula **hoje** que começa em 1 h → `GET /api/aulas/{id}` | `chamadaLiberada = false`, `segundosParaChamada ≈ 3600` |
| 4 | `POST …/registrar-sessao` nessa aula | **409** `A chamada ficara disponivel a partir das HH:mm de dd/MM/yyyy.`; aula segue "Agendada" |
| 5 | Aula que começou há 10 min e não terminou → `registrar-sessao` | 200, "Realizada", `FrequenciaRegistradaEm` preenchida (conferir no banco) |
| 6 | Mesma aula → `registrar-sessao` de novo | 409 (já realizada) |
| 7 | Aula excluída ainda "Agendada" → `registrar-sessao` | 404 |
| 8 | Aula com **data futura** | 409; editar, cancelar e excluir continuam respondendo 200/204 |
| 9 | `GET /api/notificacoes` | uma `AulaPendente` por aula já terminada e sem frequência, **inclusive as anteriores a 19/10/2026** (hoje: 2 a mais que antes), sem duplicar em chamadas repetidas |
| 10 | Aula em andamento | **sem** aviso |
| 11 | Registrar a chamada de uma aula com aviso | o aviso sai da lista e do contador |

## 3. Telas (Chrome)

| # | Passo | Esperado |
|---|---|---|
| 1 | Agenda/Aulas → "Agendar aula", qualquer data | sem seção "Presença", sem "A data já chegou…", botão "Agendar aula" |
| 2 | Detalhes de aula futura | mensagem "A chamada ficará disponível a partir das HH:MM de DD/MM/AAAA."; sem caixas de presença nem "Registrar como realizada"; Editar/Cancelar/Excluir presentes |
| 3 | Detalhes de aula que começa em ~2 min, página aberta | a chamada aparece sozinha no horário (nova busca agendada pela duração que o servidor informou) |
| 4 | Detalhes de aula antiga sem frequência | chamada da turma liberada; registrar → "Realizada" |
| 5 | Sino → aviso "Aula sem frequência" → "Ir para a aula" | abre os detalhes com a chamada liberada (≤ 2 cliques, SC-004) |
| 6 | Relógio do computador adiantado/atrasado | a tela segue o servidor (não muda o que está liberado) |
| 7 | Capturas de antes e depois das demais telas | só mudam o formulário de agendar e os detalhes da aula |

## 4. Auditoria (somente leitura)

Rodar `database/consultas/aulas_realizadas_antes_do_inicio.sql` no banco **local**: esperado 0 linhas. Conferir `SELECT COUNT(*)` das tabelas `aula`, `aula_aluno` e `pagamento` antes e depois: **iguais** (SC-006). Em produção, quem roda é o usuário, que decide o que fazer com as linhas.

## 5. Encerramento

Apagar os dados "TESTE-064"; conferir que turma 15 e vínculos 20, 21, 22 e 34 seguem intactos; parar os servidores locais.
