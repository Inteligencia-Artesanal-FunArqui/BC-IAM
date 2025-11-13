# Script to update namespaces from monolith to microservice

Write-Host "Updating IAM namespaces..." -ForegroundColor Green

# Update IAM folder
Get-ChildItem -Path "IAM.API\IAM" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace 'OsitoPolarPlatform\.API\.IAM', 'OsitoPolar.IAM.Service'
    Set-Content -Path $_.FullName -Value $newContent -NoNewline
    Write-Host "Updated: $($_.Name)" -ForegroundColor Yellow
}

Write-Host "`nUpdating Shared namespaces..." -ForegroundColor Green

# Update Shared folder
Get-ChildItem -Path "IAM.API\Shared" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace 'OsitoPolarPlatform\.API\.Shared', 'OsitoPolar.IAM.Service.Shared'
    Set-Content -Path $_.FullName -Value $newContent -NoNewline
    Write-Host "Updated: $($_.Name)" -ForegroundColor Yellow
}

Write-Host "`nNamespace update complete!" -ForegroundColor Green
