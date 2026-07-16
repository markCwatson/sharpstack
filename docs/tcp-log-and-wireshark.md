# Reading the TCP log with Wireshark

This guide explains one complete HTTP exchange as reported by sharpstack's TCP
log and shows how to inspect the same packets in Wireshark. For the underlying
state transitions, see [TCP connection state machine](tcp-state-machine.md).

## Where the connection comes from

In the Docker setup, a request takes this path:

```text
host curl
    -> Docker port 127.0.0.1:8080
    -> socat inside the container
    -> Linux TCP client at 10.0.0.1:<ephemeral-port>
    -> rawstack0
    -> sharpstack at 10.0.0.2:80
```

The TCP packets observed by sharpstack are therefore between `10.0.0.1` and
`10.0.0.2`. The source port `38836` in this example is an ephemeral port chosen
by the Linux kernel for socat's outbound connection. It will usually change on
the next request.

## Example exchange

The corrected endpoint labels and state snapshots produce a trace like this:

```text
TCP IN 10.0.0.2:80 <- 10.0.0.1:38836 flags=[SYN] seq=42765507 ack=0 payload=0B
TCP OUT 10.0.0.2:80 -> 10.0.0.1:38836 flags=[SYN, ACK] seq=10000 ack=42765508 payload=0B state=SynReceived
TCP IN 10.0.0.2:80 <- 10.0.0.1:38836 flags=[ACK] seq=42765508 ack=10001 payload=0B
TCP IN 10.0.0.2:80 <- 10.0.0.1:38836 flags=[PSH, ACK] seq=42765508 ack=10001 payload=77B
TCP OUT 10.0.0.2:80 -> 10.0.0.1:38836 flags=[ACK] seq=10001 ack=42765585 payload=0B state=Established
TCP OUT 10.0.0.2:80 -> 10.0.0.1:38836 flags=[PSH, ACK] seq=10001 ack=42765585 payload=122B state=Established
TCP OUT 10.0.0.2:80 -> 10.0.0.1:38836 flags=[FIN, ACK] seq=10123 ack=42765585 payload=0B state=FinWait1
TCP IN 10.0.0.2:80 <- 10.0.0.1:38836 flags=[ACK] seq=42765585 ack=10123 payload=0B
TCP IN 10.0.0.2:80 <- 10.0.0.1:38836 flags=[FIN, ACK] seq=42765585 ack=10124 payload=0B
TCP OUT 10.0.0.2:80 -> 10.0.0.1:38836 flags=[ACK] seq=10124 ack=42765586 payload=0B state=TimeWait
```

`TcpServer` logs outbound packets when it creates a response batch. It then
returns the encoded frames to `StackRunner`, which writes them to the TAP one at
a time. Consequently, all three `TCP OUT` lines can be printed before the peer
has an opportunity to send an ACK. The log preserves generation order, but it
does not prove exact wire interleaving. Use a packet capture for that.

## Field meanings

| Field            | Meaning                                                                    |
| ---------------- | -------------------------------------------------------------------------- |
| `IN` / `OUT`     | Packet direction relative to sharpstack                                    |
| `10.0.0.2:80`    | sharpstack's local IP address and TCP port                                 |
| `10.0.0.1:38836` | Linux/socat peer address and ephemeral port                                |
| `flags`          | TCP control bits set in this packet                                        |
| `seq`            | Sequence number of the first byte, SYN, or FIN in this packet              |
| `ack`            | Cumulative acknowledgment: the next sequence number expected from the peer |
| `payload`        | Number of application bytes carried by this packet                         |
| `state`          | Connection state immediately after the event that emitted this packet      |

The relevant flags are:

- `SYN`: establish a connection and synchronize initial sequence numbers.
- `ACK`: the acknowledgment field is valid.
- `PSH`: the packet carries application data that should be delivered promptly.
- `FIN`: the sender has no more bytes to send and is closing its direction.

SYN and FIN each consume one sequence number. Every payload byte consumes one
sequence number. A plain ACK consumes no sequence space.

## Line-by-line walkthrough

### 1. Peer sends SYN

```text
TCP IN ... flags=[SYN] seq=42765507 ack=0 payload=0B
```

The peer proposes initial sequence number `42765507`. SYN consumes one sequence
number, so sharpstack must next expect:

$$
42765507 + 1 = 42765508
$$

