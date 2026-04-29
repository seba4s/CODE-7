param(
    [string]$UnityExe = "",
    [string]$ProjectPath = ""
)

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    Write-Host "Pasa la ruta a Unity.exe con -UnityExe."
    Write-Host "Ejemplo:"
    Write-Host ".\\build-windows.ps1 -UnityExe \"C:\\Program Files\\Unity\\Hub\\Editor\\6000.4.4f1\\Editor\\Unity.exe\""
    exit 1
}

& "$UnityExe" `
    -batchmode `
    -quit `
    -projectPath "$ProjectPath" `
    -executeMethod WindowsBuild.BuildWindowsFromCli `
    -logFile -

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build fallo con codigo $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Build completado. Revisa Builds/Windows dentro del proyecto."
