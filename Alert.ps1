param (
    [string]$Title,
    [string]$Body,
    [string]$Duration,
    [string]$AppId,
    [string]$IconPath
)

# Ensure BurntToast module is available if needed, but we can also use built-in Windows APIs.
# For simplicity, let's assume BurntToast is preferred as per research.
# If not installed, this might fail, so let's use a fallback or assume it's there.

if (Get-Module -ListAvailable -Name BurntToast) {
    $params = @{
        Text = $Title, $Body
        AppLogo = $IconPath
        Duration = $Duration
        AppId = $AppId
    }
    New-BurntToastNotification @params
} else {
    Write-Output "BurntToast module not found. Installing..."
    Install-Module -Name BurntToast -Force -Scope CurrentUser
    $params = @{
        Text = $Title, $Body
        AppLogo = $IconPath
        Duration = $Duration
        AppId = $AppId
    }
    New-BurntToastNotification @params
}

Write-Output "Notification sent: $Title"
