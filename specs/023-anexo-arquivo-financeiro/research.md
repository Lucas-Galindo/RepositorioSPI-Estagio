# Research: Anexo de Arquivo em Contas a Pagar e a Receber

Nenhum `NEEDS CLARIFICATION` permaneceu no Technical Context do plan.md — o spec já resolveu, na
sessão de `/speckit-clarify` de 2026-09-16, os dois pontos de maior impacto (momento do anexo em
relação à criação do registro, e confirmação antes de substituir). As decisões abaixo cobrem os
pontos de design técnico que a fase de planejamento precisou fechar para desenhar
`data-model.md`/`contracts/`.

## 1. Onde guardar o BLOB: colunas diretas nas tabelas existentes, ou tabela `anexo` separada?

- **Decision**: 5 colunas novas adicionadas diretamente em `conta_pagar` e em `pagamento`
  (`arquivo_conteudo MEDIUMBLOB NULL`, `arquivo_nome_original VARCHAR(255) NULL`,
  `arquivo_tipo_mime VARCHAR(100) NULL`, `arquivo_tamanho_bytes INT UNSIGNED NULL`,
  `arquivo_data_upload DATETIME NULL`) — não uma tabela `anexo_comprovante` separada.
- **Rationale**: A relação é estritamente um-para-um e opcional (spec: "cada registro tem no
  máximo um anexo"). Uma tabela separada exigiria uma chave estrangeira única por linha
  (`conta_pagar_id` OU `pagamento_id`, nunca os dois), o que em MySQL não tem um jeito limpo de
  impor via `CHECK` simples sem duplicar a tabela ou usar uma coluna discriminadora — complexidade
  desnecessária para um relacionamento 1:1 opcional. O próprio pedido do usuário já oferecia essa
  opção como primeira escolha ("na própria tabela do registro"). Colunas diretas também tornam a
  substituição de anexo (FR-006) uma única operação `UPDATE` na mesma linha — sem exigir um
  `DELETE` em outra tabela nem lidar com "linha órfã" alguma, o que combina diretamente com a
  motivação de simplicidade operacional já registrada no spec (Assumptions).
- **Alternatives considered**:
  - Tabela `anexo_comprovante` com FK nullable para `conta_pagar_id` e `pagamento_id` → rejeitada:
    exigiria lógica de aplicação para garantir "exatamente um dos dois preenchido" (sem suporte
    nativo simples no MySQL usado pelo projeto), e uma consulta a mais (JOIN) em toda tela de
    detalhe só para saber se há anexo — sem nenhum ganho, já que a relação nunca é 1:N.
  - Duas tabelas `anexo_conta_pagar` / `anexo_pagamento` separadas, cada uma com FK única não-nula
    → rejeitada: resolve a ambiguidade do ponto acima, mas ainda exige um JOIN a mais por tela sem
    necessidade real, e replica a mesma estrutura de colunas duas vezes só para evitar 5 colunas
    "a mais" nas tabelas já existentes — não há economia real de esquema.
- **Tamanho da coluna binária**: `MEDIUMBLOB` (até ~16MB) em vez de `LONGBLOB` (até 4GB) — folga
  suficiente acima do limite de 10MB (FR-003) sem exagerar a capacidade máxima teórica da coluna
  para um caso de uso que nunca deveria passar de 10MB (a validação do tamanho acontece na
  aplicação antes de qualquer gravação, então o teto da coluna é só uma rede de segurança).

## 2. Como transportar o upload e o download pela API

- **Decision**: Upload via `multipart/form-data` com `[FromForm] IFormFile` (recurso nativo do
  ASP.NET Core, nenhum pacote novo) num endpoint `POST` dedicado por entidade (`POST
  /api/contas-pagar/{id}/anexo`, `POST /api/pagamentos/{id}/anexo`), reaproveitado tanto para o
  primeiro anexo quanto para a substituição (o mesmo endpoint sempre sobrescreve — a confirmação
  de FR-016 é responsabilidade do frontend, não da API). Download via `GET
  /api/contas-pagar/{id}/anexo` / `GET /api/pagamentos/{id}/anexo`, devolvendo o binário com o
  `Content-Type` original e `Content-Disposition: inline; filename="..."` (permite que o navegador
  exiba imagens/PDF diretamente numa aba nova, com a opção de salvar já disponível nos controles
  nativos do navegador — cobre "visualizar" e "baixar" com uma única resposta).
- **Rationale**: `multipart/form-data` é o padrão idiomático do ASP.NET Core para upload de
  arquivo e não exige codificar o binário em base64 (que infla ~33% o tamanho da requisição e
  complicaria o limite de 10MB). O frontend não tem hoje nenhum helper de upload em
  `lib/api/client.ts` (só `apiGet`/`apiPost`/`apiPut`/`apiDelete`, todos JSON) — dois helpers novos
  são necessários: um que envia `FormData` sem forçar `Content-Type: application/json`, e um que
  lê a resposta como `Blob` em vez de fazer `JSON.parse`.
- **Alternatives considered**:
  - Enviar o arquivo como base64 dentro de um JSON (reaproveitando `apiPost` tal como está) →
    rejeitado: infla o payload em ~33%, tornando o limite de 10MB do arquivo original mais difícil
    de mapear para um limite de requisição, e não é o padrão idiomático de upload do ASP.NET Core.
  - Servir o download como uma URL pré-assinada/redirecionamento → não aplicável: não há storage
    externo (decisão do spec), então não existe URL de terceiros para assinar; o próprio backend
    sempre serve o binário diretamente do banco.

## 3. Onde centralizar a validação de formato/tamanho (Princípio II da constituição)

- **Decision**: Um serviço novo e pequeno, `SPI.Application/Anexos/Services/AnexoValidator.cs`
  (`IAnexoValidator`), chamado tanto por `ContaPagarService.AnexarArquivoAsync` quanto por
  `PagamentoService.AnexarArquivoAsync` antes de gravar qualquer coisa. Valida: (a) tamanho ≤
  10MB; (b) assinatura binária (magic bytes) do conteúdo real corresponde a JPG, PNG ou PDF — não
  a extensão do nome do arquivo nem o `Content-Type` declarado pelo navegador (FR-002, Edge Case
  de arquivo renomeado).
- **Rationale**: Formato e tamanho aceitos são exatamente a mesma regra nos dois pontos de entrada
  (Contas a Pagar e Contas a Receber) — duplicá-la em `ContaPagarService` e `PagamentoService`
  violaria o Princípio II (mesmo raciocínio já aplicado a `AplicarFiltroReceita` em specs
  anteriores). A verificação de assinatura binária (magic bytes) é poucas linhas de comparação de
  bytes (`FF D8 FF` para JPEG, `89 50 4E 47 0D 0A 1A 0A` para PNG, `25 50 44 46 2D` — "%PDF-" —
  para PDF) e não exige nenhuma biblioteca externa de detecção de tipo de arquivo, mantendo a
  mesma preferência do projeto por evitar dependências novas quando uma implementação pequena e
  direta resolve (mesma decisão tomada em specs/022 ao evitar Moq/NSubstitute).
- **Alternatives considered**:
  - Duplicar a validação em cada service → rejeitado: viola diretamente o Princípio II.
  - Adicionar um pacote de detecção de tipo de arquivo (ex.: uma lib de "magic number sniffing")
    → rejeitado como desnecessário: os três formatos aceitos (JPG, PNG, PDF) têm assinaturas
    binárias simples e bem documentadas, verificáveis com poucas linhas de código sem dependência
    externa.

## 4. Limite de tamanho de requisição do servidor

- **Decision**: Nenhuma mudança de configuração é necessária. O limite padrão do Kestrel
  (`MaxRequestBodySize`, 30MB) já não é customizado em `src/SPI.Api/Program.cs` hoje, e 30MB
  comporta um upload de 10MB mais a sobrecarga do `multipart/form-data` (cabeçalhos, boundary)
  com folga.
- **Rationale**: Evita introduzir uma configuração nova sem necessidade real — o requisito de
  10MB (FR-003) é aplicado pela validação da aplicação (`AnexoValidator`), não por um limite de
  infraestrutura; o limite de infraestrutura já é maior que o necessário e não precisa ser tocado.
