FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

COPY ./ /src/
WORKDIR /src
RUN dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true --no-cache -o /output

FROM mcr.microsoft.com/powershell:lts-alpine-3.14

COPY --from=build /output/ /install
COPY ./docker/ /home/

WORKDIR /home
CMD [ "pwsh", "-File", "initialize.ps1" ]