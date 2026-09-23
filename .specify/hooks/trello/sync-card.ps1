<#
Sincroniza o card da feature no quadro Trello "Estagio SPI" conforme a fase do Spec Kit.
Contrato completo: specs/040-trello-sync-hooks/contracts/sync-card-cli.md
Nunca propaga falha: qualquer erro e' capturado, logado em sync.log, e o script sempre sai com 0
(FR-011) -- a sincronizacao com o Trello e' um extra, nunca pode travar o Spec Kit de verdade.
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('backlog', 'design', 'a-fazer', 'em-andamento', 'revisao-codigo')]
    [string]$Fase,

    [switch]$DryRun
)

$scriptDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..\..\..')).Path
$logPath = Join-Path $scriptDir 'sync.log'

function Write-SyncLog {
    param([string]$Fase, [string]$Result, [string]$Detail)
    $line = "[{0}] fase={1} resultado={2} detalhe={3}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Fase, $Result, $Detail
    try { Add-Content -Path $logPath -Value $line -Encoding UTF8 } catch { }
}

function Get-TrelloCredentials {
    $key = $env:TRELLO_API_KEY
    $token = $env:TRELLO_TOKEN
    $boardId = $env:TRELLO_BOARD_ID

    if (-not $key -or -not $token -or -not $boardId) {
        $envFile = Join-Path $scriptDir '.env'
        if (Test-Path $envFile) {
            foreach ($line in Get-Content $envFile) {
                if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$') {
                    $name = $Matches[1]
                    $value = $Matches[2]
                    switch ($name) {
                        'TRELLO_API_KEY' { if (-not $key) { $key = $value } }
                        'TRELLO_TOKEN' { if (-not $token) { $token = $value } }
                        'TRELLO_BOARD_ID' { if (-not $boardId) { $boardId = $value } }
                    }
                }
            }
        }
    }

    if (-not $key -or -not $token -or -not $boardId) {
        throw "Credenciais do Trello ausentes (TRELLO_API_KEY/TRELLO_TOKEN/TRELLO_BOARD_ID) -- configure variaveis de ambiente ou .specify/hooks/trello/.env (ver README.md)"
    }

    return @{ Key = $key; Token = $token; BoardId = $boardId }
}

function Get-FeatureId {
    $featureJsonPath = Join-Path $repoRoot '.specify/feature.json'
    if (-not (Test-Path $featureJsonPath)) { throw ".specify/feature.json nao encontrado" }
    $json = Get-Content $featureJsonPath -Raw | ConvertFrom-Json
    if (-not $json.feature_directory) { throw "feature_directory ausente em .specify/feature.json" }
    return ($json.feature_directory -replace '^specs[\\/]', '')
}

function Get-TrelloLists {
    param($Cred)
    $uri = "https://api.trello.com/1/boards/$($Cred.BoardId)/lists?key=$($Cred.Key)&token=$($Cred.Token)"
    $lists = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 15
    $map = @{}
    foreach ($l in $lists) { $map[$l.name] = $l.id }
    return $map
}

function Get-TrelloCards {
    param($Cred)
    $uri = "https://api.trello.com/1/boards/$($Cred.BoardId)/cards?key=$($Cred.Key)&token=$($Cred.Token)&fields=name,desc,idList"
    return Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 15
}

function Find-FeatureCard {
    param($Cards, [string]$FeatureId)
    $marker = "Feature: $FeatureId"
    return $Cards | Where-Object { $_.desc -and ($_.desc.TrimEnd() -match [regex]::Escape($marker) + '\s*$') } | Select-Object -First 1
}

function New-TrelloCard {
    param($Cred, [string]$ListId, [string]$Name, [string]$Desc)
    $uri = "https://api.trello.com/1/cards?key=$($Cred.Key)&token=$($Cred.Token)"
    return Invoke-RestMethod -Uri $uri -Method Post -Body @{ idList = $ListId; name = $Name; desc = $Desc } -TimeoutSec 15
}

function Move-TrelloCard {
    param($Cred, [string]$CardId, [string]$ListId)
    # Invoke-RestMethod -Method Put com -Body hashtable nao e' aplicado pela API do Trello
    # (retorna 200 sem mover o card) -- idList precisa ir na query string tambem no PUT.
    $uri = "https://api.trello.com/1/cards/${CardId}?key=$($Cred.Key)&token=$($Cred.Token)&idList=$ListId"
    Invoke-RestMethod -Uri $uri -Method Put -TimeoutSec 15 | Out-Null
}

