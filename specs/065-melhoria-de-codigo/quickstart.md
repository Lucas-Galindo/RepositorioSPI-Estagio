# Quickstart: validar a melhoria do código (spec 065)

Como a spec não muda comportamento, a validação é **comparar antes e depois**.

## Pré-requisitos

- Backup do banco local (`database/backup/backup-database.ps1`). Dados sintéticos "TESTE-065" apagados no fim. Nunca acessar produção.
- API local com o servidor de e-mail apontado para um endereço inexistente; login de teste informado pelo usuário (vai por variável de ambiente, nunca em arquivo).
- Frontend em modo desenvolvimento apontando para a API local (porta 3000, por causa do CORS).

## 1. Linha de base (antes de mexer)

| # | Passo | Resultado esperado |
|---|---|---|
| 1 | `dotnet build` e `dotnet test` | 0 erros; 464 testes aprovados; guardar a contagem de avisos |
| 2 | `npx tsc --noEmit` e `npx eslint` nos arquivos que serão tocados | limpos |
| 3 | Rodar o **instantâneo da API** e salvar a saída | arquivo "antes" com status e corpo normalizados |
| 4 | Rodar as **capturas de tela** e salvar os hashes | pasta "antes" |
| 5 | Medir: blocos `catch` nos controllers, linhas dos controllers, classes CSS | números "antes" |

## 2. Depois de cada história

| # | Passo | Resultado esperado |
|---|---|---|
| 6 | `dotnet build`, `dotnet test` | 0 erros; os mesmos testes aprovados (+ os do filtro) |
| 7 | Repetir o instantâneo da API | **idêntico** ao de antes |
| 8 | Repetir as capturas de tela | hashes **idênticos** nas telas tocadas |
| 9 | Ler o diff | só o que a história permite (US4: só comentário) |

## 3. Medidas finais (para o relatório)

- Blocos `catch` repetidos nos controllers: de mais de 160 para 0 (os especiais do login ficam).
- Linhas dos controllers: queda de pelo menos 1.000.
- Classes CSS removidas: 18; exports sem uso: 2 tratados; `NU1510` resolvido.
- Métodos comentados: lista da pesquisa R6.
