# Data Model: Detalhes da Aula em Pop-up na Home (spec 061)

Sem mudança no banco, na API ou nas entidades. O pop-up usa o tipo `Aula` do frontend (`frontend/lib/api/aulas.ts`), que já traz:

- `id`, `materiaNome`, `turmaNome` (nulo na aula individual), `dataInicio`, `horaInicio`, `horaFim`, `status`, `descricao`;
- `alunos[]`: `alunoId`, `nome`, `ra`, `presente` (nulo até a aula ser registrada), `motivoNaoRegistro` e `reposicao`.

Estado novo, só na tela da Home: `aulaAberta: Aula | null` (a aula cujo pop-up está aberto).
