<#
.SYNOPSIS
    Gera um backup completo do banco spi_db via mysqldump (specs/050-backup-automatico-banco).

.DESCRIPTION
    Le a connection string dos .NET User Secrets do SPI.Api (nunca duplica a
    credencial em outro arquivo - Principio IV), gera um dump timestamped em
    database/backup/, aplica retencao de 8 backups (US2) e registra o
    resultado (sucesso ou falha) em database/backup/backup.log. Pensado para
    rodar tanto manualmente quanto via Tarefa Agendada do Windows
    (registrar-tarefa-agendada.ps1), independente da aplicacao estar aberta.

.NOTES
    Especifico de desenvolvimento/Windows - ver spec.md "Limitacoes aceitas"
    e "O que precisa evoluir antes de producao".
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$backupDir = Join-Path $repoRoot 'database\backup'
$logFile = Join-Path $backupDir 'backup.log'
$retencaoMaxima = 8

function Write-BackupLog {
    param([string]$Linha)
    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Add-Content -Path $logFile -Value "$timestamp $Linha"
}

try {
    # Edge Case (spec.md): a pasta pode ter sido removida ou ainda nao
    # existir na primeira execucao - nunca falhar por isso.
    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }

    # R1: resolve o UserSecretsId do proprio .csproj da API, sem hardcoded.
    $csprojPath = Join-Path $repoRoot 'src\SPI.Api\SPI.Api.csproj'
    if (-not (Test-Path $csprojPath)) {
        throw "Nao encontrei $csprojPath para resolver o UserSecretsId."
    }
    $csprojConteudo = Get-Content $csprojPath -Raw
    if ($csprojConteudo -notmatch '<UserSecretsId>([^<]+)</UserSecretsId>') {
        throw "UserSecretsId nao encontrado em $csprojPath."
    }
    $userSecretsId = $Matches[1]

    $secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\$userSecretsId\secrets.json"
    if (-not (Test-Path $secretsPath)) {
        throw "Arquivo de secrets nao encontrado em $secretsPath. Configure ConnectionStrings:SpiDb via 'dotnet user-secrets set' antes de rodar o backup."
    }
    $secrets = Get-Content $secretsPath -Raw | ConvertFrom-Json

    $connectionString = $secrets.'ConnectionStrings:SpiDb'
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "ConnectionStrings:SpiDb nao esta definido nos User Secrets ($secretsPath)."
    }

    $dbServer = $null; $dbPort = '3306'; $dbName = $null; $dbUser = $null; $dbPassword = $null
    foreach ($par in $connectionString.Split(';')) {
        if ([string]::IsNullOrWhiteSpace($par)) { continue }
        $chave, $valor = $par.Split('=', 2)
        switch ($chave.Trim().ToLowerInvariant()) {
            'server'   { $dbServer = $valor.Trim() }
            'port'     { $dbPort = $valor.Trim() }
            'database' { $dbName = $valor.Trim() }
            'user'     { $dbUser = $valor.Trim() }
            'password' { $dbPassword = $valor.Trim() }
        }
    }
    if (-not $dbServer -or -not $dbName -or -not $dbUser) {
        throw "Nao foi possivel extrair Server/Database/User da connection string."
    }

    # Resolve mysqldump.exe via PATH, com fallback para o caminho padrao de
    # instalacao ja usado no resto do projeto (ver memoria/tasks anteriores).
    $mysqldumpCmd = Get-Command 'mysqldump.exe' -ErrorAction SilentlyContinue
    $mysqldumpPath = if ($mysqldumpCmd) { $mysqldumpCmd.Source } else { 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe' }
    if (-not (Test-Path $mysqldumpPath)) {
        throw "mysqldump.exe nao encontrado (nem no PATH, nem em $mysqldumpPath)."
    }

    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $nomeArquivo = "spi_db_$timestamp.sql"
    $caminhoArquivo = Join-Path $backupDir $nomeArquivo

    # Senha passada via variavel de ambiente MYSQL_PWD (nunca em texto plano
    # na linha de comando/arquivo novo) - mesmo padrao ja usado no projeto.
    $env:MYSQL_PWD = $dbPassword
    try {
        & $mysqldumpPath --host=$dbServer --port=$dbPort --user=$dbUser $dbName > $caminhoArquivo 2>"$caminhoArquivo.stderr"
        $exitCodeDump = $LASTEXITCODE
    } finally {
        Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
    }

    $stderrPath = "$caminhoArquivo.stderr"
    $stderrConteudo = if (Test-Path $stderrPath) { Get-Content $stderrPath -Raw } else { '' }
    if (Test-Path $stderrPath) { Remove-Item $stderrPath -Force }

    if ($exitCodeDump -ne 0 -or -not (Test-Path $caminhoArquivo) -or (Get-Item $caminhoArquivo).Length -eq 0) {
        if (Test-Path $caminhoArquivo) { Remove-Item $caminhoArquivo -Force -ErrorAction SilentlyContinue }
        $mensagemErro = if ($stderrConteudo) { $stderrConteudo.Trim() } else { "mysqldump terminou com codigo $exitCodeDump" }
        throw "Falha ao gerar o dump: $mensagemErro"
    }

    Write-BackupLog "OK $nomeArquivo"

    # US2 (FR-006, research.md R3): retencao simples por ordenacao do nome
    # (yyyyMMdd_HHmmss ordena igual como texto e como data).
    $backupsExistentes = Get-ChildItem -Path $backupDir -Filter 'spi_db_*.sql' | Sort-Object Name
    if ($backupsExistentes.Count -gt $retencaoMaxima) {
        $paraRemover = $backupsExistentes | Select-Object -First ($backupsExistentes.Count - $retencaoMaxima)
        foreach ($arquivo in $paraRemover) {
            Remove-Item $arquivo.FullName -Force
            Write-BackupLog "RETENCAO removido $($arquivo.Name)"
        }
    }

    Write-Output "Backup gerado com sucesso: $caminhoArquivo"
    exit 0
}
catch {
    Write-BackupLog "FALHA $($_.Exception.Message)"
    Write-Error "Falha ao gerar backup: $($_.Exception.Message)"
    exit 1
}
