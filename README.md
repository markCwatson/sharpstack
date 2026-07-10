# http server in C#

This is a full networking stqack and minimal http server written in C#12/.NET10 without the use of `System.Net`. The only .NET types I am going to use is `System.IO` for sockets.

All C# code is hand-written. Some documentation is written by AI/LLMs (this is to help guide me - but I may not follow it's suggestions exactly)

The goal is to run a local http server where I can do this

```shell
ping 10.0.0.2
```

and i will receive and ARP response, and this

```shell
curl http://10.0.0.2:8080/index.html
```

and it will return the `html` file.

Since I am using a TAP, it hast to be run on Linux.
