$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (
    Join-Path $PSScriptRoot "..\.."
)

Push-Location $repoRoot

try {
    Write-Host ""
    Write-Host "=== P12 FINAL VERIFICATION ==="

    Write-Host ""
    Write-Host "=== BUILD ==="

    dotnet build IntelliDocs.slnx --no-restore

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }

    Write-Host ""
    Write-Host "=== .NET TESTS ==="

    dotnet test IntelliDocs.slnx --no-build

    if ($LASTEXITCODE -ne 0) {
        throw ".NET tests failed."
    }

    Write-Host ""
    Write-Host "=== PYTHON QUALITY ==="

    Push-Location "evaluation"

    try {
        python -m pytest -q

        if ($LASTEXITCODE -ne 0) {
            throw "Python evaluation failed."
        }
    }
    finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "=== TERRAFORM ==="

    terraform -chdir="infra\terraform" fmt -check

    if ($LASTEXITCODE -ne 0) {
        throw "Terraform format check failed."
    }

    terraform -chdir="infra\terraform" validate

    if ($LASTEXITCODE -ne 0) {
        throw "Terraform validation failed."
    }

    Write-Host ""
    Write-Host "=== GIT DIFF ==="

    git diff --check

    if ($LASTEXITCODE -ne 0) {
        throw "git diff --check failed."
    }

    Write-Host ""
    Write-Host "P12 final verification: PASS"
}
finally {
    Pop-Location
}