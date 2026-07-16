#!/bin/sh

set -eu

ip tuntap add dev rawstack0 mode tap
ip addr add 10.0.0.1/24 dev rawstack0
ip link set rawstack0 up

dotnet App.dll &

exec socat TCP-LISTEN:8080,reuseaddr,fork TCP:10.0.0.2:80