# Script to fix remaining references

Write-Host "Fixing Shared references..." -ForegroundColor Green

# Fix Shared references that were missed
Get-ChildItem -Path "IAM.API" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match 'OsitoPolarPlatform\.API\.Shared') {
        $newContent = $content -replace 'OsitoPolarPlatform\.API\.Shared', 'OsitoPolar.IAM.Service.Shared'
        Set-Content -Path $_.FullName -Value $newContent -NoNewline
        Write-Host "Fixed Shared reference in: $($_.Name)" -ForegroundColor Yellow
    }
}

Write-Host "`nCommenting out cross-context dependencies..." -ForegroundColor Green

# Comment out references to other bounded contexts
$boundedContexts = @(
    'OsitoPolarPlatform.API.Profiles',
    'OsitoPolarPlatform.API.Notifications',
    'OsitoPolarPlatform.API.SubscriptionsAndPayments',
    'OsitoPolarPlatform.API.EquipmentManagement',
    'OsitoPolarPlatform.API.ServiceRequests',
    'OsitoPolarPlatform.API.WorkOrders',
    'OsitoPolarPlatform.API.Analytics'
)

Get-ChildItem -Path "IAM.API\IAM" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName
    $modified = $false

    $newContent = $content | ForEach-Object {
        $line = $_
        foreach ($context in $boundedContexts) {
            if ($line -match "^\s*using $context") {
                $line = "// " + $line
                $modified = $true
                break
            }
        }
        $line
    }

    if ($modified) {
        $newContent | Set-Content -Path $_.FullName
        Write-Host "Commented cross-context usings in: $($_.Name)" -ForegroundColor Yellow
    }
}

Write-Host "`nReferences fixed!" -ForegroundColor Green