function Add-TrelloComment {
    param($Cred, [string]$CardId, [string]$Text)
    $uri = "https://api.trello.com/1/cards/$CardId/actions/comments?key=$($Cred.Key)&token=$($Cred.Token)"
    Invoke-RestMethod -Uri $uri -Method Post -Body @{ text = $Text } -TimeoutSec 15 | Out-Null
}

function Get-FeatureTitle {
    param([string]$FeatureId)
    $specPath = Join-Path $repoRoot "specs/$FeatureId/spec.md"
    if (Test-Path $specPath) {
        $line = Get-Content $specPath -Encoding UTF8 | Where-Object { $_ -match '^# Feature Specification:\s*(.+)$' } | Select-Object -First 1
        if ($line -match '^# Feature Specification:\s*(.+)$') { return $Matches[1].Trim() }
    }
    return $FeatureId
}

function Get-SpecSummary {
    param([string]$FeatureId)
    $specPath = Join-Path $repoRoot "specs/$FeatureId/spec.md"
    if (-not (Test-Path $specPath)) { return "Sem resumo disponivel (spec.md nao encontrado).`n`nFeature: $FeatureId" }
    $bodyLines = Get-Content $specPath -Encoding UTF8 | Where-Object { $_ -and ($_ -notmatch '^#') -and ($_ -notmatch '^\*\*') -and ($_ -notmatch '^```') } | Select-Object -First 5
    $summary = ($bodyLines -join ' ').Trim()
    if (-not $summary) { $summary = "Feature $FeatureId." }
    if ($summary.Length -gt 500) { $summary = $summary.Substring(0, 500) + '...' }
    return "$summary`n`nFeature: $FeatureId"
}

function Get-TarefasBloqueantes {
    param([string]$FeatureId)
    $tasksPath = Join-Path $repoRoot "specs/$FeatureId/tasks.md"
    if (-not (Test-Path $tasksPath)) { return -1 }
    $pending = Get-Content $tasksPath -Encoding UTF8 | Where-Object { $_ -match '^\s*-\s\[\s\]\s*T\d+' -and ($_ -notmatch '(?i)\(parte manual\)') }
    return @($pending).Count
}

