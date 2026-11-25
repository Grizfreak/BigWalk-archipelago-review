# How the mod works

## Who talks to whom

```mermaid
flowchart LR
    AP[(Archipelago server)]
    subgraph Host["Host's PC"]
        direction TB
        Conn["ApConnection<br/>socket, its own thread"]
        Run["ApRuntime<br/>main thread, every frame"]
        Patches["Harmony patches<br/>watch the game"]
        Core["Core<br/>items, keys, doors, traps..."]
        Chan["ModChannel<br/>Mirror message 0xB1A9"]
    end
    subgraph Guest["A guest's PC"]
        GChan["ModChannel"]
        GCore["Core: what is shown"]
    end
    AP <-->|"WebSocket<br/>Archipelago.MultiClient.Net"| Conn
    Conn <--> Run
    Patches -->|checks| Run
    Run -->|items received| Core
    Run --> Chan
    Chan <-->|"Mirror: the game's network"| GChan
    GChan --> GCore
```

Only the host connects to the server. A guest never sends a check: what it does goes to the host through the game's own
network.

## A check made by a guest

```mermaid
sequenceDiagram
    participant G as Guest
    participant M as Mirror
    participant H as Host
    participant S as Server
    G->>M: an action in the game (the game's own command)
    M->>H: the host's game changes state
    Note over H: a patch sees it, CheckTracker marks it, ApRuntime queues it
    H->>S: LocationChecks
```

Nothing in the guest's mod: the game sends the action to the host, which is the authority.

## Losing the server and finding it again

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Connecting: TickReconnect
    Connecting --> Connected: connected and logged in
    Connecting --> Failed: refused, or no answer after 15 s
    Connected --> Failed: socket closed
    Failed --> Connecting: tried again every 10 s
    Connected --> Connected: OnJustConnected resends the checks, then the item cursor
```

Checks made without a connection are in the save (`ap_reported_*`): they all go again on the next connection.
