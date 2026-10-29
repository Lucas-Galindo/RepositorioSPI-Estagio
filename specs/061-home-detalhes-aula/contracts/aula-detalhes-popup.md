# Contrato: componente `AulaDetalhesPopup`

Arquivo: `frontend/components/aulas/AulaDetalhesPopup.tsx`.

## Props

| Prop | Tipo | Uso |
|---|---|---|
| `aula` | `Aula` | A aula a exibir (já carregada pela tela que abre o pop-up). |
| `onFechar` | `() => void` | Chamada por "Fechar", clique fora do pop-up e Esc. A tela que abriu zera o estado. |

## Conteúdo

- Título: a matéria da aula. Subtítulo: o status, em `StatusBadge`.
- Bloco de detalhes:
  - Data e horário ("DD/MM/AAAA, HH:mm — HH:mm");
  - Turma: o nome da turma, ou "Atendimento individual";
  - Alunos: um item por aluno, com RA. Aula realizada: "Presente", "Faltou" ou "Pacote esgotado". Reposição: marca "Reposição".
  - Descrição: só quando existir.
- Rodapé: "Fechar" e "Abrir aula" (vai para `/aulas/{id}`).

## Comportamento

- **Teclado**: Esc fecha. O foco inicial fica em "Abrir aula". Ao fechar, o foco volta para o elemento que abriu o pop-up.
- **Semântica**: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` ligado ao título.
- **Somente leitura**: nenhum botão de editar, cancelar, excluir ou registrar presença.
- **Sem rede**: nenhuma chamada à API.