The ACK field is zero because the peer has not received a sequence number from
sharpstack yet.

### 2. Sharpstack sends SYN+ACK

```text
TCP OUT ... flags=[SYN, ACK] seq=10000 ack=42765508 payload=0B state=SynReceived
```

Sharpstack uses deterministic initial sequence number `10000`. Its ACK confirms
the peer's SYN by requesting `42765508`. Sharpstack's SYN also consumes one
sequence number, making its next send sequence `10001`.

### 3. Peer completes the handshake

```text
TCP IN ... flags=[ACK] seq=42765508 ack=10001 payload=0B
```

The peer acknowledges sharpstack's SYN by requesting sequence `10001`. The
packet carries no payload, so the peer's sequence remains `42765508`. The
connection enters `Established`.

### 4. Peer sends the HTTP request

```text
TCP IN ... flags=[PSH, ACK] seq=42765508 ack=10001 payload=77B
```

The HTTP request starts at sequence `42765508` and contains 77 bytes. After
accepting it, sharpstack expects:

$$
42765508 + 77 = 42765585
$$

The peer's `ack=10001` still says it has received everything from sharpstack up
to, but not including, sequence `10001`.

### 5. Sharpstack acknowledges the request

```text
TCP OUT ... flags=[ACK] seq=10001 ack=42765585 payload=0B state=Established
```

This is a cumulative ACK for all 77 request bytes. It has no payload and no SYN
or FIN, so it does not advance sharpstack's send sequence.

### 6. Sharpstack sends the HTTP response

```text
TCP OUT ... flags=[PSH, ACK] seq=10001 ack=42765585 payload=122B state=Established
```

The response starts at `10001` and contains 122 bytes. Sharpstack's next send
sequence becomes:

$$
10001 + 122 = 10123
$$

The ACK remains `42765585` because the peer has sent no additional sequence
space. The separate ACK in the previous line could eventually be optimized by
piggybacking it on this response.

### 7. Sharpstack starts active close

```text
TCP OUT ... flags=[FIN, ACK] seq=10123 ack=42765585 payload=0B state=FinWait1
```

The HTTP application requested `Connection: close`, so sharpstack sends FIN.
FIN occupies sequence `10123`, making sharpstack's next sequence `10124`. The
state becomes `FinWait1` while sharpstack waits for that FIN to be acknowledged.

### 8. Peer acknowledges response data

```text
TCP IN ... flags=[ACK] seq=42765585 ack=10123 payload=0B
```

This ACK covers the 122 response bytes through sequence `10122`. It does not yet
cover the FIN at `10123`. Because ACK consumes no sequence space, the peer's
sequence remains `42765585`. On a live capture this ACK can appear before the
FIN packet because `StackRunner` writes the response and FIN as separate frames,
even though the batched application log printed both outbound lines first.

### 9. Peer acknowledges FIN and sends its FIN

```text
TCP IN ... flags=[FIN, ACK] seq=42765585 ack=10124 payload=0B
```

`ack=10124` acknowledges sharpstack's FIN. The peer also sends its own FIN at
sequence `42765585`. That FIN consumes one sequence number, so sharpstack must
acknowledge `42765586`.

### 10. Sharpstack sends the final ACK

```text
TCP OUT ... flags=[ACK] seq=10124 ack=42765586 payload=0B state=TimeWait
```

Sharpstack acknowledges the peer FIN and enters `TimeWait`. TIME-WAIT allows it
to acknowledge a retransmitted FIN and prevents delayed packets from an old
connection being confused with a new connection using the same four-tuple.

## Capture on native Linux

Start sharpstack and capture the TAP interface with Wireshark:

```shell
sudo wireshark -i rawstack0 -k
```

Or capture to a file with tcpdump and inspect it later:

```shell
sudo tcpdump -i rawstack0 -s 0 -w sharpstack.pcap
```

Run `curl http://10.0.0.2:80/` in another terminal, stop tcpdump with `Ctrl+C`,
then open `sharpstack.pcap` in Wireshark.

## Capture with Docker Desktop

On Docker Desktop, `rawstack0` is inside the sharpstack container's Linux
network namespace. The runtime image does not include tcpdump, so use a
temporary diagnostic container that joins the same network namespace.

First start sharpstack and create a capture directory:

```shell
./scripts/run-docker.sh
mkdir -p captures
```

Then start the capture in a second terminal:

