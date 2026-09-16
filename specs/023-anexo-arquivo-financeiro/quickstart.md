# Quickstart: Validando o Anexo de Arquivo em Contas a Pagar e a Receber

## Pré-requisitos

- Backend (`SPI.Api`) e frontend (`frontend/`) rodando localmente, com o script
  `database/12_anexo_comprovante_financeiro.sql` já aplicado no banco de desenvolvimento.
- Login como `Professor` (mesmo acesso já usado hoje para Contas a Pagar/Receber).
- Pelo menos um registro existente de Contas a Pagar e um de Contas a Receber, sem anexo.
- Um arquivo JPG ou PNG de teste (< 10MB), um PDF de teste (< 10MB), um arquivo de outro formato
  (ex.: `.txt` ou `.docx`) e, se possível, um arquivo válido em tamanho mas maior que 10MB (ou
  simule reduzindo temporariamente o limite para testar mais rápido).

## Cenário 1 — Anexar o primeiro arquivo a um registro existente (User Story 1)

1. Abrir um registro de Contas a Pagar sem anexo (`/financeiro/contas-a-pagar/{id}`).
2. Clicar em "Anexar arquivo" e selecionar o PDF de teste.
3. Confirmar que o registro passa a exibir o anexo (nome do arquivo, ícone) com opções de
   visualizar/baixar e substituir.
4. Repetir os passos 1-3 para um registro de Contas a Receber (`/financeiro/contas-a-receber/{id}`).
5. Tentar anexar o arquivo de formato não aceito (`.txt`/`.docx`): confirmar que o sistema rejeita
   com uma mensagem clara e nenhum anexo é salvo.
6. Tentar anexar o arquivo maior que 10MB: confirmar rejeição com mensagem clara sobre o limite de
   tamanho.

Ver contrato de upload em [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md#post-apicontas-pagaridanexo-e-post-apipagamentosidanexo).

## Cenário 2 — Visualizar/baixar o anexo (User Story 2)

1. Num registro que já tem anexo (resultado do Cenário 1), clicar na opção de visualizar/baixar.
2. Confirmar que o arquivo abre/baixa com o mesmo conteúdo enviado (comparar tamanho/hash com o
   arquivo original, se possível).
3. Repetir para o anexo do registro de Contas a Receber.
4. Confirmar, no painel Network do navegador, que a listagem de Contas a Pagar/Receber (`GET
   /api/contas-pagar`, `GET /api/pagamentos`) não inclui o conteúdo binário do anexo — só o
   detalhe individual (`GET .../{id}`) traz os metadados (`anexo: {...}`), e o binário em si só é
   buscado ao clicar em visualizar/baixar.

## Cenário 3 — Oferta automática após salvar um novo registro (Clarifications, FR-013 a FR-015)

1. Criar um novo registro de Contas a Pagar (`/financeiro/contas-a-pagar/novo`) e salvar.
2. Confirmar que, imediatamente após salvar com sucesso, a interface oferece a opção de anexar um
   arquivo a esse registro recém-criado, sem exigir navegar de volta para procurá-lo.
3. Ignorar/fechar essa oferta e confirmar que o registro continua salvo normalmente, acessível
   depois pela tela de detalhe, e que o anexo pode ser adicionado ali a qualquer momento (mesmo
   comportamento do Cenário 1).
4. Repetir os passos 1-3 para um novo registro de Contas a Receber.

## Cenário 4 — Substituir um anexo existente, com confirmação (User Story 3, FR-016)

1. Num registro que já tem anexo, clicar em "Substituir".
2. Confirmar que aparece um aviso informando que o anexo atual será substituído e não poderá ser
   recuperado, **antes** de qualquer novo arquivo ser solicitado/enviado.
3. Cancelar o aviso e confirmar que nada muda — o anexo original continua exatamente como estava
   (visualizável/baixável, mesmo conteúdo).
4. Repetir a substituição, desta vez confirmando o aviso, e enviar um novo arquivo válido.
5. Confirmar que o novo arquivo passa a ser o anexo do registro, e que o arquivo anterior não está
   mais acessível (tentar baixar a versão antiga não deve ser possível por nenhum caminho da UI).
6. Repetir o passo 4 com um arquivo inválido (formato errado ou > 10MB) na confirmação: confirmar
   que a rejeição ocorre e o anexo anterior permanece inalterado.

## Validação de backend (sem UI)

Rodar os testes do validador de arquivo compartilhado (ver `tasks.md` para o teste específico):

```powershell
dotnet test tests/SPI.Application.Tests --filter "FullyQualifiedName~Anexos"
```

Ou testar os endpoints diretamente:

```
POST /api/contas-pagar/{id}/anexo   (multipart/form-data, campo "arquivo")
GET  /api/contas-pagar/{id}/anexo
POST /api/pagamentos/{id}/anexo
GET  /api/pagamentos/{id}/anexo
```

e confirmar que `GET /api/contas-pagar/{id}` / `GET /api/pagamentos/{id}` passam a incluir o
campo `anexo` (metadados, nunca o binário) depois de um upload bem-sucedido.
