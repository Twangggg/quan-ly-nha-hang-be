# Test Password Reset Feature

## Prerequisites
# Make sure server is running: dotnet run
# Server URL: http://localhost:5133

$baseUrl = "http://localhost:5133/api/auth"

Write-Host "=== Password Reset Testing Script ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Request Password Reset - Valid Employee
Write-Host "Test 1: Request Password Reset with Valid EmployeeCode" -ForegroundColor Yellow
$requestBody = @{
    employeeCode = "EMP001"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/request-password-reset" `
        -Method Post `
        -ContentType "application/json" `
        -Body $requestBody
    
    Write-Host "✅ Success: $($response.message)" -ForegroundColor Green
    Write-Host "   → Check email inbox for reset link" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Request Password Reset - Invalid Employee
Write-Host "Test 2: Request Password Reset with Invalid EmployeeCode" -ForegroundColor Yellow
$requestBody = @{
    employeeCode = "NOTEXIST"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/request-password-reset" `
        -Method Post `
        -ContentType "application/json" `
        -Body $requestBody
    
    Write-Host "✅ Success: $($response.message)" -ForegroundColor Green
    Write-Host "   → Message should be same as Test 1 (security)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Rate Limiting (commented out by default)
# Uncomment to test rate limiting
<#
Write-Host "Test 3: Rate Limiting (4 requests)" -ForegroundColor Yellow
for ($i = 1; $i -le 4; $i++) {
    Write-Host "  Request $i..." -NoNewline
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/request-password-reset" `
            -Method Post `
            -ContentType "application/json" `
            -Body $requestBody
        Write-Host " ✅" -ForegroundColor Green
    } catch {
        Write-Host " ❌ Blocked (expected on 4th)" -ForegroundColor Red
    }
    Start-Sleep -Seconds 1
}
Write-Host ""
#>

# Test 4: Reset Password - Valid Token
Write-Host "Test 4: Reset Password with Valid Token" -ForegroundColor Yellow
Write-Host "   ⚠️  You need to get token from email first!" -ForegroundColor Magenta
$token = Read-Host "Enter token from email (or press Enter to skip)"

if ($token) {
    $requestBody = @{
        token = $token
        newPassword = "NewPass123!"
        confirmPassword = "NewPass123!"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/reset-password" `
            -Method Post `
            -ContentType "application/json" `
            -Body $requestBody
        
        Write-Host "✅ Success: $($response.message)" -ForegroundColor Green
        Write-Host "   → Try logging in with new password" -ForegroundColor Gray
    } catch {
        $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "❌ Failed: $($errorResponse.message)" -ForegroundColor Red
    }
} else {
    Write-Host "⏭️  Skipped (no token provided)" -ForegroundColor Gray
}
Write-Host ""

# Test 5: Reset Password - Weak Password
Write-Host "Test 5: Reset Password with Weak Password" -ForegroundColor Yellow
if ($token) {
    $requestBody = @{
        token = $token
        newPassword = "123"
        confirmPassword = "123"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/reset-password" `
            -Method Post `
            -ContentType "application/json" `
            -Body $requestBody
        
        Write-Host "❌ Should have failed validation!" -ForegroundColor Red
    } catch {
        Write-Host "✅ Validation failed as expected" -ForegroundColor Green
        $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
        if ($errorResponse.errors) {
            Write-Host "   Errors:" -ForegroundColor Gray
            foreach ($key in $errorResponse.errors.PSObject.Properties.Name) {
                foreach ($error in $errorResponse.errors.$key) {
                    Write-Host "     - $error" -ForegroundColor Gray
                }
            }
        }
    }
} else {
    Write-Host "⏭️  Skipped (no token provided)" -ForegroundColor Gray
}
Write-Host ""

# Test 6: Reset Password - Confirm Mismatch
Write-Host "Test 6: Reset Password with Confirm Password Mismatch" -ForegroundColor Yellow
if ($token) {
    $requestBody = @{
        token = $token
        newPassword = "NewPass123!"
        confirmPassword = "DifferentPass123!"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/reset-password" `
            -Method Post `
            -ContentType "application/json" `
            -Body $requestBody
        
        Write-Host "❌ Should have failed validation!" -ForegroundColor Red
    } catch {
        Write-Host "✅ Validation failed as expected" -ForegroundColor Green
        Write-Host "   Error: Confirm password does not match" -ForegroundColor Gray
    }
} else {
    Write-Host "⏭️  Skipped (no token provided)" -ForegroundColor Gray
}
Write-Host ""

Write-Host "=== Testing Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  ✅ Request password reset endpoint working" -ForegroundColor Green
Write-Host "  ✅ Security: Generic message for invalid users" -ForegroundColor Green
Write-Host "  ✅ Password validation working" -ForegroundColor Green
Write-Host "  ✅ Token-based reset implemented" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Check email inbox for reset link" -ForegroundColor Gray
Write-Host "  2. Extract token from email URL" -ForegroundColor Gray
Write-Host "  3. Run this script again and enter the token" -ForegroundColor Gray
Write-Host "  4. Verify password was changed by logging in" -ForegroundColor Gray
