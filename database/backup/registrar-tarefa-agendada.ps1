<#
.SYNOPSIS
    Registra (ou atualiza) a Tarefa Agendada do Windows que roda o backup semanal.

.DESCRIPTION
    Roda uma unica vez (como administrador, se exigido pela politica local)
    para configurar o Agendador de Tarefas do Windows a chamar
    backup-database.ps1 semanalmente, de forma independente da aplicacao
    estar aberta (FR-002). Idempotente: se a tarefa ja existir, atualiza em
    vez de duplicar.

.PARAMETER DiaDaSemana
    Dia da semana para rodar o backup. Padrao: Sunday.

.PARAMETER Horario
    Horario (HH:mm) para rodar o backup. Padrao: 02:00.

.EXAMPLE
    .\registrar-tarefa-agendada.ps1
    .\registrar-tarefa-agendada.ps1 -DiaDaSemana Monday -Horario 03:30
#>

[CmdletBinding()]
param(
    [ValidateSet('Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday')]
    [string]$DiaDaSemana = 'Sunday',

    [string]$Horario = '02:00'
)

$ErrorActionPreference = 'Stop'

$nomeTarefa = 'SPI - Backup Semanal do Banco'
$scriptBackup = Join-Path $PSScriptRoot 'backup-database.ps1'

if (-not (Test-Path $scriptBackup)) {
    throw "Nao encontrei $scriptBackup."
}

$acao = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$scriptBackup`""
$gatilho = New-ScheduledTaskTrigger -Weekly -DaysOfWeek $DiaDaSemana -At $Horario
$configuracoes = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopOnIdleEnd

$tarefaExistente = Get-ScheduledTask -TaskName $nomeTarefa -ErrorAction SilentlyContinue
if ($tarefaExistente) {
    Set-ScheduledTask -TaskName $nomeTarefa -Action $acao -Trigger $gatilho -Settings $configuracoes | Out-Null
    Write-Output "Tarefa '$nomeTarefa' ja existia - atualizada para $DiaDaSemana as $Horario."
} else {
    Register-ScheduledTask -TaskName $nomeTarefa -Action $acao -Trigger $gatilho -Settings $configuracoes -Description 'Backup semanal automatico do banco spi_db (specs/050-backup-automatico-banco). Independente da aplicacao SPI estar aberta.' | Out-Null
    Write-Output "Tarefa '$nomeTarefa' registrada para $DiaDaSemana as $Horario."
}

Write-Output "Para forcar uma execucao de teste agora: Start-ScheduledTask -TaskName '$nomeTarefa'"
Write-Output "Para conferir o historico: abra o Agendador de Tarefas do Windows -> localize '$nomeTarefa' -> aba Historico."
