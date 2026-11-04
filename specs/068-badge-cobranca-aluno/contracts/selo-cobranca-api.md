# Contrato: selo de cobrança (spec 068)

Nenhum endpoint novo. Três respostas existentes ganham um campo opcional `cobranca` (camelCase no JSON). Autenticação e perfil (`Professor`) não mudam.

## Objeto `cobranca`

```json
{
  "categoria": "Pacote",
  "valor": 300.00,
  "turmaNome": "Turma A",
  "progresso": { "total": 10, "totalEstimado": false, "aulasDadas": 7, "aulasRestantes": 3, "estado": "Atencao" },
  "avisoPacoteEsgotado": false,
  "outrosVinculos": 1,
  "detalhes": [
    { "categoria": "Pacote", "turmaNome": "Turma A", "valor": 300.00, "progresso": { "...": "..." } },
    { "categoria": "Avulsa", "turmaNome": null, "valor": 80.00, "progresso": null }
  ]
}
```

| Campo | Quando vem |
|---|---|
| `categoria` | sempre: `Mensalidade`, `Pacote`, `Avulsa` ou `SemVinculo` |
| `valor` | sempre (em `SemVinculo`, o valor da aula do aluno) |
| `turmaNome` | só no resumo da lista, quando o vínculo é de uma turma |
| `progresso` | só na categoria `Pacote` (mesmo formato da spec 058) |
| `avisoPacoteEsgotado` | `true` só na `Avulsa` que vale porque o pacote do grupo esgotou |
| `outrosVinculos`, `detalhes` | só no resumo da lista |

## `GET /api/alunos?incluirCobranca=true` — resumo da lista

Novo parâmetro de consulta **opcional** `incluirCobranca` (boolean, padrão `false`). Com `true`, cada item de `AlunoResponse` traz `cobranca` com o **resumo** (principal + `outrosVinculos` + `detalhes`). **Sem o parâmetro** (ou com `false`) a resposta é idêntica à de hoje: `cobranca` é `null` e nenhuma consulta extra é feita. Só a tela de Alunos envia `true`; as demais telas usam a lista só para escolher um aluno. Os demais endpoints de aluno (`GET /{id}`, cadastrar, atualizar…) **não** preenchem `cobranca` (`null`).

## `GET /api/turmas/{id}` — o que vale na turma

Cada aluno matriculado em `alunos[]` traz `cobranca` com o vínculo que **vale nessa turma** (sem `turmaNome`, `outrosVinculos` nem `detalhes`). `alunosComPacoteForaDaTurma[]` não traz `cobranca`.

## Aula (uma aula só) — o que vale para a aula

`GET /api/aulas/{id}` e os retornos de editar, participantes, registrar sessão e cancelar trazem, em cada item de `alunos[]` (inclusive reposição), `cobranca` com o vínculo que **vale para a aula** (turma da aula; aula individual = aulas individuais). `GET /api/aulas` (lista) **não** preenche.

## Garantias

- **Sem consulta por aluno**: no máximo duas consultas por resposta (vínculos ativos em lote e presenças dos pacotes em lote).
- **Mesma regra da cobrança**: `categoria` e `avisoPacoteEsgotado` do grupo coincidem com `RegraCobrancaPresenca.Decidir`.
- **Somente leitura**: nenhuma dessas respostas altera dado.
