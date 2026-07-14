#!/bin/bash

echo "pinging"
ping 10.0.0.2 -c3
echo " "

sleep 3

echo "curling"
echo "curl -i http://10.0.0.2:80/index.html"
sleep 1
curl -i http://10.0.0.2:80/index.html