```shell
docker run --rm \
  --network container:sharpstack \
  --cap-add NET_RAW \
  --cap-add NET_ADMIN \
  --volume "$PWD/captures:/captures" \
  nicolaka/netshoot \
  tcpdump -i rawstack0 -s 0 -U \
  -w /captures/sharpstack.pcap \
  'arp or icmp or tcp port 80'
```

While tcpdump is running, make a request from another terminal:

```shell
curl -i http://127.0.0.1:8080/
```

Stop tcpdump with `Ctrl+C`, then open `captures/sharpstack.pcap` in Wireshark on
the host. The capture contains the TAP-side connection between `10.0.0.1` and
`10.0.0.2`; it does not contain the separate host-to-socat connection on port 8080.

A validated capture displayed with relative sequence numbers looked like this:

```text
10.0.0.1:55922 -> 10.0.0.2:80  SYN      seq=0
10.0.0.2:80 -> 10.0.0.1:55922  SYN,ACK  seq=0   ack=1
10.0.0.1:55922 -> 10.0.0.2:80  ACK              ack=1
10.0.0.1:55922 -> 10.0.0.2:80  PSH,ACK  seq=1   ack=1   len=77
10.0.0.2:80 -> 10.0.0.1:55922  ACK              ack=78
10.0.0.2:80 -> 10.0.0.1:55922  PSH,ACK  seq=1   ack=78  len=122
10.0.0.1:55922 -> 10.0.0.2:80  ACK              ack=123
10.0.0.2:80 -> 10.0.0.1:55922  FIN,ACK  seq=123 ack=78
10.0.0.1:55922 -> 10.0.0.2:80  FIN,ACK  seq=78  ack=124
10.0.0.2:80 -> 10.0.0.1:55922  ACK              ack=79
```

The relative values differ from the application log's absolute values, but the
increments are the same: 77 request bytes, 122 response bytes, and one sequence
number for each SYN and FIN.

## Useful Wireshark tools

Apply these display filters after opening the capture:

| Goal                                    | Display filter                                             |
| --------------------------------------- | ---------------------------------------------------------- |
| Only sharpstack HTTP traffic            | `ip.addr == 10.0.0.2 && tcp.port == 80`                    |
| One selected TCP conversation           | `tcp.stream eq 0`                                          |
| Connection establishment                | `tcp.flags.syn == 1`                                       |
| Connection close                        | `tcp.flags.fin == 1`                                       |
| Packets carrying data                   | `tcp.len > 0`                                              |
| Decoded HTTP messages                   | `http`                                                     |
| Retransmissions or out-of-order packets | `tcp.analysis.retransmission or tcp.analysis.out_of_order` |
| All implemented protocols               | `arp or icmp or tcp`                                       |

Useful investigations include:

1. Select a packet and expand **Transmission Control Protocol** to inspect flags,
   sequence numbers, acknowledgment numbers, window size, and checksum.
2. Right-click a TCP packet and choose **Follow > TCP Stream** to reconstruct the
   HTTP request and response.
3. Open **Statistics > Flow Graph**, choose the displayed TCP packets, and view
   the handshake, data, and close as a sequence diagram.
4. Open **Statistics > Conversations > TCP** to find the stream and byte counts.
5. Enable TCP checksum validation under **Preferences > Protocols > TCP** to
   check sharpstack's generated checksum. Capture-location and offload behavior
   can sometimes make kernel-generated checksums appear unverified.

Wireshark normally displays relative sequence numbers, starting each direction
near zero to make a conversation easier to read. The sharpstack log displays
the absolute wire values. To compare them directly, inspect `tcp.seq_raw` and
`tcp.ack_raw` in the packet details, add those fields as columns, or disable
**Relative sequence numbers** under **Preferences > Protocols > TCP**.

## What to look for when debugging

- A SYN with no SYN+ACK suggests listener lookup, packet parsing, or frame output
  failed.
- Repeated SYN or data packets suggest an ACK was absent, malformed, or lost.
- An ACK that does not equal the previous sequence plus payload length, SYN, or
  FIN indicates sequence-accounting trouble.
- A valid HTTP request with no response suggests the receive stream did not set
  `DataAvailable`, or the application considered the headers incomplete.
- A response without a final FIN suggests the application did not request close.
- Repeated FIN packets during TIME-WAIT are expected to receive the same final
  cumulative ACK.
