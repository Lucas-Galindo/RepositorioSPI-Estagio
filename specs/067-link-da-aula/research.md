# Research: Link da aula temporário (spec 067)

Decisões baseadas na leitura do código (2026-11-03) e nas respostas do usuário (origem: a professora informa; guarda: memória do servidor; validade: fim da aula).

## R1 — Onde o link fica: um armazenamento próprio em memória, não `IMemoryCache`

**Decisão**: `ILinkAulaTemporario` (contrato em `SPI.Application/Aulas/Services/`) com a implementação `LinkAulaTemporario`, **singleton**, guardando `aulaId → (link, expiraEm)` num `ConcurrentDictionary`. Expiração conferida **na leitura** e limpeza das vencidas a cada gravação/leitura (volume mínimo: uma professora, dezenas de aulas).

**Por quê**:
- Tem de ser testável com relógio falso: o `IMemoryCache` do .NET decide a expiração pelo seu próprio relógio, difícil de controlar nos testes (a projeto usa fakes manuais e o `TimeProvider` da spec 064).
- Não exige pacote novo; mantém tudo na camada Application (que já referencia o framework do ASP.NET).
- Comportamento exato e simples: expirou → `null`.

**Alternativas descartadas**: (a) `IMemoryCache` com `AbsoluteExpiration` — expiração menos testável e comportamento de remoção por pressão de memória imprevisível; (b) banco ou arquivo — contraria o pedido do usuário; (c) `Session`/cookie — o e-mail é enviado por uma rotina em segundo plano, sem sessão.

## R2 — Contrato do armazenamento

```csharp
public interface ILinkAulaTemporario
{
    string? Obter(int aulaId);                       // null se nao existe ou expirou
    void Definir(int aulaId, string link, DateTime fimDaAula);   // fimDaAula em hora de Sao Paulo (de parede)
    void Remover(int aulaId);
}
```

A expiração absoluta vem de `fimDaAula` (data + hora de fim da aula, hora de parede de São Paulo) convertida com o ajudante de relógio. O link **nunca** é escrito em log (nem em exceções).

## R3 — Regra de elegibilidade (única, no backend)

Tipo estático puro `RegraLinkAula` (padrão de `RegraCobrancaPresenca`): `AceitaLink(Aula, DateTime agora)` = aula ativa **e** `Status != "Cancelada"` **e** `agora < DataInicio+HoraFim`; `FimDaAula(Aula)`. Usada por `AulaService` (recusa com 409 e preenche `AceitaLinkAula`) e pelo `Mapear` (só devolve o link se a aula ainda aceita). O frontend só exibe `aceitaLinkAula`.

## R4 — Validação do endereço

`DefinirLinkAulaRequestValidator` (FluentValidation, padrão do projeto, mensagens sem acento): obrigatório quando informado; `Uri.TryCreate(link, UriKind.Absolute, out var u)`, `u.Scheme` ∈ {`http`,`https`}; sem espaços (`link.Any(char.IsWhiteSpace)` falha); máx. 500 caracteres. A remoção usa `DELETE` (sem corpo), então não há "link vazio".

## R5 — API

- `PUT /api/aulas/{id}/link` com `{ "link": "https://..." }` → 200 com a `AulaResponse` atualizada. `400` validação, `404` aula inexistente/excluída, `409` aula que não aceita link (cancelada ou terminada).
- `DELETE /api/aulas/{id}/link` → 200 com a aula; idempotente (remover o que não existe é 200).
- `AulaResponse` ganha `linkAula` (`string?`) e `aceitaLinkAula` (`bool`), preenchidos por `Mapear` em **toda** resposta de aula (a lista também, porque o popup da Home/Agenda usa a aula já carregada da lista).
- As ações declaram `ProducesResponseType(400/404/409)`: o `ErrosDeNegocioFilter` da spec 065 só devolve 404/409 se declarados; **sem `try/catch` em controller**.

## R6 — Como o e-mail acha a aula (e o link)

O `Lembrete` é por turma e não guarda o id da aula, mas o horário dele é calculado a partir da **próxima aula agendada da turma** (`ILembreteRepository.ObterProximaAulaDaTurmaAsync`). No envio, `LembreteDispatcherService.ProcessarLembretesVencidosAsync` já recebe `lembreteRepository`; passa a chamar `ObterProximaAulaDaTurmaAsync(lembrete.TurmaId)` **uma vez por lembrete que vai ser enviado** e consulta o armazenamento. A função estática ganha o parâmetro opcional `ILinkAulaTemporario? links = null` (padrão nulo) para os testes atuais seguirem compilando e se comportando igual. O corpo do e-mail ganha um parágrafo **só** quando há link, com `WebUtility.HtmlEncode` no texto e no `href`; sem link, a string é a atual.

