#Requires -Version 5.1
param([switch]$MergeIdenticalVocabs)

$ErrorActionPreference = 'Stop'
$infrastructurePath = Join-Path $PSScriptRoot '../src/JlptTrainer.Infrastructure'

Push-Location -LiteralPath $infrastructurePath
try {
    $factoryArgs = @()
    if ($MergeIdenticalVocabs) {
        $factoryArgs = @('--', '--merge-identical-vocabs')
    }
    dotnet ef database update --startup-project ../JlptTrainer.Api @factoryArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Database migration failed (dotnet ef exit code: $LASTEXITCODE)."
    }
} finally {
    Pop-Location
}
