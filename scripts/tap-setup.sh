#!/bin/bash

sudo ip tuntap add dev rawstack0 mode tap user "$USER"
sudo ip addr add 10.0.0.1/24 dev rawstack0
sudo ip link set rawstack0 up