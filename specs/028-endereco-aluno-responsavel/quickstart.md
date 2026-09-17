# Quickstart: Validar Endereço do Aluno e do Responsável

Guia de validação manual (não há framework de teste automatizado no projeto — ver plan.md
Technical Context). Rodar após a implementação (tasks.md), com backend e frontend rodando
localmente.

## Pré-requisitos

- Backend (`dotnet run` em `src/SPI.Api`) e frontend (`npm run dev` em `frontend/`) rodando.
- Migração `database/13_endereco_aluno_responsavel.sql` aplicada no banco local.
- Login como Professora/Admin.
- Um CEP real válido à mão para testar o preenchimento automático (ex.: `01001000`, Praça da Sé
  — São Paulo).

## Cenário 1 — Cadastrar Aluno com endereço obrigatório (US1)

1. Ir para Alunos → Novo Aluno, preencher os campos básicos, deixar CEP/Rua/Número vazios e
   tentar salvar.
2. **Esperado**: o sistema rejeita e indica que CEP, Rua e Número são obrigatórios (FR-002).
3. Preencher CEP, Rua e Número (deixando Complemento/Bairro/Cidade/Estado vazios) e salvar.
4. **Esperado**: o cadastro é aceito (FR-002, SC-001).
5. Abrir o detalhe desse Aluno.
6. **Esperado**: o endereço aparece com os campos preenchidos (FR-003, SC-003).

## Cenário 2 — Endereço do responsável com "mesmo endereço" marcado (US2)

1. Cadastrar (ou editar) um Aluno marcando "é menor de idade".
2. **Esperado**: aparece a seção "Endereço do responsável" com o checkbox "Mesmo endereço do
   aluno" já marcado (FR-004, FR-005).
3. Preencher o endereço do Aluno e salvar sem tocar na seção do responsável.
4. **Esperado**: o cadastro é aceito; ao reabrir o detalhe, o endereço do responsável aparece
   idêntico ao do Aluno (FR-006, SC-002).
5. Editar o endereço do Aluno (ex.: trocar o Número) e salvar.
6. **Esperado**: o endereço do responsável exibido também reflete o novo Número — continuam
   sincronizados enquanto o checkbox estiver marcado (research.md Decisão 2).

## Cenário 3 — Endereço do responsável independente (US2)

1. Editar um Aluno menor de idade, desmarcar "Mesmo endereço do aluno".
2. **Esperado**: os 7 campos de endereço do responsável aparecem vazios e editáveis,
   independentes dos do Aluno (FR-007).
3. Preencher um endereço diferente do endereço do Aluno e salvar.
4. **Esperado**: o cadastro é aceito; no detalhe, os dois endereços aparecem diferentes entre si
   (FR-007, SC-002).
5. Editar o endereço do Aluno de novo (sem tocar no do responsável).
6. **Esperado**: o endereço do responsável permanece exatamente como estava — não muda junto
   (diferente do Cenário 2, porque o checkbox está desmarcado).

## Cenário 4 — Seção de responsável some quando não é menor (US2)

1. Abrir o formulário de um Aluno que NÃO está marcado como menor de idade.
2. **Esperado**: nenhuma seção de endereço de responsável é exibida (FR-008).
3. Marcar "é menor de idade" nesse mesmo formulário (sem salvar ainda).
4. **Esperado**: a seção de endereço do responsável aparece imediatamente, com "mesmo endereço"
   marcado por padrão.

## Cenário 5 — Reabrir edição de um Aluno menor de idade já cadastrado

1. Cadastrar um Aluno menor de idade com endereço de responsável independente (Cenário 3).
2. Navegar para outra tela e voltar para editar esse mesmo Aluno.
3. **Esperado**: o formulário já abre com "é menor de idade" marcado e a seção de endereço do
   responsável visível com os dados salvos — sem precisar remarcar manualmente (research.md
   Decisão 1, corrige uma falha pré-existente do formulário).

## Cenário 6 — Preenchimento automático por CEP funciona (US3)

1. No campo CEP do endereço do Aluno, digitar um CEP válido e existente (ex.: `01001000`).
2. **Esperado**: assim que o 8º dígito é digitado, Rua, Bairro, Cidade e Estado são preenchidos
   automaticamente (FR-010, SC-004).
3. Editar manualmente o campo Rua preenchido automaticamente.
4. **Esperado**: a edição é aceita, sem o valor voltar sozinho para o que veio da busca (FR-011).
5. Repetir os passos 1-2 no campo CEP do endereço do responsável (com "mesmo endereço"
   desmarcado).
6. **Esperado**: mesmo comportamento, de forma independente do endereço do Aluno (FR-013).

## Cenário 7 — Falha na busca de CEP não bloqueia o cadastro (US3)

1. Desconectar a internet (ou usar um CEP inexistente, ex.: `00000000`) e digitar no campo CEP.
2. **Esperado**: nenhum erro bloqueante aparece; os campos de Rua/Bairro/Cidade/Estado
   continuam vazios e editáveis manualmente (FR-012, SC-005).
3. Preencher os campos manualmente e salvar o cadastro.
4. **Esperado**: o cadastro é aceito normalmente, sem exigir que a busca automática tenha
   funcionado.

## Cenário 8 — Alunos antigos sem endereço continuam editáveis

1. Localizar (ou simular, se necessário) um Aluno cadastrado antes desta feature, sem endereço.
2. Editar apenas um campo não relacionado a endereço (ex.: Valor da aula) e salvar, sem
   preencher CEP/Rua/Número.
3. **Esperado**: o sistema aceita a alteração normalmente — não força o preenchimento de
   endereço para editar outros campos (Edge Case da spec, research.md Decisão 3).