**Falha de qualquer parte desse caminho** (aula não encontrada, armazenamento sem o link): o e-mail sai sem link. Nenhuma exceção nova é possível no fluxo de envio.

**Limite conhecido**: se o lembrete atrasar e a "próxima aula" já for a seguinte, o link é o da seguinte (é a que o lembrete anuncia de qualquer forma).

## R7 — Relógio

Usa o ajudante da spec 064 (`RelogioExtensions.AgoraEmSaoPaulo(TimeProvider)` e o fuso `America/Sao_Paulo`). Como a 064 ainda não foi implementada, a tarefa de fundação **cria o arquivo se não existir** com o mesmo contrato (a 064 só o reaproveita) e registra `TimeProvider.System` no DI se ainda não estiver. A conversão hora de parede → `DateTimeOffset` usa o `TimeZoneInfo` de São Paulo, não o fuso do servidor.

## R8 — Ciclo de vida do link

| Evento | Ação |
|---|---|
| Passa o fim da aula | some sozinho (expiração na leitura) |
| `CancelarAsync`, `ExcluirAsync` da aula | `Remover(aulaId)` |
| `AtualizarAsync` (data/hora mudou) e há link | `Definir` de novo com o novo fim; se o novo fim já passou, `Remover` |
| Registrar chamada (`Realizada`) | **mantém** (a aula pode estar em andamento e a professora ainda quer ver o link) |
| Reinício da API | perde tudo (aceito; a tela avisa) |

## R9 — Frontend

- `lib/api/aulas.ts`: `Aula` ganha `linkAula: string | null` e `aceitaLinkAula: boolean`; funções `definirLinkAula(id, link, token)` e `removerLinkAula(id, token)`.
- `aulas/[id]/page.tsx`: bloco "Link da aula" — se `linkAula`, mostra `<a href target="_blank" rel="noopener noreferrer">` com botões "Trocar" e "Remover"; se vazio e `aceitaLinkAula`, mostra o campo e "Salvar link"; se `!aceitaLinkAula` e sem link, não mostra nada. Aviso fixo: "Este link fica guardado só até o fim da aula e se perde se o sistema reiniciar." Erros do servidor aparecem como no resto da tela (toast).
- `AulaDetalhesPopup.tsx`: mostra o link clicável quando existe; **somente leitura** (as ações ficam na página, como o popup já faz).
- A tela **não** valida o endereço, não decide elegibilidade nem expiração (FR-011); só repassa o texto.
- Reaproveita o padrão visual da tela (`mini-panel`, `field`, `btn`); nenhum componente novo é necessário.
- Segurança: o `href` só é renderizado se o servidor devolveu o link (já validado como http/https). O React escapa o texto.

## R10 — Testes

- `LinkAulaTemporarioTests` (relógio falso): define/obtém; expira exatamente no fim; remove; troca; limpeza.
- `RegraLinkAulaTests`: ativa/cancelada/terminada/excluída; instante exato do fim.
- `DefinirLinkAulaRequestValidatorTests`: válidos e inválidos (`ftp`, `javascript:`, vazio, espaço, 501 caracteres).
- `AulaServiceLinkTests`: define/troca/remove; 409 em cancelada e terminada; 404 em excluída; cancelar/excluir remove; editar horário atualiza validade; `Mapear` expõe `linkAula`/`aceitaLinkAula`.
- `LembreteDispatcherLinkTests`: com link → corpo contém o link protegido; sem link → corpo idêntico ao atual (comparação de texto exato); link expirado → sem link; regra de envio inalterada (status, tentativas); script/aspas no link ficam protegidos.
- Os testes atuais do dispatcher e do `AulaService` continuam sem alteração de `Assert` (parâmetros novos opcionais; os 5 construtores de `AulaService` ganham os novos argumentos, como na spec 064).

## R11 — Riscos

| Risco | Mitigação |
|---|---|
| Reinício perde links | Aceito pelo usuário; aviso na tela |
| Link com senha vaza em log | Nunca logado; `grep` de verificação nas tarefas |
| `javascript:` / HTML injection pelo link | Validação de esquema no servidor + `HtmlEncode` no e-mail + escape do React |
| Conflito com a spec 064 (mesmos arquivos: `AulaService`, `AulaResponse`, `aulas/[id]/page.tsx`) | Recomendar 064 antes; as edições são aditivas e em pontos distintos |
| Lembrete anuncia a aula "errada" se atrasar | Documentado; comportamento do cálculo atual do lembrete |
