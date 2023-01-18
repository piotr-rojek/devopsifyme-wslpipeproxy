docker build --tag devopsifyme/wslpipeproxy .
docker create --name output devopsifyme/wslpipeproxy
docker cp output:/install/ output/
docker container rm output



# docker container rm test --force
# docker run --name test --privileged --pid=host -v /mnt/c/:/app -e INSTALL_DIR=wslpipeproxy3 wslpipeproxy