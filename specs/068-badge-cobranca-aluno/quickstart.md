# Quickstart: validar o selo de cobrança (spec 068)

Contratos em [contracts/selo-cobranca-api.md](contracts/selo-cobranca-api.md); modelo em [data-model.md](data-model.md).

## Pré-requisitos

- Backup do banco local antes de criar dados de teste; **sem migração**.
- API local (porta 5099) com `Smtp__SmtpHost=smtp.invalido.localhost`; frontend na porta 3000. Login de teste por `SPI_TESTE_SENHA` (nunca em arquivo).
- Dados sintéticos "TESTE-068" (alunos, turmas, vínculos), apagados no fim; dados do usuário intactos. Construir e subir o frontend com a API parada se faltar memória.

## 1. Testes automatizados

`dotnet test tests/SPI.Application.Tests` (sem `--no-build`): `ResumoCobrancaServiceTests` (principal e "+N", sem vínculo, pacote com restantes/esgotado, mensalidade, consistência com `Decidir`, uma consulta por lista), testes de lista/turma/aula e toda a suíte anterior sem alterar `Assert`.

## 2. Cenário de dados (API autenticada)

Criar: aluno A (só **mensalidade**), B (**pacote** da turma T1 com 10 aulas e 3 dadas), C (**pacote** de T1 **esgotado** e **avulsa** em T1), D (pacote esgotado **sem** avulsa), E (**pacote de T1** e **avulsa** de T2), F (sem vínculo).

| # | Chamada | Esperado |
|---|---|---|
| 1 | `GET /api/alunos?incluirCobranca=true` | A: `Mensalidade`; B: `Pacote` com `aulasRestantes=7`; E: `Pacote` com `outrosVinculos=1` e 2 `detalhes`; F: `SemVinculo` com o valor da aula |
| 2 | `GET /api/turmas/{T1}` | A `Mensalidade`; B `Pacote`; C `Avulsa` com `avisoPacoteEsgotado=true`; D `Pacote` com estado `Esgotado`; E `Pacote`; F `SemVinculo` |
| 3 | `GET /api/turmas/{T2}` (com E matriculado) | E: `Avulsa` (o pacote de T1 **não** vale em T2) |
| 4 | `GET /api/aulas/{aula de T1}` (com um aluno de reposição que tem pacote de outra turma) | reposição: `SemVinculo`/`Avulsa`, nunca o pacote da outra turma |
| 1b | `GET /api/alunos` (sem o parâmetro) | resposta como hoje: `cobranca` nulo em todos e nenhuma consulta de cobrança no log |
| 5 | Contar consultas ao banco por chamada (log da API em Development) | no máximo duas consultas de cobrança por resposta, independentemente do número de alunos |
| 6 | Comparar `categoria` do item 2 com a cobrança gerada ao registrar a chamada de uma aula de T1 | coincidem (mesma regra) |
| 7 | `DESCRIBE`/contagens das tabelas antes e depois | iguais (somente leitura) |

## 3. Telas (Chrome, claro e escuro)

| # | Passo | Esperado |
|---|---|---|
| 1 | `/alunos` | coluna "Cobrança" com um selo por aluno; E mostra "+1" e a dica lista os dois vínculos; F mostra "Sem vínculo (valor da aula)" |
| 2 | `/turmas/{T1}` | selo de cada aluno com o vínculo que vale na turma, sem repetir o nome da turma; D destacado como esgotado |
| 3 | `/aulas/{aula de T1}` | selo ao lado de cada aluno e dos "Outros alunos"; C mostra Avulsa com "pacote esgotado" |
| 4 | Número de requisições de cada tela (aba Rede) | o mesmo de antes da mudança |
| 5 | `npm run verificar-contraste` | 0 pares abaixo de 4.5:1 |
| 6 | Capturas das demais telas | só mudam lista de alunos, turma e chamada |
