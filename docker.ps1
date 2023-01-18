docker build --tag devopsifyme/wslpipeproxy:0.2.0 .
docker container rm test --force
docker run --name test --privileged --pid=host -v /mnt/c/:/app -e INSTALL_DIR=wslpipeproxy3 wslpipeproxy