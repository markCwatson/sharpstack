# (wip) Network Stack

Since I want to work with raw sockets, and since I want to implement my own ARP, I need a TUN/TAP which is a virtual network interface in Linux.

The stack will look something like

```shell
HTTP/1.1
TCP
IPv4+ICMP
ARP
Ethernet
```
