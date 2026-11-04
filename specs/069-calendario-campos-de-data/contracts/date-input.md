# Contrato: componente `DateInput` (spec 069)

Arquivo: `frontend/components/shared/DateInput.tsx` (existente; sem componente novo).

## Propriedades

| Propriedade | Tipo | Mudança |
|---|---|---|
| `value` | `string` (`yyyy-MM-dd` ou vazio) | igual |
| `onChange` | `(value: string) => void` | igual: recebe `yyyy-MM-dd` ou vazio |
| `required`, `id`, `className`, `title`, `disabled` | opcionais | iguais (`className` continua no campo de texto) |
| `min` | `string` (`yyyy-MM-dd`) opcional | **novo**: repassado ao auxiliar nativo, para o calendário não deixar escolher dias anteriores (usado em "Fim da turma", que tinha `min` no campo nativo); o campo de texto não bloqueia a digitação (o servidor valida) |
| `ariaLabel` | `string` opcional | **novo**: rótulo de acessibilidade do campo de texto |
| `style` | `CSSProperties` opcional | **novo**: estilo do invólucro (ex.: `flex`, `minWidth` em linhas de tabela) |

## Estrutura renderizada

```text
<span class="date-wrap" style=...>
  <input type="text" class="date-input [className]" ... />          campo mascarado dd/mm/aaaa (igual a hoje)
  <button type="button" class="date-cal-btn" aria-label="Abrir calendário" [disabled]> ícone cal </button>
  <input type="date" class="date-nativo" tabindex="-1" aria-hidden="true" [min] ... />   auxiliar invisível
</span>
```

## Comportamento

- Digitar: igual a hoje (máscara, `onChange` só com data completa válida ou vazio).
- Clicar no botão: abre o calendário do navegador (`showPicker()` do auxiliar; sem suporte, só foca o auxiliar, sem erro).
- Escolher um dia no calendário: `onChange("yyyy-MM-dd")` com o valor entregue pelo navegador; o texto mostra `dd/mm/aaaa` (mesma sincronização por mudança de `value` que já existe).
- `disabled`: o campo, o botão e o auxiliar ficam desabilitados.
- O auxiliar recebe a data do **texto exibido** só quando ela está completa; texto vazio, parcial ou inválido deixa o auxiliar vazio (o calendário abre em hoje) sem mexer no que foi digitado.

## CSS (`styles/dashboard.css`)

- `.date-wrap`, `.date-cal-btn`, `.date-nativo` (novos seletores, só tokens de cor).
- `color-scheme: light` / `.dark-mode …: dark` em `.date-nativo` e em `input[type="month"]`.
- `.field .date-wrap { display: block }` e `.field .date-wrap .date-input { width: 100% }` para formulários: o `.field` é uma coluna flex que estica os filhos diretos, e o campo agora está dentro do invólucro.
- O auxiliar usa o seletor `.date-wrap .date-nativo` (mais específico que `.field input`), para o preenchimento e a sombra dos campos de formulário não o atingirem.

## Garantias

- O contrato de valor (`yyyy-MM-dd`) não muda: nenhuma tela precisa mudar a leitura do valor.
- Nenhuma conversão com `Date`; nenhuma cor literal.
