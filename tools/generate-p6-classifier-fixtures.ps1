param(
    [string]$OutputRoot = "samples\synthetic\p6\classifier"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$trainingCount = 5
$holdoutCount = 2

$classes = @(
    "invoice",
    "purchase_order",
    "delivery_note",
    "contract",
    "form"
)

$companies = @(
    "Fictional Components GmbH",
    "Example Manufacturing AG",
    "Demo Logistics GmbH",
    "Sample Industrial Systems AG",
    "Synthetic Services GmbH",
    "Portfolio Technologies AG",
    "Testwerk Solutions GmbH"
)

$cities = @(
    "Berlin",
    "Hamburg",
    "Munich",
    "Cologne",
    "Frankfurt",
    "Stuttgart",
    "Dusseldorf"
)

function Get-DocumentLines {
    param(
        [Parameter(Mandatory)]
        [string]$Class,

        [Parameter(Mandatory)]
        [int]$Index,

        [Parameter(Mandatory)]
        [bool]$IsHoldout
    )

    $offset = if ($IsHoldout) { 20 } else { 0 }
    $n = $Index + $offset

    $companyA = $companies[$n % $companies.Count]
    $companyB = $companies[($n + 2) % $companies.Count]
    $city = $cities[$n % $cities.Count]
    $day = "{0:D2}" -f (($n % 27) + 1)
    $date = "2026-09-$day"

    switch ($Class) {
        "invoice" {
            return @(
                $(if ($Index % 2 -eq 0) { "INVOICE" } else { "RECHNUNG" }),
                "Invoice Number: INV-2026-{0:D4}" -f (1000 + $n),
                "Invoice Date: $date",
                "Supplier: $companyA",
                "Customer: $companyB",
                "Location: $city",
                "Currency: EUR",
                "Subtotal: {0:N2} EUR" -f (800 + ($n * 17)),
                "VAT: {0:N2} EUR" -f (152 + ($n * 3)),
                "Total Amount: {0:N2} EUR" -f (952 + ($n * 20)),
                "",
                "Synthetic classifier fixture - no real customer data"
            )
        }

        "purchase_order" {
            return @(
                $(if ($Index % 2 -eq 0) { "PURCHASE ORDER" } else { "BESTELLUNG" }),
                "Purchase Order Number: PO-2026-{0:D4}" -f (5000 + $n),
                "Order Date: $date",
                "Buyer: $companyA",
                "Supplier: $companyB",
                "Ship To: $city",
                "Currency: EUR",
                "Item: Industrial Sensor Model {0:D2}" -f $n,
                "Quantity: {0}" -f (5 + $n),
                "Unit Price: {0:N2} EUR" -f (100 + ($n * 4)),
                "Total Amount: {0:N2} EUR" -f ((5 + $n) * (100 + ($n * 4))),
                "",
                "Synthetic classifier fixture - no real customer data"
            )
        }

        "delivery_note" {
            return @(
                $(if ($Index % 2 -eq 0) { "DELIVERY NOTE" } else { "LIEFERSCHEIN" }),
                "Delivery Note Number: DN-2026-{0:D4}" -f (7000 + $n),
                "Delivery Date: $date",
                "Supplier: $companyA",
                "Customer: $companyB",
                "Destination: $city",
                "Purchase Order: PO-2026-{0:D4}" -f (5000 + $n),
                "Item: Industrial Sensor Model {0:D2}" -f $n,
                "Quantity Delivered: {0}" -f (5 + $n),
                "Condition: Received in good order",
                "",
                "Synthetic classifier fixture - no real customer data"
            )
        }

        "contract" {
            return @(
                $(if ($Index % 2 -eq 0) { "SERVICE AGREEMENT" } else { "FRAMEWORK CONTRACT" }),
                "Contract Reference: CTR-2026-{0:D4}" -f (3000 + $n),
                "Effective Date: $date",
                "Party A: $companyA",
                "Party B: $companyB",
                "Jurisdiction: Germany",
                "Service Location: $city",
                "Term: {0} months" -f (12 + ($n % 3) * 6),
                "",
                "Scope of Services",
                "The parties agree to provide the services described in this agreement.",
                "Payment terms and service levels are governed by the agreed schedule.",
                "",
                "Synthetic classifier fixture - no real customer data"
            )
        }

        "form" {
            return @(
                $(if ($Index % 2 -eq 0) { "SUPPLIER REGISTRATION FORM" } else { "BUSINESS INFORMATION FORM" }),
                "Form Reference: FORM-2026-{0:D4}" -f (9000 + $n),
                "Company Name: $companyA",
                "Contact Name: Example Contact $n",
                "City: $city",
                "Country: Germany",
                "Preferred Currency: EUR",
                "Business Type: Industrial Services",
                "Approved: Yes",
                "",
                "[ ] New supplier",
                "[X] Registration complete",
                "[ ] Additional documents required",
                "",
                "Synthetic classifier fixture - no real customer data"
            )
        }

        default {
            throw "Unsupported document class: $Class"
        }
    }
}

function Write-DocumentImage {
    param(
        [Parameter(Mandatory)]
        [string[]]$Lines,

        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [int]$Variant
    )

    $width = 1400
    $height = 1800

    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.Clear([System.Drawing.Color]::White)
        $graphics.TextRenderingHint =
            [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

        $titleFont = New-Object System.Drawing.Font(
            "Arial",
            (26 + ($Variant % 3)),
            [System.Drawing.FontStyle]::Bold
        )

        $bodyFont = New-Object System.Drawing.Font(
            "Arial",
            (17 + ($Variant % 2)),
            [System.Drawing.FontStyle]::Regular
        )

        $footerFont = New-Object System.Drawing.Font(
            "Arial",
            12,
            [System.Drawing.FontStyle]::Italic
        )

        $brush = [System.Drawing.Brushes]::Black
        $footerBrush = [System.Drawing.Brushes]::DimGray

        try {
            $left = 80 + (($Variant % 3) * 18)
            $top = 80 + (($Variant % 2) * 24)

            $graphics.DrawString(
                $Lines[0],
                $titleFont,
                $brush,
                $left,
                $top
            )

            $y = $top + 82

            for ($i = 1; $i -lt $Lines.Count; $i++) {
                $line = $Lines[$i]

                if ([string]::IsNullOrWhiteSpace($line)) {
                    $y += 24
                    continue
                }

                $isFooter =
                    $line -like "Synthetic classifier fixture*"

                $font =
                    if ($isFooter) {
                        $footerFont
                    }
                    else {
                        $bodyFont
                    }

                $lineBrush =
                    if ($isFooter) {
                        $footerBrush
                    }
                    else {
                        $brush
                    }

                $graphics.DrawString(
                    $line,
                    $font,
                    $lineBrush,
                    $left,
                    $y
                )

                $y +=
                    if ($isFooter) {
                        42
                    }
                    else {
                        50 + (($Variant + $i) % 3) * 4
                    }
            }
        }
        finally {
            $titleFont.Dispose()
            $bodyFont.Dispose()
            $footerFont.Dispose()
        }

        $directory = Split-Path -Parent $Path
        New-Item -ItemType Directory -Force -Path $directory |
            Out-Null

        $bitmap.Save(
            $Path,
            [System.Drawing.Imaging.ImageFormat]::Png
        )
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

foreach ($class in $classes) {
    for ($i = 1; $i -le $trainingCount; $i++) {
        $lines = Get-DocumentLines `
            -Class $class `
            -Index $i `
            -IsHoldout $false

        $fileName = "{0}-training-{1:D2}.png" -f $class, $i

        $path = Join-Path `
            $OutputRoot `
            "training\$class\$fileName"

        Write-DocumentImage `
            -Lines $lines `
            -Path $path `
            -Variant $i
    }

    for ($i = 1; $i -le $holdoutCount; $i++) {
        $lines = Get-DocumentLines `
            -Class $class `
            -Index $i `
            -IsHoldout $true

        $fileName = "{0}-holdout-{1:D2}.png" -f $class, $i

        $path = Join-Path `
            $OutputRoot `
            "holdout\$class\$fileName"

        Write-DocumentImage `
            -Lines $lines `
            -Path $path `
            -Variant ($i + 10)
    }
}

$trainingFiles =
    Get-ChildItem `
        (Join-Path $OutputRoot "training") `
        -File `
        -Recurse

$holdoutFiles =
    Get-ChildItem `
        (Join-Path $OutputRoot "holdout") `
        -File `
        -Recurse

Write-Host "Generated classifier fixtures."
Write-Host "Training files: $($trainingFiles.Count)"
Write-Host "Holdout files:  $($holdoutFiles.Count)"
