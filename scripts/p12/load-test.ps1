param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$Requests = 100,
    [int]$Concurrency = 10,
    [string]$Endpoint = "/health"
)

$ErrorActionPreference = "Stop"

if ($Requests -lt 1) {
    throw "Requests must be >= 1."
}

if ($Concurrency -lt 1) {
    throw "Concurrency must be >= 1."
}

$target = $BaseUrl.TrimEnd("/") + "/" + $Endpoint.TrimStart("/")

Write-Host "IntelliDocs P12 load test"
Write-Host "Target      : $target"
Write-Host "Requests    : $Requests"
Write-Host "Concurrency : $Concurrency"

$results = New-Object System.Collections.ArrayList
$nextRequest = 0
$gate = New-Object object

$workers = @()

$overall = [System.Diagnostics.Stopwatch]::StartNew()

for ($workerId = 0; $workerId -lt $Concurrency; $workerId++) {

    $workers += Start-Job -ArgumentList $target, $Requests, $workerId, $Concurrency -ScriptBlock {

        param($Target, $RequestCount, $WorkerId, $WorkerCount)

        $workerResults = @()

        for ($i = $WorkerId; $i -lt $RequestCount; $i += $WorkerCount) {

            $watch = [System.Diagnostics.Stopwatch]::StartNew()

            try {
                $response = Invoke-WebRequest `
                    -Uri $Target `
                    -Method Get `
                    -UseBasicParsing `
                    -TimeoutSec 30

                $watch.Stop()

                $workerResults += [PSCustomObject]@{
                    RequestId  = $i
                    Success    = $response.StatusCode -ge 200 -and $response.StatusCode -lt 400
                    StatusCode = [int]$response.StatusCode
                    DurationMs = [math]::Round($watch.Elapsed.TotalMilliseconds, 2)
                    Error      = $null
                }
            }
            catch {
                $watch.Stop()

                $statusCode = 0

                if ($_.Exception.Response -and
                    $_.Exception.Response.StatusCode) {

                    $statusCode =
                    [int]$_.Exception.Response.StatusCode
                }

                $workerResults += [PSCustomObject]@{
                    RequestId  = $i
                    Success    = $false
                    StatusCode = $statusCode
                    DurationMs = [math]::Round($watch.Elapsed.TotalMilliseconds, 2)
                    Error      = $_.Exception.GetType().Name
                }
            }
        }

        $workerResults
    }
}

$workers | Wait-Job | Out-Null

foreach ($job in $workers) {
    $jobResults = Receive-Job $job

    foreach ($result in $jobResults) {
        [void]$results.Add($result)
    }

    Remove-Job $job
}

$overall.Stop()

$ordered = @(
    $results |
    Sort-Object RequestId
)

$durations = @(
    $ordered |
    ForEach-Object { [double]$_.DurationMs } |
    Sort-Object
)

function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percentile
    )

    if ($Values.Count -eq 0) {
        return 0
    }

    $index = [math]::Ceiling(
        ($Percentile / 100.0) * $Values.Count
    ) - 1

    if ($index -lt 0) {
        $index = 0
    }

    if ($index -ge $Values.Count) {
        $index = $Values.Count - 1
    }

    return [math]::Round($Values[$index], 2)
}

$successCount = @(
    $ordered | Where-Object Success
).Count

$failureCount = $ordered.Count - $successCount

$elapsedSeconds = $overall.Elapsed.TotalSeconds

$throughput = if ($elapsedSeconds -gt 0) {
    [math]::Round($ordered.Count / $elapsedSeconds, 2)
}
else {
    0
}

$summary = [PSCustomObject]@{
    Target            = $target
    Requests          = $ordered.Count
    Concurrency       = $Concurrency
    Successful        = $successCount
    Failed            = $failureCount
    SuccessRate       = if ($ordered.Count -gt 0) {
        [math]::Round(
            ($successCount / $ordered.Count) * 100,
            2
        )
    }
    else {
        0
    }
    ElapsedSeconds    = [math]::Round($elapsedSeconds, 2)
    RequestsPerSecond = $throughput
    P50Ms             = Get-Percentile $durations 50
    P95Ms             = Get-Percentile $durations 95
    P99Ms             = Get-Percentile $durations 99
    MaxMs             = if ($durations.Count -gt 0) {
        [math]::Round($durations[-1], 2)
    }
    else {
        0
    }
}

Write-Host ""
Write-Host "=== RESULT ==="
$summary | Format-List

if ($failureCount -gt 0) {
    Write-Host ""
    Write-Host "=== FAILURES ==="

    $ordered |
    Where-Object { -not $_.Success } |
    Group-Object StatusCode, Error |
    Select-Object Count, Name |
    Format-Table -AutoSize
}

if ($failureCount -gt 0) {
    exit 1
}

exit 0