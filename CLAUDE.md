# Regras do projeto para Claude Code

## Cobertura de verificação para requisitos "MUST NOT"

Todo requisito "MUST NOT" de uma spec (`spec.md`) precisa de uma tarefa de verificação explícita em `tasks.md` — não basta a implementação evitar a coisa proibida; precisa existir uma tarefa que *confirme* que ela não aconteceu. Esse é o achado mais recorrente das análises (`/speckit-analyze`) feitas neste projeto.

- **"Não criar arquivo/componente/tela novo"**: a tarefa de verificação MUST usar `git status --porcelain -- <pastas relevantes>` (confirmando que não há arquivos não rastreados) — nunca só `git diff --stat`, que **não mostra arquivos novos não rastreados** e dá falso positivo de conformidade.
- **"Não reordenar/recalcular/duplicar lógica no frontend"** (ou qualquer "MUST NOT" sobre lógica de código, não só aparência): a tarefa de verificação MUST incluir a leitura direta do diff do código correspondente, não só uma checagem visual na tela — uma tela pode parecer correta mesmo que a lógica proibida tenha sido introduzida (ex.: um `.sort()` que coincidentemente reproduz a ordem que já vinha certa do backend).
- Ao gerar `tasks.md` (`/speckit-tasks`), para cada FR "MUST NOT"/"MUST NOT introduzir"/equivalente, adicionar (ou estender uma tarefa de Polish já existente com) a verificação apropriada acima, em vez de deixar implícito que a implementação "só não vai fazer aquilo".
