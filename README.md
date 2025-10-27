# SilkBound

## Rollout Plan
Currently, SilkBound is planned to release in 3 major steps.

### 1. Open Alpha
- This version of the mod is *incomplete*.
- This version holds the barebones fundamentals for basic multiplayer support.
- Bugs reported may be disregarded if not crucial or planned for future builds.

### 2. Open Beta
- This version of the mod is *still incomplete*.
- Most, if not all features should be implemented
- Visual effects/features such as the skin library may be lackluster until full release

### 3. Open Release
- This version of the mod is finally *complete*
- Bug reports accepted and tracked
- Potential release event (YouTube video, Discord event, etc.)

<br>

## Technical Details
This section will cover a description of the frameworks SilkBound utilizes and provides

### 1. Networking Layers
- SilkBound is complete with a fully custom implemented Network Layer system.
- Each connection can be generalized into the following generic class:
#### `NetworkConnection`
- Generic, provides abstract methods such as `Send`
- In charge of entire network stack during implementation (to allow modularity)
- Essentially a persistent proxy between networked code and networking implementation.
#### `NetworkServer : NetworkConnection`
- Also generic, derives from `NetworkConnection`.
- Provides additional methods for `SendExcept`, `SendExcluding`, `SendIncluding`, etc.
- Required as the NetworkServer type is used to detect if the local connection is a server.
#### `Packet`
- SilkBound uses a custom packet protocol in this format: `[packetLength][nameLength][name][clientId][payload]`
- While packets are framed and techincally boundless, a constant is set that restricts the streams utilized by network layers to have a fixed buffer size of - at the time of writing this - 512 bytes. The updated value can be found in `SilkConstants.PACKET_BUFFER`.


IMCOMPLEtE
