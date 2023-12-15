# DevOpsify Me - WSL Pipe Proxy

Tool for exposing unix sockets inside of WSL to Windows applications like Docker CLI. One popular use case is exposing docker.sock from WSL to docker cli running on Windows. 

Architecture:
```
DockerCLI <-> NPIPE (Our Proxy) <-> wsl.exe <-> STDIN/OUT (socat) <-> docker.sock <-> dockerd
```

We have two proxies, one this on Windows, other on Linux socat. They talk to each other via Standard In/Out pipes. Windows proxy listens for incoming connections on named pipe, to which applications connect. For each new connection socat process is started and all traffic is forwarded to unix socket as a new connection.

Check [DevOpsify Me](https://devopsifyme.com) for more information.

## Use Cases

* Running Docker on WSL, accessing from Windows Host
* Replacing Docker Desktop, Rancher Desktop

## Installation

We recommend installing as a container - see https://hub.docker.com/r/devopsifyme/wslpipeproxy. Also possible to register as a Windows service `sc.exe create DevOpsifyMeWslPipeProxy binpath= PathToThePublishFolder\wslpipeproxy.exe`.

### Docker
Edit the settings:

- F_DISTRIBUTION: Default "Ubuntu-22.04"
- F_NPIPE: Default "dockerOnWSLUbuntu2204"
- F_UNIX: Default "/run/var/docker.sock"
- HOST_PATH: Default "/mnt/c/wslpipeproxy"

```sh
sudo docker run -d --name wslpipeproxy --env  F_UNIX="" --env  F_NPIPE="" --env F_DISTRIBUTION="" --privileged --pid=host  --restart=always  -v /mnt/c/:/app  devopsifyme/wslpipeproxy
```

## Manual installation - docker.sock

> Note that the easiest is to simply run a container https://hub.docker.com/r/devopsifyme/wslpipeproxy. But you are welcome to also run the tool as a Windows service or just straight from command line as shown below.

Prepare your WSL instance, here we assume Ubuntu 22.04. 
* check if net7 runitme is installed `dotnet --list-runtimes`
* enable systemd support https://learn.microsoft.com/en-us/windows/wsl/wsl-config#systemd-support
* add yourself to docker group so that we have access to docker.sock
* install docker, if not already installed
* install socat, if not already installed

```sh
sudo groupadd docker
sudo gpasswd -a $USER docker

sudo snap install docker
sudo apt install socat
```

Edit appsettings.json and configure fowarding towards your distribution `wsl --list`:

```json
"Forwardings": [
    {
      "Distribution": "Ubuntu-22.04",
      "Npipe": "dockerOnWSLUbuntu2204",
      "Unix": "/run/var/docker.sock"
    }
  ]
```

Then add docker context and activate it

```ps
docker context create docker-on-ubuntu2204 --docker host=npipe:////./pipe/dockerOnWSLUbuntu2204
docker context use docker-on-ubuntu2204
```

Run the proxy `wslpipeproxy.exe`, making sure that appsettings.json is in the working directory.

## Publish

Requires [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download)

```ps
dotnet publish -c Release -p:PublishSingleFile=true --self-contained false
```

## Remarks

This is Proof of Concept work at this point - kindly please report any issues found.