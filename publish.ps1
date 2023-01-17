dotnet publish -c Release -p:PublishSingleFile=true --self-contained false

New-Item -Type Directory output -Force
Compress-Archive `
    -Path src\DevOpsifyMe.WslPipeProxy\bin\Release\net7.0\win-x64\publish\* `
    -DestinationPath output\wslpipeproxy-winx64.zip -Force
