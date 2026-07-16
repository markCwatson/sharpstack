# Full networking stack + minimal http server in C#/.NET

[![.NET tests](https://github.com/markCwatson/sharpstack/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/markCwatson/sharpstack/actions/workflows/tests.yml)

This is a full networking stack and minimal http server written in C#12/.NET10 without the use of `System.Net`.

Most of the C# code is hand-written (with the exception of tests which are 100% AI gernerated).

## Function

The goal is to run a local http server where I can do this

```shell
ping 10.0.0.2
```

and I will receive an ICMP echo response, and this

```shell
curl http://10.0.0.2:80/
```

and it will return an HTTP response from the custom TCP stack.

## Running on Linux

Since I am using a TAP, it has to be run on Linux. I have to use a TAP because for a real http server like nginx, the kernel
handles the networking stack + socket.

```
NIC -> kernel Ethernet/IP/TCP -> socket -> nginx
```

And since I want to implement a simple Ethernet/ARP/IPv4/ICMP/TCP stack, then I need to bypass the kernel's protocol processing of these frames
simulating as if I was reading directly from the NIC. A TAP device acts as a virtual Layer 2 Ethernet interface.

To setup the TAP on Linux, run

```shell
./scripts/tap-setup.sh
```

then run the server

```shell
./scripts/run.sh
```

Then call it from the host:

```shell
curl -i http://10.0.0.2:8080/
```

## Run with Docker

The Docker setup requires a Linux Docker daemon with `/dev/net/tun` support.
It works with Docker Engine on Linux and Docker Desktop's Linux VM on macOS.
The run script grants the container access to `/dev/net/tun` and the
`NET_ADMIN` capability so it can create `rawstack0`.

The application reads Ethernet frames rather than opening a kernel TCP socket,
so publishing a container port directly to port 80 would not work. The image
uses `socat` as a host-facing TCP proxy:

```text
curl 127.0.0.1:8080
    -> Docker port forwarding
    -> socat listener on container port 8080
    -> Linux TCP connection over rawstack0
    -> sharpstack at 10.0.0.2:80
```

Build and start the container:

```shell
./scripts/run-docker.sh
```

Then call it from the host:

```shell
curl -i http://127.0.0.1:8080/
```

Useful verification commands:

```shell
docker ps --filter name=sharpstack
docker exec sharpstack ip address show rawstack0
docker exec sharpstack ps -ef
docker logs sharpstack
```

See [Reading the TCP log with Wireshark](docs/tcp-log-and-wireshark.md) for a
line-by-line explanation of the TCP output and instructions for capturing the
TAP traffic.

Stop the container:

```shell
docker stop sharpstack
```

On Docker Desktop, `10.0.0.2` belongs to a TAP network inside the Linux VM and
is not directly reachable from the macOS host. Use the published loopback port.
On native Linux, the non-Docker TAP setup can expose `10.0.0.2` directly.
