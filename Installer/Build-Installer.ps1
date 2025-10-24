# Build-Installer.ps1
# Build script for CoreTray installer

param(
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipInno,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("x64", "arm64")]
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"
$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Colors
$ColorInfo = "Cyan"
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"

function Write-Step {
    param([string]$Message)
    Write-Host "`n▶ $Message" -ForegroundColor $ColorInfo
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $ColorSuccess
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor $ColorWarning
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $ColorError
}

# Store original location
$originalLocation = Get-Location
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath

try {
    Write-Host "`n╔═══════════════════════════════════════╗" -ForegroundColor $ColorInfo
    Write-Host "║   CoreTray Installer Build Script    ║" -ForegroundColor $ColorInfo
    Write-Host "╚═══════════════════════════════════════╝`n" -ForegroundColor $ColorInfo

    Write-Step "Checking prerequisites..."
    
    if (-not $SkipInno) {
        if (-not (Test-Path $InnoSetupPath)) {
            Write-Error "Inno Setup not found at: $InnoSetupPath"
            Write-Host "Please download and install from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
            exit 1
        }
        Write-Success "Inno Setup found"
    }
    
    # Check for .NET
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error ".NET SDK not found"
        exit 1
    }
    Write-Success ".NET SDK found (version $dotnetVersion)"
    
    # Check for required installer
    Set-Location $scriptPath
    $missingAssets = @()
    
    if (-not (Test-Path "SetupIcon.ico")) {
        $missingAssets += "SetupIcon.ico"
    }
    if (-not (Test-Path "WizardBanner.bmp")) {
        $missingAssets += "WizardBanner.bmp"
    }
    
    if ($missingAssets.Count -gt 0) {
        Write-Warning "Missing installer assets:"
        $missingAssets | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
        Write-Host "`nRun Prepare-InstallerAssets.ps1 to create them." -ForegroundColor Yellow
        Write-Host "Example: .\Prepare-InstallerAssets.ps1 -BannerImagePath 'path\to\banner.png' -IconPath 'path\to\icon.ico'" -ForegroundColor Gray
        
        $response = Read-Host "`nContinue anyway? (y/N)"
        if ($response -ne 'y' -and $response -ne 'Y') {
            exit 1
        }
    }
    else {
        Write-Success "All installer assets found"
    }

    # Build the application
    if (-not $SkipBuild) {
        Write-Step "Building CoreTray ($Architecture Release)..."
        
        Set-Location "$projectRoot\CoreTray"
        
        $buildArgs = @(
            "publish"
            "-c", "Release"
            "-r", "win10-$Architecture"
            "--self-contained", "false"
            "-p:Platform=$Architecture"
        )
        
        Write-Host "Running: dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
        & dotnet @buildArgs
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed"
            exit 1
        }
        
        Write-Success "Build completed"
        
        # Verify the executable exists
        $exePath = "bin\$Architecture\Release\net7.0-windows10.0.19041.0\win10-$Architecture\CoreTray.exe"
        if (-not (Test-Path $exePath)) {
            Write-Error "Executable not found at: $exePath"
            exit 1
        }
        Write-Success "Executable verified: $exePath"
    }
    else {
        Write-Warning "Skipping build (using existing files)"
    }

    # Step 3: Create output directory
    Write-Step "Preparing output directory..."
    
    Set-Location $scriptPath
    $outputDir = "Output"
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir | Out-Null
    }
    Write-Success "Output directory ready"

    # Compile installer
    if (-not $SkipInno) {
        Write-Step "Compiling installer with Inno Setup..."
        
        Write-Host "Running: $InnoSetupPath CoreTray.iss" -ForegroundColor Gray
        & $InnoSetupPath "CoreTray.iss"
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Inno Setup compilation failed"
            exit 1
        }
        
        Write-Success "Installer compiled successfully"
    }
    else {
        Write-Warning "Skipping Inno Setup compilation"
    }

    # Show results
    Write-Host "`n╔═══════════════════════════════════════╗" -ForegroundColor $ColorSuccess
    Write-Host "║         Build Completed! ✓            ║" -ForegroundColor $ColorSuccess
    Write-Host "╚═══════════════════════════════════════╝`n" -ForegroundColor $ColorSuccess
    
    $installerFiles = Get-ChildItem -Path $outputDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending
    if ($installerFiles.Count -gt 0) {
        Write-Host "Installer created:" -ForegroundColor $ColorSuccess
        $latestInstaller = $installerFiles[0]
        Write-Host "  File: $($latestInstaller.Name)" -ForegroundColor White
        Write-Host "  Size: $([math]::Round($latestInstaller.Length / 1MB, 2)) MB" -ForegroundColor White
        Write-Host "  Path: $($latestInstaller.FullName)" -ForegroundColor White
        Write-Host ""
        Write-Host "You can now test the installer by running it!" -ForegroundColor $ColorInfo
    }
    else {
        Write-Warning "No installer file found in output directory"
    }
}
catch {
    Write-Host "`n╔═══════════════════════════════════════╗" -ForegroundColor $ColorError
    Write-Host "║          Build Failed! ✗              ║" -ForegroundColor $ColorError
    Write-Host "╚═══════════════════════════════════════╝`n" -ForegroundColor $ColorError
    Write-Error "Error: $($_.Exception.Message)"
    Write-Host "`nStack trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}
finally {
    Set-Location $originalLocation
}

Write-Host ""
