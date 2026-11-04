param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('StockFlow', 'ExpenseLens', 'SupportDesk')]
    [string]$Project
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repoRoot "projects/$Project/compose.yaml"
$testDatabase = 'portfolio_test_' + [Guid]::NewGuid().ToString('N')
$databaseCreated = $false
$testFailed = $false
$devPassword = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { 'portfolio_dev' }

Push-Location $repoRoot
try {
    docker compose -f $composeFile up --build -d
    if ($LASTEXITCODE -ne 0) { throw 'Docker stack did not start.' }

    docker compose -f $composeFile exec -T db psql -U portfolio -d $Project -c "CREATE DATABASE $testDatabase;"
    if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated test database.' }
    $databaseCreated = $true

    # The migration image includes the SDK and this project's test sources.
    # No PostgreSQL port needs to be published on the host.
    docker compose -f $composeFile run --rm --no-deps --entrypoint dotnet `
        -e "PORTFOLIO_TEST_DATABASE=Host=db;Database=$testDatabase;Username=portfolio;Password=$devPassword" `
        migrate test "projects/$Project/tests/$Project.Tests" -c Release
    $testFailed = $LASTEXITCODE -ne 0
}
finally {
    if ($databaseCreated) {
        # Only the generated database from this invocation is removed.
        docker compose -f $composeFile exec -T db psql -U portfolio -d $Project -c "DROP DATABASE $testDatabase WITH (FORCE);"
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove temporary database $testDatabase" }
    }
    Pop-Location
}
if ($testFailed) { throw 'PostgreSQL tests failed.' }
