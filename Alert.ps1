param (
    [string]$Title,
    [string]$Body,
    [string]$Duration,
    [string]$AppId,
    [string]$IconPath
)

# 2. VERSION & ADMIN CHECK: Self-Upgrade and Self-Elevate
$pwshPath = "$env:ProgramFiles\PowerShell\7\pwsh.exe"

# Normalize Duration for XML (WPF gives "Short" or "Long")
$xmlDuration = if ($Duration -eq "Long") { "long" } else { "short" }

switch ($PSVersionTable.PSVersion.Major) {
    { $_ -lt 6 } {
        Write-Host "Current Version: 5.1. Checking for PowerShell 7..." -ForegroundColor Cyan
        
        if (-not (Test-Path $pwshPath)) {
            Write-Host "PowerShell 7 not found. Installing via Winget..." -ForegroundColor Yellow
            winget install --id Microsoft.Powershell --source winget --silent --accept-source-agreements --accept-package-agreements
        }

        if (Test-Path $pwshPath) {
            Write-Host "Re-launching in PowerShell 7 as Administrator..." -ForegroundColor Green
            # Re-pass parameters during elevation
            $argList = "-File `"$PSCommandPath`" -Title `"$Title`" -Body `"$Body`" -Duration `"$Duration`" -AppId `"$AppId`" -IconPath `"$IconPath`""
            Start-Process -FilePath $pwshPath -ArgumentList $argList -Verb RunAs
            exit 
        } else {
            Write-Warning "Upgrade failed. Proceeding with scan in 5.1..."
        }
    }
    Default {
        # Check if we are already Admin in PS7. If not, elevate.
        $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
        if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            Write-Host "Elevating PowerShell 7 to Administrator..." -ForegroundColor Green
            $argList = "-File `"$PSCommandPath`" -Title `"$Title`" -Body `"$Body`" -Duration `"$Duration`" -AppId `"$AppId`" -IconPath `"$IconPath`""
            Start-Process -FilePath $pwshPath -ArgumentList $argList -Verb RunAs
            exit
        }
    }
}

# 2.5 EXECUTION POLICY CHECK
if ((Get-ExecutionPolicy -Scope CurrentUser) -ne 'RemoteSigned') {
    Write-Host "Setting Execution Policy to RemoteSigned for CurrentUser..." -ForegroundColor Cyan
    Set-ExecutionPolicy RemoteSigned -Force -Scope CurrentUser
}

# 3. DISCOVERY: Scan Subnet and Resolve Names
$ipInfo = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127*" -and ($_.InterfaceAlias -like "*Ethernet*" -or $_.InterfaceAlias -like "*Wi-Fi*") } | Select-Object -First 1

if ($null -eq $ipInfo) {
    Write-Warning "No active network interface found. Defaulting to local PC."
    $liveComputers = @($env:COMPUTERNAME)
} else {
    $subnetPrefix = $ipInfo.IPAddress.Substring(0, $ipInfo.IPAddress.LastIndexOf('.') + 1)
    Write-Host "Scanning Subnet: ${subnetPrefix}0/24 and resolving names..." -ForegroundColor Cyan

    # High-speed parallel scan (requires PS7 ForEach-Object -Parallel)
    if ($PSVersionTable.PSVersion.Major -ge 7) {
        $liveComputers = 1..254 | ForEach-Object -Parallel {
            $ip = "$using:subnetPrefix$_"
            if (Test-Connection -ComputerName $ip -Count 1 -Quiet -TimeoutSeconds 1) {
                try {
                    $name = [System.Net.Dns]::GetHostEntry($ip).HostName.Split('.')[0]
                    Write-Host "Found: $ip -> $name" -ForegroundColor Green
                    return $name
                } catch {
                    Write-Host "Found: $ip (No DNS Name)" -ForegroundColor Yellow
                    return $ip
                }
            }
        } -ThrottleLimit 50
    } else {
        # Fallback for PS 5.1
        $liveComputers = 1..254 | ForEach-Object {
            $ip = "${subnetPrefix}$_"
            if (Test-Connection -ComputerName $ip -Count 1 -Quiet -TimeoutSeconds 1) {
                return $ip
            }
        }
    }
}

# Filter out any nulls
$liveComputers = $liveComputers | Where-Object { $_ }

# 4. EXECUTION: Variable Injection with Computer Name Identification
if ($liveComputers.Count -gt 0) {
    Write-Host "Sending alerts to $($liveComputers.Count) computers..." -ForegroundColor Green
    
    Invoke-Command -ComputerName $liveComputers -ScriptBlock {
        $title = $using:Title
        $body = $using:Body
        $duration = $using:xmlDuration
        $appId = $using:AppId
        $iconPath = $using:IconPath
        $thisPC = $env:COMPUTERNAME 

        try {
            [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
            $template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastImageAndText02)
            $toastXml = [xml]$template.GetXml()

            # Apply settings
            $toastXml.toast.SetAttribute("duration", $duration)
            
            if ($iconPath) {
                $imageNode = $toastXml.GetElementsByTagName("image")[0]
                $imageNode.SetAttribute("src", "file:///$iconPath") > $null
            }

            $displayTitle = "[$thisPC] - $title"
            
            $toastXml.GetElementsByTagName("text")[0].AppendChild($toastXml.CreateTextNode($displayTitle)) > $null
            $toastXml.GetElementsByTagName("text")[1].AppendChild($toastXml.CreateTextNode($body)) > $null
            
            $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
            $xml.LoadXml($toastXml.OuterXml)
            $toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
            
            Write-Host "Sending alert on $thisPC via $appId..." -ForegroundColor Green
            [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($appId).Show($toast)
        } catch {
            Write-Error "Failed to send notification on ${thisPC}: $($_.Exception.Message)"
        }
    } -ErrorAction SilentlyContinue
}
