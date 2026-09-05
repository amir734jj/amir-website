# Demystifying WebRTC, STUN, and TURN (and Why I Spun Up Coturn in Docker)

If you've ever tried to build a real-time chat app with peer-to-peer file sharing, you quickly hit a wall. In a perfect world, two browsers would just look at each other, shake hands, and start slinging encrypted files back and forth. 

In reality, routers, firewalls, and NAT (Network Address Translation) exist. They act like paranoid bouncers, dropping direct connections before they even reach your machine.

In my project, [**`symmetric-crypto-chat-room`**](https://chat.coolify.hesamian.com/)—an end-to-end encrypted chat room built with Blazor and SignalR—I wanted users to be able to exchange text messages, voice, and files (up to 50MB) without my server ever seeing the plaintext or storing the unencrypted data. Text message communication runs through SignalR, while voice and file transfers use WebRTC. To make file and voice connections work across the messy reality of the internet, I had to implement WebRTC and deploy a STUN/TURN server.

Here is the plain-English breakdown of what these technologies do, how they bypass network restrictions, and how I hosted my own Coturn relay using Docker.

---

## The Problem with Peer-to-Peer

Most devices don't have their own public IP address. Instead, your home router gets one public IP, and it assigns private, internal IPs (like `192.168.1.10`) to your laptop, phone, and TV. When you request a webpage, the router translates your private IP to the public one and keeps track of the connection.

But when your friend's browser tries to send a file *directly* to your browser, their request hits your router's public IP and the router has no idea which internal device the file is meant for. By default, it just drops the connection.

## STUN (The Public Mirror)

STUN (Session Traversal Utilities for NAT) is basically a mirror on the public internet. 

When your browser wants to connect to a peer, it first shouts out to a STUN server: *"Hey, what do I look like from the outside?"*

The STUN server looks at the request and replies: *"To the public internet, your traffic is coming from IP `203.0.113.45` on port `54321`."*

Your browser then takes that public IP and port and hands it to your friend (in my app, this handoff happens securely over the SignalR websocket). If both routers play nice, the browsers can now send traffic directly to those specific ports. This is called **"hole punching."** When it works, it's blazing fast, completely decentralized, and costs the server zero bandwidth.

## TURN (The Fallback Relay)

STUN works great for home networks, but what happens when a user is on strict corporate Wi-Fi or a cellular network with "Symmetric NAT"? These networks scramble ports dynamically and block unrecognized incoming traffic entirely. STUN hole-punching completely fails here.

Enter **TURN** (Traversal Using Relays around NAT).

If STUN is a mirror, TURN is a trusted middleman. When two browsers realize they simply cannot connect directly, they both connect to the TURN server and say, *"We can't reach each other. Please just pass our packets back and forth."*

Because TURN actually relays the raw binary data of the file transfer, it eats up server bandwidth. This is why Google provides free public STUN servers for discovery, but nobody hosts free public TURN servers for your heavy file transfers. You have to host your own.

## Hosting Coturn in Docker

To guarantee file transfers actually complete regardless of the users' network setups, I deployed my own STUN/TURN server using [**Coturn**](https://github.com/coturn/coturn), the gold standard open-source implementation.


## Tying it all together in Blazor

When a user opens the chat room, the Blazor WebAssembly app initializes the WebRTC `RTCPeerConnection`. It passes the Coturn server credentials directly into the configuration.

SignalR handles the room's text message communication and WebRTC signaling. Text messages are encrypted in the browser before they are sent, and voice frames are encrypted with AES-256-CTR before they travel over WebRTC. In other words, the chat's text, voice, and file content is encrypted client-side; the server only relays or stores ciphertext.

When you drag and drop a file into the chat:
1. The app encrypts the file locally in your browser using **AES-256-CTR** and the shared room passphrase.
2. The file is sliced into small binary chunks.
3. If the network allows it, STUN punches a hole and the encrypted chunks fly directly peer-to-peer to the other user.
4. If firewalls block the direct route, the Dockerized Coturn server quietly catches the encrypted chunks and relays them to the recipient.
5. The recipient's browser reassembles the chunks and decrypts the file locally.

My server never sees the plaintext chat, never has the keys to decrypt the files, and users never have to configure their router settings to make it work.