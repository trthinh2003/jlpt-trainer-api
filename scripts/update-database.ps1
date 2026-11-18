#Requires -Version 5.1

$ErrorActionPreference = 'Stop'
$infrastructurePath = Join-Path $PSScriptRoot '../src/JlptTrainer.Infrastructure'

Push-Location -LiteralPath $infrastructurePath
try {
    dotnet ef database update --startup-project ../JlptTrainer.Api
    if ($LASTEXITCODE -ne 0) {
        throw "Database migration failed (dotnet ef exit code: $LASTEXITCODE)."
    }
} finally {
    Pop-Location
}
