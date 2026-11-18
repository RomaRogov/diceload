# unity-dialog.ps1 -EditorPid PID [-Press NAME]
#   Windows half of unity-dialog (run that, from Git Bash): the modal dialogs a Unity Editor is waiting on,
#   read and pressed through UI Automation. Prints what UnityDialog.swift prints on macOS:
#     dialog: <title>
#       text: <each text>
#       buttons: "<name>" "<name>"
#   or "no dialog: …", "pressed "<name>"", "no button "<name>" — the dialogs are:", "couldn't press …".
param([Parameter(Mandatory = $true)][int]$EditorPid, [string]$Press)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$AE = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
function Where-Is($property, $value) { New-Object System.Windows.Automation.PropertyCondition($property, $value) }

# A dialog: a standard Win32 dialog window (#32770: message boxes, Save As) or any modal window of the
# Editor's process, at the top level or owned by the main window.
$dialogs = @()
foreach ($w in $AE::RootElement.FindAll($Scope::Children, (Where-Is $AE::ProcessIdProperty $EditorPid))) {
    $modal = $false
    try { $modal = $w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Current.IsModal } catch { }
    if ($w.Current.ClassName -eq '#32770' -or $modal) { $dialogs += $w }
    foreach ($c in $w.FindAll($Scope::Children, (Where-Is $AE::ClassNameProperty '#32770'))) { $dialogs += $c }
}

if ($dialogs.Count -eq 0) {
    'no dialog: the Editor isn''t waiting on one (it may be busy compiling or importing — wait, then try again)'
    exit 0
}

$report = ''
foreach ($d in $dialogs) {
    $texts = $d.FindAll($Scope::Descendants, (Where-Is $AE::ControlTypeProperty ([System.Windows.Automation.ControlType]::Text))) |
        ForEach-Object { $_.Current.Name } | Where-Object { $_ }
    $buttons = @($d.FindAll($Scope::Descendants, (Where-Is $AE::ControlTypeProperty ([System.Windows.Automation.ControlType]::Button))) |
        Where-Object { $_.Current.Name })
    if ($Press) {
        $b = $buttons | Where-Object { $_.Current.Name -eq $Press } | Select-Object -First 1
        if ($b) {
            try {
                $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
                "pressed `"$Press`""
                exit 0
            } catch {
                "couldn't press `"$Press`" ($($_.Exception.Message))"
                exit 1
            }
        }
    }
    $report += "dialog: $($d.Current.Name)`n"
    foreach ($t in $texts) { $report += "  text: $t`n" }
    $report += '  buttons: ' + (($buttons | ForEach-Object { "`"$($_.Current.Name)`"" }) -join ' ') + "`n"
}
if ($Press) { "no button `"$Press`" — the dialogs are:" }
[Console]::Write($report)
