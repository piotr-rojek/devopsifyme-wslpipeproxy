[CmdletBinding()]
param(
    $HostPath = $env:HOST_PATH ?? "/mnt/c/wslpipeproxy",

    $ForwardDistribution = $env:F_DISTRIBUTION ?? "Ubuntu-22.04",
    $ForwardNpipe = $env:F_NPIPE ?? "dockerOnWSL" + ($ForwardDistribution -replace '[^\w]',''),
    $ForwardUnix = $env:F_UNIX ??'/run/docker.sock'
)

Get-ChildItem env:

$InstallDir =  Split-Path $HostPath -Leaf
$installPath = "/app/" + $InstallDir

Write-Host "Installation path: $installPath; Host installation path: $HostPath"

# Copy binaries to windows share, /app directory has to be mounted to HOST_PATH
Write-Host "Copying application to $installPath"
New-Item -Type Directory $installPath -Force | Out-Null
Copy-Item /install/* $installPath -Recurse

Write-Host "To register your docker context on Windows run..."
Write-Host "  docker context create $ForwardNpipe --docker host=npipe:////./pipe/$ForwardNpipe"
Write-Host "  docker context use $ForwardNpipe"

# Execute commands on the host node (WSL), requires priviledged and pid=host
Set-Location $installPath
Write-Host "Running application on host in directory: $HostPath"
$parameters = " --forwardings:0:distribution $ForwardDistribution --forwardings:0:npipe $ForwardNpipe --forwardings:0:unix $ForwardUnix"
Write-Host "Running nsenter -t 1 -m -u -i -n -- sh -c ""cd $HostPath && $HostPath/wslpipeproxy.exe $parameters"""
nsenter -t 1 -m -u -n -i sh -c "cd $HostPath && $HostPath/wslpipeproxy.exe $parameters"