function Test-AutomatedTestsPass {
    $testsProject = Join-Path $repoRoot 'tests/SPI.Application.Tests'
    if (-not (Test-Path $testsProject)) { return $false }
    & dotnet test $testsProject | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Get-ImplementationSummary {
    param([string]$FeatureId)
    $tasksPath = Join-Path $repoRoot "specs/$FeatureId/tasks.md"
    if (-not (Test-Path $tasksPath)) { return "Implementacao concluida (tasks.md nao encontrado para detalhar)." }
    $doneHeadings = New-Object System.Collections.Generic.List[string]
    $currentHeading = $null
    foreach ($line in (Get-Content $tasksPath -Encoding UTF8)) {
        if ($line -match '^##\s+(.+)$') { $currentHeading = $Matches[1].Trim() }
        if ($line -match '^\s*-\s\[[xX]\]' -and $currentHeading -and (-not $doneHeadings.Contains($currentHeading))) {
            $doneHeadings.Add($currentHeading)
        }
    }
    if ($doneHeadings.Count -eq 0) { return "Implementacao concluida." }
    return "Fases concluidas:`n- " + ($doneHeadings -join "`n- ")
}

try {
    switch ($Fase) {
        'backlog' {
            $cred = Get-TrelloCredentials
            $featureId = Get-FeatureId
            $cards = Get-TrelloCards -Cred $cred
            $existing = Find-FeatureCard -Cards $cards -FeatureId $featureId

            if ($existing) {
                Write-SyncLog -Fase $Fase -Result 'skip' -Detail "card ja existe ($featureId)"
                Write-Output "[trello-sync] backlog: card ja existe, nada a fazer ($featureId)"
            }
            else {
                $lists = Get-TrelloLists -Cred $cred
                if (-not $lists.ContainsKey('Backlog')) { throw "Lista 'Backlog' nao encontrada no quadro" }
                $title = Get-FeatureTitle -FeatureId $featureId
                $desc = Get-SpecSummary -FeatureId $featureId
                if (-not $DryRun) { New-TrelloCard -Cred $cred -ListId $lists['Backlog'] -Name $title -Desc $desc | Out-Null }
                $resultado = if ($DryRun) { 'dry-run' } else { 'ok' }
                $detalhe = if ($DryRun) { "criaria card '$title' ($featureId)" } else { "card criado ($featureId)" }
                Write-SyncLog -Fase $Fase -Result $resultado -Detail $detalhe
                Write-Output "[trello-sync] backlog: $detalhe"
            }
        }

        { $_ -in @('design', 'a-fazer', 'em-andamento') } {
            $listNameMap = @{ 'design' = 'Design'; 'a-fazer' = 'A Fazer'; 'em-andamento' = 'Em andamento' }
            $targetListName = $listNameMap[$Fase]
            $cred = Get-TrelloCredentials
            $featureId = Get-FeatureId
            $cards = Get-TrelloCards -Cred $cred
            $card = Find-FeatureCard -Cards $cards -FeatureId $featureId

            if (-not $card) {
                Write-SyncLog -Fase $Fase -Result 'skip' -Detail "card nao encontrado ($featureId)"
                Write-Output "[trello-sync] ${Fase}: card nao encontrado, nada a mover ($featureId)"
            }
            else {
                $lists = Get-TrelloLists -Cred $cred
                if (-not $lists.ContainsKey($targetListName)) { throw "Lista '$targetListName' nao encontrada no quadro" }
                if (-not $DryRun) { Move-TrelloCard -Cred $cred -CardId $card.id -ListId $lists[$targetListName] }
                $resultado = if ($DryRun) { 'dry-run' } else { 'ok' }
                $detalhe = if ($DryRun) { "moveria card para '$targetListName' ($featureId)" } else { "card movido para '$targetListName' ($featureId)" }
                Write-SyncLog -Fase $Fase -Result $resultado -Detail $detalhe
                Write-Output "[trello-sync] ${Fase}: $detalhe"
            }
        }

        'revisao-codigo' {
            $cred = Get-TrelloCredentials
            $featureId = Get-FeatureId
            $cards = Get-TrelloCards -Cred $cred
            $card = Find-FeatureCard -Cards $cards -FeatureId $featureId

            if (-not $card) {
                Write-SyncLog -Fase $Fase -Result 'skip' -Detail "card nao encontrado ($featureId)"
                Write-Output "[trello-sync] revisao-codigo: card nao encontrado, nada a fazer ($featureId)"
            }
            else {
                $pendentes = Get-TarefasBloqueantes -FeatureId $featureId
                $testesOk = Test-AutomatedTestsPass
                if ($pendentes -eq 0 -and $testesOk) {
                    $lists = Get-TrelloLists -Cred $cred
                    if (-not $lists.ContainsKey('Revisão de código')) { throw "Lista 'Revisão de código' nao encontrada no quadro" }
                    $summary = Get-ImplementationSummary -FeatureId $featureId
                    if (-not $DryRun) {
                        Move-TrelloCard -Cred $cred -CardId $card.id -ListId $lists['Revisão de código']
                        Add-TrelloComment -Cred $cred -CardId $card.id -Text $summary
                    }
                    $resultado = if ($DryRun) { 'dry-run' } else { 'ok' }
                    $detalhe = if ($DryRun) { "moveria para 'Revisão de código' + comentario ($featureId)" } else { "card movido para 'Revisão de código' + comentario ($featureId)" }
                    Write-SyncLog -Fase $Fase -Result $resultado -Detail $detalhe
                    Write-Output "[trello-sync] revisao-codigo: $detalhe"
                }
                else {
                    $motivoTestes = if ($testesOk) { 'ok' } else { 'falharam' }
                    $motivo = "$pendentes tarefa(s) nao-manual pendente(s), testes $motivoTestes"
                    Write-SyncLog -Fase $Fase -Result 'nao-movido' -Detail "$motivo ($featureId)"
                    Write-Output "[trello-sync] revisao-codigo: NAO movido -- $motivo ($featureId)"
                }
            }
        }

        default {
            Write-SyncLog -Fase $Fase -Result 'skip' -Detail 'fase nao reconhecida'
            Write-Output "[trello-sync] fase '$Fase' nao reconhecida, nada a fazer"
        }
    }
}
catch {
    $errMsg = $_.Exception.Message
    Write-SyncLog -Fase $Fase -Result 'erro' -Detail $errMsg
    Write-Output "[trello-sync] ${Fase}: falha na sincronizacao (detalhe em sync.log) -- Spec Kit segue normalmente"
}

exit 0
