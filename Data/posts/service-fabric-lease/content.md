# Visualizing Service Fabric Cluster Ring Formation with Avalonia

When building resilient, distributed systems, one of the fundamental challenges is node health monitoring and failure detection without relying on a central coordinator. Service Fabric solves this through a low-level, peer-to-peer **Lease Subsystem**.

To better understand how node heartbeat exchanges, lease relationships, and federation rings behave under various network conditions, I built [**`dotnet-lease-simulation`**](https://lease-simulation.coolify.hesamian.com/) an interactive visual simulator written in C# using **Avalonia UI**.

## Background

Service Fabric uses a concept called **"lease"** to create a federation layer and ring to form a cluster.

In distributed computing, heartbeat mechanisms often suffer from split-brain scenarios or asymmetric network partitions where Node `A` can talk to Node `B`, but Node `B` cannot talk to Node `A`. Service Fabric avoids these pitfalls by introducing explicit, bilateral lease contracts managed directly at the transport level.

### Key Concepts of Service Fabric Leases:

1. **Lease Agent & Layer**: Operating right above the transport layer, the Lease Subsystem provides fault detection for the upper Federation layer.
2. **Lease Relationships (Subject & Monitor)**:
    - A node acts as both a **Lease Subject** (promising to stay alive) and a **Lease Monitor** (verifying another node is alive).
    - Leases are two-way relationships: Node A holds a lease on Node B while Node B holds a lease on Node A.
3. **Lease Duration & Renewals**:
    - Each lease contract has a strict expiration time (*Lease Duration*).
    - Nodes periodically exchange `LeaseRenew` requests and acknowledgments.
    - If a lease fails to renew before the duration expires, the lease is terminated, and the failure is reported to the Federation layer.
4. **Federation Ring**:
    - Nodes arrange themselves in a logical ring based on 128-bit Node IDs.
    - Each node establishes lease relationships with its immediate ring neighbors (predicessors and successors).
    - This ring topology allows the cluster to maintain consensus, detect departed or dead nodes, and re-balance partitions safely.

## Why?

Because it's cool and **Avalonia** is great. It can output both Desktop app and WebApp.

### Why Visualizing Leases Matters
Understanding Service Fabric's lease layer purely through text or trace logs can be abstract. Being able to visually inspect how nodes ping their predecessors, manage clock skew safety margins, and isolate failing nodes provides intuitive insight into distributed consensus mechanics.

