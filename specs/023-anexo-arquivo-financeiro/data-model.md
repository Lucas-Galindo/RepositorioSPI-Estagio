# Data Model: Anexo de Arquivo em Contas a Pagar e a Receber

Nenhuma tabela nova — 5 colunas novas adicionadas a cada uma das duas tabelas já existentes
(`conta_pagar` e `pagamento`), conforme decisão em [research.md](./research.md) #1. Nenhuma outra
tabela ou entidade do sistema é alterada.

## Colunas novas (idênticas em `conta_pagar` e em `pagamento`)

| Coluna | Tipo MySQL | Nullable | Descrição |
|---|---|---|---|
| `arquivo_conteudo` | `MEDIUMBLOB` | Sim (NULL = sem anexo) | Conteúdo binário do arquivo, exatamente como enviado (FR-005, FR-009) |
| `arquivo_nome_original` | `VARCHAR(255)` | Sim | Nome do arquivo como o usuário o enviou (ex.: `nota-fiscal-setembro.pdf`) — usado na indicação visual (FR-008) e no download |
| `arquivo_tipo_mime` | `VARCHAR(100)` | Sim | Tipo MIME detectado a partir do conteúdo real (`image/jpeg`, `image/png` ou `application/pdf` — nunca o `Content-Type` declarado pelo navegador, ver FR-002) |
| `arquivo_tamanho_bytes` | `INT UNSIGNED` | Sim | Tamanho em bytes do conteúdo — sempre ≤ 10.485.760 (10MB), garantido pela validação da aplicação antes da gravação (FR-003) |
| `arquivo_data_upload` | `DATETIME` | Sim | Data/hora em que o anexo atual foi salvo — atualizada a cada substituição (FR-006), nunca preserva a data do anexo anterior |

As 5 colunas são NULL simultaneamente quando o registro não tem anexo, e todas preenchidas
simultaneamente quando tem — nunca um subconjunto parcial (a aplicação sempre grava/lê as 5
juntas, nunca uma isoladamente).

## Entidade lógica: Anexo de Comprovante

Não é uma entidade EF Core própria — é a projeção lógica das 5 colunas acima em cada uma das duas
entidades já existentes (`ContaPagar`, `Pagamento`). Descrita como entidade separada aqui apenas
porque o spec a trata como um conceito de negócio distinto (ver spec.md "Key Entities").

| Campo lógico | Origem | Regra |
|---|---|---|
| Conteúdo binário | `arquivo_conteudo` | Nunca exposto em nenhuma resposta JSON — só entregue pelo endpoint de download dedicado, como corpo binário puro (não em base64), ver [contracts/](./contracts/) |
| Nome original | `arquivo_nome_original` | Exibido na indicação visual (FR-008) e usado como nome sugerido de download |
| Tipo | `arquivo_tipo_mime` | Determina o `Content-Type` da resposta de download e a validação de formato aceito |
| Tamanho | `arquivo_tamanho_bytes` | Exposto nos metadados para a UI poder mostrar, por exemplo, "1,2 MB" ao lado do nome |
| Data do upload | `arquivo_data_upload` | Exposta nos metadados; não é editável pelo usuário |

### Regras de negócio

- **Substituição (FR-006)**: anexar um novo arquivo é sempre um `UPDATE` das 5 colunas na mesma
  linha do registro — o valor anterior de `arquivo_conteudo` é fisicamente sobrescrito pela nova
  gravação; não existe estado intermediário em que as duas versões coexistem, nem versão anterior
  recuperável depois (por isso a confirmação de FR-016 acontece antes do envio, no frontend).
- **Validação (FR-002, FR-003)**: antes de qualquer `UPDATE`, o `AnexoValidator` (ver
  research.md #3) confirma tamanho ≤ 10MB e assinatura binária correspondente a JPG/PNG/PDF. Se a
  validação falhar, nenhuma coluna é tocada — o anexo anterior (se houver) permanece exatamente
  como estava (FR-007).
- **Opcionalidade (FR-011)**: as 5 colunas são sempre `NULL` até o primeiro anexo; nenhum fluxo de
  criação/edição do registro em si depende delas ou é bloqueado por sua ausência.
- **Sem exclusão física do registro pai**: como `ContaPagar`/`Pagamento` já usam exclusão lógica
  por `Status` (nunca são fisicamente removidos pela aplicação hoje), o anexo vinculado a um
  registro "Cancelado" continua existindo e acessível — nenhuma lógica nova de expiração/limpeza é
  introduzida por esta feature.

## Migração de dados existentes

Registros já existentes de `conta_pagar` e `pagamento` recebem as 5 colunas novas como `NULL`
(sem anexo) — nenhum dado pré-existente é afetado, migrado ou precisa de preenchimento retroativo.
