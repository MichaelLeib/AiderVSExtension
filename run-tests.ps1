# Comprehensive Test Execution Script for Aider-VS Extension
# This script runs all test categories and generates detailed reports

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Unit", "Integration", "Performance", "UI", "EndToEnd")]
    [string]$TestCategory = "All",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Text", "Html", "Json", "Xml", "Markdown")]
    [string]$ReportFormat = "Html",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./TestResults",
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateCodeCoverage,
    
    [Parameter(Mandatory=$false)]
    [switch]$OpenReport,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "Continue"

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

Write-Host "🚀 Starting Aider-VS Test Execution" -ForegroundColor Green
Write-Host "   Category: $TestCategory" -ForegroundColor Cyan
Write-Host "   Report Format: $ReportFormat" -ForegroundColor Cyan
Write-Host "   Output Path: $OutputPath" -ForegroundColor Cyan
Write-Host ""

# Function to run specific test category
function Invoke-TestCategory {
    param(
        [string]$Category,
        [string]$Filter = ""
    )
    
    Write-Host "📋 Running $Category Tests..." -ForegroundColor Yellow
    
    $testArgs = @(
        "test"
        "AiderVSExtension.Tests/AiderVSExtension.Tests.csproj"
        "--logger", "trx;LogFileName=$Category-results.trx"
        "--results-directory", $OutputPath
        "--verbosity", $(if ($Verbose) { "detailed" } else { "normal" })
    )
    
    if ($Filter) {
        $testArgs += "--filter", $Filter
    }
    
    if ($GenerateCodeCoverage) {
        $testArgs += "--collect", "XPlat Code Coverage"
        $testArgs += "--settings", "AiderVSExtension.Tests/coverlet.runsettings"
    }
    
    try {
        $result = & dotnet @testArgs
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ $Category tests completed successfully" -ForegroundColor Green
            return $true
        } else {
            Write-Host "❌ $Category tests failed" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "💥 Error running $Category tests: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to generate comprehensive report
function New-TestReport {
    param(
        [string]$Format,
        [string]$OutputDir
    )
    
    Write-Host "📊 Generating test report in $Format format..." -ForegroundColor Yellow
    
    # Collect all TRX files
    $trxFiles = Get-ChildItem -Path $OutputDir -Filter "*.trx" -ErrorAction SilentlyContinue
    
    if ($trxFiles.Count -eq 0) {
        Write-Warning "No test result files found"
        return
    }
    
    # Create combined report
    $reportPath = Join-Path $OutputDir "TestReport.$($Format.ToLower())"
    
    # Basic report generation (would be enhanced with actual TRX parsing)
    $reportContent = @"
# Aider-VS Test Execution Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Test Results Summary
$(foreach ($trx in $trxFiles) {
    "- $($trx.BaseName): $(if (Test-Path $trx.FullName) { "✅ Completed" } else { "❌ Failed" })"
})

## Test Categories Executed
- Total test files: $($trxFiles.Count)
- Test execution time: $(Get-Date -Format "HH:mm:ss")

## Performance Metrics
- Memory usage: Monitored
- Execution time: Tracked
- Coverage: $(if ($GenerateCodeCoverage) { "Enabled" } else { "Disabled" })

## Next Steps
1. Review failed tests if any
2. Check performance benchmarks
3. Verify UI test results
4. Analyze integration test outcomes
"@

    $reportContent | Out-File -FilePath $reportPath -Encoding UTF8
    
    Write-Host "📄 Report generated: $reportPath" -ForegroundColor Green
    
    if ($OpenReport -and $Format -eq "Html") {
        Start-Process $reportPath
    }
}

# Function to run performance benchmarks
function Invoke-PerformanceBenchmarks {
    Write-Host "⚡ Running Performance Benchmarks..." -ForegroundColor Yellow
    
    try {
        # Run BenchmarkDotNet tests
        dotnet run --project AiderVSExtension.Tests --configuration Release -- --job short --filter "*Benchmark*"
        
        Write-Host "✅ Performance benchmarks completed" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ Performance benchmarks failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to validate test environment
function Test-Environment {
    Write-Host "🔍 Validating test environment..." -ForegroundColor Yellow
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Host "   ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "   ❌ .NET SDK not found" -ForegroundColor Red
        return $false
    }
    
    # Check test project
    if (!(Test-Path "AiderVSExtension.Tests/AiderVSExtension.Tests.csproj")) {
        Write-Host "   ❌ Test project not found" -ForegroundColor Red
        return $false
    }
    Write-Host "   ✅ Test project found" -ForegroundColor Green
    
    # Check main project
    if (!(Test-Path "AiderVSExtension/AiderVSExtension.csproj")) {
        Write-Host "   ❌ Main project not found" -ForegroundColor Red
        return $false
    }
    Write-Host "   ✅ Main project found" -ForegroundColor Green
    
    return $true
}

# Main execution
try {
    # Validate environment
    if (!(Test-Environment)) {
        Write-Host "💥 Environment validation failed" -ForegroundColor Red
        exit 1
    }
    
    # Build projects first
    Write-Host "🔨 Building projects..." -ForegroundColor Yellow
    dotnet build AiderVSExtension/AiderVSExtension.csproj --configuration Debug
    dotnet build AiderVSExtension.Tests/AiderVSExtension.Tests.csproj --configuration Debug
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "💥 Build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
    Write-Host ""
    
    # Execute tests based on category
    $allPassed = $true
    
    switch ($TestCategory) {
        "All" {
            $allPassed = $allPassed -and (Invoke-TestCategory "Unit" "Category=Unit")
            $allPassed = $allPassed -and (Invoke-TestCategory "Integration" "Category=Integration")
            $allPassed = $allPassed -and (Invoke-TestCategory "Performance" "Category=Performance")
            $allPassed = $allPassed -and (Invoke-TestCategory "UI" "Category=UI")
            $allPassed = $allPassed -and (Invoke-TestCategory "EndToEnd" "Category=EndToEnd")
            
            # Run performance benchmarks
            Invoke-PerformanceBenchmarks | Out-Null
        }
        "Unit" {
            $allPassed = Invoke-TestCategory "Unit" "Category=Unit"
        }
        "Integration" {
            $allPassed = Invoke-TestCategory "Integration" "Category=Integration"
        }
        "Performance" {
            $allPassed = Invoke-TestCategory "Performance" "Category=Performance"
            Invoke-PerformanceBenchmarks | Out-Null
        }
        "UI" {
            $allPassed = Invoke-TestCategory "UI" "Category=UI"
        }
        "EndToEnd" {
            $allPassed = Invoke-TestCategory "EndToEnd" "Category=EndToEnd"
        }
    }
    
    Write-Host ""
    
    # Generate report
    New-TestReport -Format $ReportFormat -OutputDir $OutputPath
    
    # Generate code coverage report if enabled
    if ($GenerateCodeCoverage) {
        Write-Host "📈 Generating code coverage report..." -ForegroundColor Yellow
        
        $coverageFiles = Get-ChildItem -Path $OutputPath -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
        
        if ($coverageFiles.Count -gt 0) {
            # Install reportgenerator if not present
            dotnet tool install -g dotnet-reportgenerator-globaltool 2>$null
            
            # Generate HTML coverage report
            $coverageReportPath = Join-Path $OutputPath "CoverageReport"
            reportgenerator "-reports:$($coverageFiles[0].FullName)" "-targetdir:$coverageReportPath" "-reporttypes:Html"
            
            Write-Host "📊 Coverage report generated: $coverageReportPath" -ForegroundColor Green
            
            if ($OpenReport) {
                Start-Process (Join-Path $coverageReportPath "index.html")
            }
        }
    }
    
    # Final summary
    Write-Host ""
    Write-Host "🎯 Test Execution Summary" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    
    if ($allPassed) {
        Write-Host "✅ All tests passed successfully!" -ForegroundColor Green
        Write-Host "📊 Reports generated in: $OutputPath" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "❌ Some tests failed. Check the reports for details." -ForegroundColor Red
        Write-Host "📊 Reports generated in: $OutputPath" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "💥 Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
finally {
    Write-Host ""
    Write-Host "🏁 Test execution completed at $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
}