param(
    [string]$InputRoot = "samples\synthetic\p6\classifier\training",
    [string]$OutputRoot = "samples\synthetic\p6\classifier\training-layout"
)

$ErrorActionPreference = "Stop"

$endpoint = $env:DocumentIntelligence__Endpoint
$apiKey = $env:DocumentIntelligence__ApiKey

if ([string]::IsNullOrWhiteSpace($endpoint)) {
    throw "DocumentIntelligence__Endpoint is required."
}

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    throw "DocumentIntelligence__ApiKey is required."
}

$endpoint = $endpoint.TrimEnd("/")

$files = Get-ChildItem `
    -Path $InputRoot `
    -Recurse `
    -File `
    -Filter "*.png"

if ($files.Count -eq 0) {
    throw "No PNG training documents found under '$InputRoot'."
}

$resolvedInputRoot = (Resolve-Path $InputRoot).Path.TrimEnd(
    [IO.Path]::DirectorySeparatorChar,
    [IO.Path]::AltDirectorySeparatorChar
)

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring(
        $resolvedInputRoot.Length
    ).TrimStart(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar
    )

    $relativeDirectory = Split-Path `
        $relativePath `
        -Parent

    $outputDirectory = Join-Path `
        $OutputRoot `
        $relativeDirectory

    New-Item `
        -ItemType Directory `
        -Path $outputDirectory `
        -Force |
        Out-Null

    $outputName =
        [IO.Path]::GetFileNameWithoutExtension(
            $file.Name
        ) + ".ocr.json"

    $outputPath = Join-Path `
        $outputDirectory `
        $outputName

    $analyzeUri =
        "$endpoint/documentintelligence/documentModels/prebuilt-layout:analyze?api-version=2024-11-30"

    Write-Host "Analyzing $relativePath"

    $response = Invoke-WebRequest `
        -UseBasicParsing `
        -Method Post `
        -Uri $analyzeUri `
        -Headers @{
            "Ocp-Apim-Subscription-Key" = $apiKey
        } `
        -ContentType "application/octet-stream" `
        -InFile $file.FullName

    $operationLocation =
        $response.Headers["Operation-Location"]

    if ([string]::IsNullOrWhiteSpace($operationLocation)) {
        throw "Operation-Location was not returned for '$relativePath'."
    }

    do {
        Start-Sleep -Seconds 1

        $pollResponse = Invoke-WebRequest `
            -UseBasicParsing `
            -Method Get `
            -Uri $operationLocation `
            -Headers @{
                "Ocp-Apim-Subscription-Key" = $apiKey
            }

        $pollJson =
            $pollResponse.Content |
            ConvertFrom-Json

        $status = $pollJson.status

        if ($status -eq "failed") {
            throw "Layout analysis failed for '$relativePath'."
        }
    }
    while ($status -notin @("succeeded", "failed"))

    [IO.File]::WriteAllText(
        $outputPath,
        $pollResponse.Content,
        [Text.UTF8Encoding]::new($false)
    )

    Write-Host "Created $outputPath"
}

Write-Host ""
Write-Host "Layout result files: $((Get-ChildItem $OutputRoot -Recurse -File -Filter '*.ocr.json').Count)"