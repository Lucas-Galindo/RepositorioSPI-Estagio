# Research: Backup Automático do Banco de Dados

Decisões técnicas da Phase 0. A decisão de produto (cadência, retenção, local, caráter temporário) já veio fechada da conversa que precedeu a spec; esta fase resolve os pontos de implementação.

## R1. Credencial de conexão: ler do `secrets.json` do .NET User Secrets, sem duplicar segredo

- **Decision**: o script resolve o `UserSecretsId` lendo `src/SPI.Api/SPI.Api.csproj` (regex simples pela tag `<UserSecretsId>...</UserSecretsId>`), monta o caminho `$env:APPDATA\Microsoft\UserSecrets\<id>\secrets.json`, lê o JSON, e extrai `ConnectionStrings:SpiDB` (hoje `Server=127.0.0.1;Port=3306;Database=spi_db;User=root;Password=...;`) — faz o parse dessa string de conexão para obter host/porta/usuário/senha/nome do banco, e passa isso como argumentos para `mysqldump`.
- **Rationale**: Princípio IV — a API já busca essa exata credencial desse exato lugar; criar um segundo arquivo de configuração (ex.: um `.env` separado para o script de backup) duplicaria o segredo em dois lugares que podem ficar dessincronizados (ex.: senha trocada só num dos dois). Ler do mesmo lugar garante uma única fonte de verdade.
- **Alternatives considered**:
  - Pedir usuário/senha como parâmetro do script, ou um arquivo de config novo (`backup.config.json`) — descartado: duplica o segredo, e exigiria o usuário manter dois lugares sincronizados manualmente.
  - Usar `my.cnf`/`.mylogin.cnf` do próprio MySQL para autenticação sem senha na linha de comando — mais "nativo" do MySQL, mas introduziria um terceiro mecanismo de credencial no projeto (nem User Secrets, nem appsettings) sem necessidade real, já que o User Secrets já resolve isso.

## R2. Mecanismo de agendamento: `Register-ScheduledTask` num script de registro, rodado uma vez

- **Decision**: `registrar-tarefa-agendada.ps1` usa o cmdlet nativo `Register-ScheduledTask` (módulo `ScheduledTasks`, já embutido no Windows) para criar uma tarefa que roda `powershell.exe -File backup-database.ps1` toda semana, num dia/horário padrão (domingo, 02:00 — janela de baixo uso), com um parâmetro opcional para o usuário escolher outro dia/horário. O usuário roda esse script de registro **uma vez** (como administrador, se exigido pela política local) — depois disso, o Windows cuida do agendamento sozinho, sem qualquer processo do projeto (API, `dotnet`, etc.) precisar estar rodando.
- **Rationale**: FR-002 explícito (funcionar independente da aplicação estar rodando) — já descartado no `spec.md` usar um `BackgroundService` por esse motivo. `Register-ScheduledTask` é scriptável e versionável (ao contrário de "abra o Agendador de Tarefas e configure manualmente pela interface"), então o passo de setup também fica documentado como código, não só como instrução em texto.
- **Alternatives considered**:
  - Instruir o usuário a configurar a Tarefa Agendada manualmente pela interface gráfica do Windows — descartado: não é reprodutível/versionável, e um passo manual multi-tela é mais sujeito a erro do que rodar um script uma vez.
  - `schtasks.exe` (linha de comando mais antiga, pré-PowerShell) em vez de `Register-ScheduledTask` — funcionalmente equivalente, mas `Register-ScheduledTask` é o cmdlet nativo mais moderno e já é PowerShell (consistente com o resto do script), sem motivo para usar a ferramenta mais antiga.

## R3. Nome do arquivo e retenção: timestamp no nome, contagem simples por ordenação

- **Decision**: cada backup é salvo como `database/backup/spi_db_<yyyyMMdd_HHmmss>.sql`. Depois de gerar um novo backup com sucesso, o script lista todos os arquivos `spi_db_*.sql` na pasta, ordena por nome (que já ordena cronologicamente, graças ao formato `yyyyMMdd_HHmmss`), e remove os mais antigos além dos 8 mais recentes.
- **Rationale**: FR-003 (nome único por data/hora) e FR-006 (retenção de 8) — o formato de nome `yyyyMMdd_HHmmss` ordena como string exatamente na mesma ordem que ordena como data, então não é preciso nem parsear data de volta do nome do arquivo para saber qual é o mais antigo — só ordenar os nomes como texto.
- **Alternatives considered**: usar a data de modificação do arquivo (`LastWriteTime`) em vez do nome — funcionalmente equivalente, mas o nome já carrega a informação de forma visível/legível (FR-003 já exige isso de qualquer forma), então reaproveitar o próprio nome para a ordenação evita uma segunda fonte da mesma informação.

## R4. Registro de falha: `backup.log` + código de saída não-zero

- **Decision**: toda execução do script (sucesso ou falha) acrescenta uma linha a `database/backup/backup.log` (`<timestamp> OK <nome-do-arquivo>` ou `<timestamp> FALHA <mensagem-de-erro>`); em caso de falha, o script termina com `exit 1` (ou código não-zero equivalente).
- **Rationale**: FR-007 (falha deve deixar registro identificável, sem notificação ativa). Um código de saída não-zero já faz o próprio Histórico do Agendador de Tarefas do Windows marcar aquela execução como falha (sem precisar de nenhum código adicional) — o `backup.log` complementa isso com uma forma mais rápida de checar sem abrir a interface do Agendador.
- **Alternatives considered**: só confiar no Histórico do Agendador de Tarefas, sem `backup.log` próprio — descartado: o Histórico do Agendador por padrão pode estar desabilitado em algumas instalações do Windows, e não é tão rápido de consultar quanto um arquivo de texto simples.

## R5. Nenhum teste automatizado (xUnit) — validação manual via `quickstart.md`

- **Decision**: sem testes xUnit — o script não faz parte da aplicação .NET testável (não há `IServiceCollection`/DI envolvido), e o projeto não tem framework de teste de scripts PowerShell configurado.
- **Rationale**: mesma decisão já aceita para mudanças de infraestrutura puramente operacional (ex.: os próprios scripts do Spec Kit/Trello não têm testes automatizados neste projeto); introduzir um framework de teste PowerShell (ex.: Pester) só para esta feature seria desproporcional.
- **Alternatives considered**: Pester (framework de teste para PowerShell) — descartado por desproporção; validação manual (`quickstart.md`) já cobre os cenários reais (gerar backup, forçar retenção, simular falha).
