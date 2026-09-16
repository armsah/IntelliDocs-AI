$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (
    Join-Path $PSScriptRoot "..\.."
)

Push-Location $repoRoot

try {
    Write-Host ""
    Write-Host "=== P12 QUALITY REGRESSION ==="

    Write-Host ""
    Write-Host "=== .NET TESTS ==="

    dotnet test IntelliDocs.slnx --no-restore

    if ($LASTEXITCODE -ne 0) {
        throw ".NET quality regression failed."
    }

    Write-Host ""
    Write-Host "=== PYTHON EVALUATION ==="

    Push-Location "evaluation"

    try {
        python -m pytest -q

        if ($LASTEXITCODE -ne 0) {
            throw "Python quality evaluation failed."
        }
    }
    finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "P12 quality regression: PASS"
}
finally {
    Pop-Location
}