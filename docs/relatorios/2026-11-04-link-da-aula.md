# Relatório — Link da aula temporário (spec 067)

**Data:** 2026-11-04 · **Spec:** [specs/067-link-da-aula](../../specs/067-link-da-aula/spec.md) · **Card Trello:** "Link da aula na expansão da aula (opcional)"

## Pedido

A professora cola o link da reunião (Meet, Zoom etc.) nos detalhes da aula. O link **não vai para o banco**: fica só na memória do servidor, vale até o fim da aula, e é incluído no e-mail de lembrete da aula (o que já existia) quando houver.

## O que foi feito

**Backend**
- `LinkAulaTemporario` (singleton, memória): guarda `(link, validade)` por aula; expira sozinho na leitura e limpa os vencidos a cada chamada; nunca registra o link em log.
- `RegraLinkAula`: a aula só aceita link se está ativa, não cancelada e ainda não terminou (fim = data + hora final, fuso de São Paulo). Mesma ideia da `RegraChamada` (064).
- `PUT /api/aulas/{id}/link` e `DELETE /api/aulas/{id}/link` (idempotente). Validação: http/https, sem espaços, até 500 caracteres. 404 para aula inexistente/excluída; 409 para aula cancelada ou já terminada.
- `AulaResponse` ganhou `linkAula` e `aceitaLinkAula`; quem decide se o link ainda vale é o servidor.
- Cancelar e excluir descartam o link; editar o horário renova a validade para o novo fim (e descarta se o novo fim já passou); registrar a chamada mantém o link até o fim.
- `LembreteDispatcherService`: busca o link da **próxima aula da turma** uma vez por lembrete e, se existir, acrescenta um parágrafo "Link da aula" com o texto protegido (HTML-encode). Sem link, o corpo é idêntico ao anterior; qualquer falha na busca só tira o link, nunca impede o envio. Regras de envio (filtros, orçamento, tentativas, status) intactas.
- Nenhuma migração e nenhuma alteração em Domain, Persistence ou `database/`.

**Frontend** (sem componente novo)
- `lib/api/aulas.ts`: `linkAula`, `aceitaLinkAula`, `definirLinkAula`, `removerLinkAula`; `apiDelete` passou a aceitar um tipo de retorno (padrão `void`, chamadores atuais inalterados).
- Detalhes da aula: bloco "Link da aula" (link clicável em nova aba, Trocar/Remover, campo + "Salvar link", aviso fixo de que o link é temporário e se perde se o sistema reiniciar). Erros do servidor aparecem em toast.
- Popup de detalhes (Home/Agenda): link clicável, somente leitura.

**Documentação:** spec/plan/research/tasks/analysis atualizados na reanálise; nota de Princípio V em `specs/001-lembretes-canal-email/spec.md`.

## Validação

| Verificação | Resultado |
|---|---|
| `dotnet test tests/SPI.Application.Tests` | **570 aprovados**, 0 falhas (eram 507 antes do card; nenhum `Assert` existente alterado) |
| Testes novos | `LinkAulaTemporarioTests`, `RegraLinkAulaTests`, `DefinirLinkAulaRequestValidatorTests`, `AulaServiceLinkTests`, `LembreteDispatcherLinkTests` (corpo com/sem link, expirado, outra aula, falha de banco, caracteres especiais, regra de envio idêntica, uma busca por lembrete) |
| `tsc --noEmit` e `eslint` nos arquivos tocados | limpos |
| `npm run verificar-contraste` | 48 combinações, 0 abaixo de 4,5:1 |
| `npm run build` (produção) | ok; `next.config.ts` restaurado |
| Sem banco | `git status --porcelain -- database` vazio; `git diff --stat` de Domain/Persistence/database vazio; nenhuma ocorrência de `LinkAula`/`link_aula` nessas pastas |
| "MUST NOT" (CLAUDE.md) | arquivos novos conferidos por `git status --porcelain` (só os previstos); sem `try/catch` novo em controller; diff do frontend sem `new Date`/`Date.now`/`new URL`/`startsWith`/`https?://`; diff do dispatcher só traz a busca do link e o parágrafo opcional |
| Log | o link não aparece em nenhum `Log*` (só o id da aula); o `UseSerilogRequestLogging` padrão não registra corpo |
| API real | sobe; `PUT`/`DELETE /api/aulas/{id}/link` e `GET /api/aulas` respondem 401 sem token; sem erro de injeção do armazenamento no dispatcher |

## O que ficou de fora / pendente

- O corpo real do e-mail com o link foi conferido só pelos testes automatizados (`LembreteDispatcherLinkTests`); não houve envio de verdade (servidor SMTP desviado de propósito).
- A aula curta de 2 min do quickstart foi validada na API real; o popup da Home/Agenda não teve captura própria (usa o mesmo link e a mesma cor do bloco dos detalhes).
- Nada foi commitado nem enviado (push), conforme combinado.

## Validação ponta a ponta (2026-11-04, API e Chrome reais)

Com login na conta da professora, dados sintéticos `TESTE-067`, API local e frontend de produção:

| Passo | Resultado |
|---|---|
| PUT link válido; GET por id e na lista | 200; `linkAula` nos dois |
| `ftp://`, `javascript:`, `abc`, com espaço, 500+ caracteres | 400 cada; link anterior preservado |
| PUT em aula cancelada / já terminada | 409 / 409 |
| PUT em aula excluída / inexistente | 404 / 404 |
| DELETE do link duas vezes | 200 / 200, `linkAula` vazio |
| Cancelar a aula com link | link descartado, `aceitaLinkAula = false` |
| Mover a aula para outro dia | link mantido; mover para o passado é recusado (409) e nada muda |
| Aula que terminou durante o teste (~2 min) | `linkAula` vazio e `aceitaLinkAula = false` depois do fim |
| Reiniciar a API | links das aulas futuras apagados (como previsto) |
| Logs da API | nenhuma ocorrência do link |
| Banco | sem coluna de link; total de aulas igual ao baseline (15) após a limpeza |
| Chrome (claro e escuro) | sem link: campo + aviso; link inválido: mensagem do servidor; salvar, trocar e remover funcionam; link abre em nova aba (`noopener noreferrer`); aula terminada sem o bloco |

**Defeito achado e corrigido na validação:** o link usava o azul padrão do navegador (ilegível no modo escuro) e o aviso saía em fonte grande porque a classe `hint` não existe. Passaram a usar `--c-accent-text` e `--c-text-muted`, e o build foi refeito e reconferido.

Limpeza: dados `TESTE-067` apagados (1 turma, 1 aluno, 9 aulas), com backup feito antes em `database/backup/spi_db_20261104_093619.sql`; servidores parados. Observação fora do escopo: o texto "A chamada ficará disponível..." da spec 064 usa a mesma classe `hint` inexistente e aparece sem estilo.

## Risco aceito

O link vive só na memória da API: **reiniciar o sistema apaga os links** das aulas que ainda não aconteceram, e a professora precisa colar de novo. A tela avisa isso. É a consequência direta da decisão de não gravar em banco.
