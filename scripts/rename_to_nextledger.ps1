# Rename BudgetWise to NextLedger
# Run from the repository root

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot

Write-Host "Renaming BudgetWise to NextLedger in $root" -ForegroundColor Cyan

# 1. Rename folders (bottom-up to avoid path issues)
$foldersToRename = @(
    # Tests
    "tests\BudgetWise.Domain.Tests",
    "tests\BudgetWise.Application.Tests",
    "tests\BudgetWise.Infrastructure.Tests",
    # Source
    "src\BudgetWise.Domain",
    "src\BudgetWise.Application",
    "src\BudgetWise.Infrastructure",
    "src\BudgetWise.App"
)

foreach ($folder in $foldersToRename) {
    $oldPath = Join-Path $root $folder
    $newPath = $oldPath -replace "BudgetWise", "NextLedger"

    if (Test-Path $oldPath) {
        Write-Host "  Renaming folder: $folder" -ForegroundColor Yellow
        Rename-Item -Path $oldPath -NewName (Split-Path -Leaf $newPath)
    }
}

# 2. Rename .csproj files
$csprojFiles = Get-ChildItem -Path $root -Filter "BudgetWise.*.csproj" -Recurse
foreach ($file in $csprojFiles) {
    $newName = $file.Name -replace "BudgetWise", "NextLedger"
    Write-Host "  Renaming csproj: $($file.Name) -> $newName" -ForegroundColor Yellow
    Rename-Item -Path $file.FullName -NewName $newName
}

# 3. Rename solution file
$slnFile = Join-Path $root "BudgetWise.sln"
if (Test-Path $slnFile) {
    Write-Host "  Renaming solution file" -ForegroundColor Yellow
    Rename-Item -Path $slnFile -NewName "NextLedger.sln"
}

# 4. Rename .ico file
$icoFile = Join-Path $root "src\NextLedger.App\BudgetWise.ico"
if (Test-Path $icoFile) {
    Write-Host "  Renaming ico file" -ForegroundColor Yellow
    Rename-Item -Path $icoFile -NewName "NextLedger.ico"
}

# 5. Update file contents
Write-Host "`nUpdating file contents..." -ForegroundColor Cyan

$extensions = @("*.cs", "*.xaml", "*.csproj", "*.sln", "*.json", "*.md", "*.yml", "*.yaml", "*.xml", "*.props", "*.targets", "*.pubxml")
$filesToUpdate = @()

foreach ($ext in $extensions) {
    $filesToUpdate += Get-ChildItem -Path $root -Filter $ext -Recurse -File | Where-Object { $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" }
}

foreach ($file in $filesToUpdate) {
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -and $content -match "BudgetWise") {
        Write-Host "  Updating: $($file.FullName)" -ForegroundColor Gray
        $newContent = $content -replace "BudgetWise", "NextLedger"
        $newContent = $newContent -replace "budgetwise", "nextledger"
        $newContent = $newContent -replace "budget-wise", "next-ledger"
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
    }
}

Write-Host "`nDone! Remember to:" -ForegroundColor Green
Write-Host "  1. Update the icon images"
Write-Host "  2. Clean and rebuild: dotnet clean && dotnet build"
Write-Host "  3. Rename the GitHub repository"
