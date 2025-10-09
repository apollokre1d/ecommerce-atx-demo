# E-Commerce API Testing Script
# This script starts the API and runs comprehensive tests

param(
    [string]$ConnectionString = "Server=3.67.133.184,1433;Database=ECommerceDB;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=true;MultipleActiveResultSets=true;",
    [string]$ApiPort = "5000",
    [switch]$SkipBuild = $false,
    [switch]$TestOnly = $false
)

Write-Host "🚀 E-Commerce API Testing Script" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Set location to the API project directory
$ApiPath = Join-Path $PSScriptRoot "src\ECommerce.API"
$TestClientPath = Join-Path $PSScriptRoot "tests\ECommerce.API.TestClient"

if (-not (Test-Path $ApiPath)) {
    Write-Host "❌ API project not found at: $ApiPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $TestClientPath)) {
    Write-Host "❌ Test client not found at: $TestClientPath" -ForegroundColor Red
    exit 1
}

# Function to check if port is available
function Test-Port {
    param([int]$Port)
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Port)
        $listener.Start()
        $listener.Stop()
        return $true
    }
    catch {
        return $false
    }
}

# Function to wait for API to be ready
function Wait-ForApi {
    param([string]$Url, [int]$TimeoutSeconds = 30)
    
    Write-Host "⏳ Waiting for API to be ready at $Url..." -ForegroundColor Yellow
    
    $timeout = (Get-Date).AddSeconds($TimeoutSeconds)
    
    while ((Get-Date) -lt $timeout) {
        try {
            $response = Invoke-WebRequest -Uri "$Url/api/info" -Method Get -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ API is ready!" -ForegroundColor Green
                return $true
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }
    
    Write-Host "❌ API failed to start within $TimeoutSeconds seconds" -ForegroundColor Red
    return $false
}

try {
    if (-not $TestOnly) {
        # Check if port is available
        if (-not (Test-Port -Port $ApiPort)) {
            Write-Host "❌ Port $ApiPort is already in use" -ForegroundColor Red
            Write-Host "   Please stop any existing API instances or choose a different port" -ForegroundColor Yellow
            exit 1
        }

        # Build the API project
        if (-not $SkipBuild) {
            Write-Host "🔨 Building API project..." -ForegroundColor Cyan
            Set-Location $ApiPath
            
            $buildResult = dotnet build --configuration Release
            if ($LASTEXITCODE -ne 0) {
                Write-Host "❌ Build failed" -ForegroundColor Red
                exit 1
            }
            Write-Host "✅ Build completed successfully" -ForegroundColor Green
            Write-Host ""
        }

        # Update connection string in appsettings
        Write-Host "🔧 Configuring connection string..." -ForegroundColor Cyan
        $appsettingsPath = Join-Path $ApiPath "appsettings.json"
        
        if (Test-Path $appsettingsPath) {
            $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
            $appsettings.ConnectionStrings.DefaultConnection = $ConnectionString
            $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
            Write-Host "✅ Connection string updated" -ForegroundColor Green
        }

        # Start the API
        Write-Host "🚀 Starting API on port $ApiPort..." -ForegroundColor Cyan
        Set-Location $ApiPath
        
        $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", "http://localhost:$ApiPort" -PassThru -WindowStyle Hidden
        
        if (-not $apiProcess) {
            Write-Host "❌ Failed to start API process" -ForegroundColor Red
            exit 1
        }

        Write-Host "✅ API process started (PID: $($apiProcess.Id))" -ForegroundColor Green
        
        # Wait for API to be ready
        $apiUrl = "http://localhost:$ApiPort"
        if (-not (Wait-ForApi -Url $apiUrl)) {
            Write-Host "❌ Stopping API process..." -ForegroundColor Red
            Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
            exit 1
        }
        
        Write-Host ""
        Write-Host "🌐 API is running at: $apiUrl" -ForegroundColor Green
        Write-Host "📖 Swagger UI available at: $apiUrl" -ForegroundColor Green
        Write-Host ""
    }

    # Build and run the test client
    Write-Host "🧪 Building test client..." -ForegroundColor Cyan
    Set-Location $TestClientPath
    
    if (-not $SkipBuild) {
        $buildResult = dotnet build --configuration Release
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Test client build failed" -ForegroundColor Red
            if ($apiProcess) { Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue }
            exit 1
        }
    }

    Write-Host "✅ Test client built successfully" -ForegroundColor Green
    Write-Host ""

    # Run the tests
    Write-Host "🧪 Running API tests..." -ForegroundColor Cyan
    Write-Host ""
    
    $testResult = dotnet run --configuration Release
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "🎉 All tests completed successfully!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ Some tests failed" -ForegroundColor Red
    }

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Clean up
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Write-Host ""
        Write-Host "🛑 Stopping API process..." -ForegroundColor Yellow
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        Write-Host "✅ API process stopped" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "📋 Testing Summary:" -ForegroundColor Cyan
Write-Host "   API URL: http://localhost:$ApiPort" -ForegroundColor White
Write-Host "   Swagger: http://localhost:$ApiPort" -ForegroundColor White
Write-Host "   Database: SQL Server at 3.67.133.184" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")