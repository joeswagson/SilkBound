
# SilkBound

## Release Zip Guide
#### The zip archives containing the release binaries are organized by 2 variables:
### Loader
- **BepInEx**: This version is the most common for silksong, as the community uses BepInEx as the primary modloader.
- **MelonLoader**: This version is primarily what SilkBound is developed using. If BepInEx wont load, or is outright broken, this may be an option to consider.
### Environent
- **Release**: Compiler optimizations and less verbose logging. Typically more efficient, but may not be the preferred build when using a pre-release.
- **Debug**: This is a build that assigns internal variables to register the mod as being in **debug** mode. This means more logging, and potentially slower runtimes if additional monitoring is taking up resources.

## Client Config Guide
### Username
- The username used when hosting or connecting to a server.
- Default: `Weaver`
### SkinName
- The name of the skin to be used by the player. This must match the name the skin is registered under.
- Default: `red` (Normal Hornet)
### HostIP
- The identifier to use when setting up a server. This varies depending on the current **NetworkLayer**:
- - **TCP**: The IP address to bind the `TcpListener` to. `0.0.0.0` will bind to any available address.
- - **Steam**: This field is unused, and will be ignored; Steam uses whatever account is actively registered to the Steam API -- this is most likely whatever you're logged into from the Steam desktop app.
- - **NamedPipe**: The name of the pipe server
- Default: `0.0.0.0` (**TCP**)
### ConnectIP
- The identifier to use when finding and connecting to a server. This varies depending on the current **NetworkLayer**:
- - **TCP**: The IP address to connect to. If the other user is not on your home Wi-Fi network, they must have a port forwarded to their devices IP address. You will need to specify this port later in the configuration. 
- - **Steam**: The Steam ID of the user to join.
- - **NamedPipe**: The name of the pipe server
- Default: `127.0.0.1` (**TCP**)
### NetworkLayer
- 0 = `TCP` - Uses a reliable TCP connection for all network transfers.
- 1 = `Steam` - Uses Steam networking to send P2P packets using the reliable channel. Unreliable channels will be supported in a later release. This layer is not used primarily during SilkBound's development, so its reliability isn't guaranteed. If you encounter any network-related issues, consider switching to the **TCP** layer. The host will need to match this, as per usual.
- 2 = `NamedPipe` -  Uses strictly machine-only Named Pipes to create local IPC connections for debugging networking without the overhead of an actual network. This layer doesn't allow connections outside of the current host machine, but is reliable for its OS-based message system, ensuring data isn't lost during standard network communication.
- Default: `TCP` (0)
### Port
- The port to use when connecting to a server using the **TCP** NetworkLayer.
- Default: `30300`
### UseMultiplayerSaving
- This is a temporary option that is still finding its place, but at the current moment it dictates if the game will attempt to save and load your save data from the local files instead of the hosts. This is still being implemented, so it may not work if you are reading this and it hasn't been removed yet. This takes priority over **ForceHostSave** in the server config.
- Default: `true` (Even unimplemented, this will fallback to the `false` behavior if needed)

## Server Config Guide
### LogPlayerDisconnections
- `true` to log when a client disconnects in console.
- Default: `true`
### ForceHostSave
- `true` to force players to use the host's save data every time a game is loaded.
- Default: `false`
### BossTargeting
- 0 = `Nearest` - Closest player to the boss will be targeted automatically.
- 1 = `Farthest` - Farthest player to the boss will be targeted automatically.
- **[NOT IMPLEMENTED]** 2 = `LowestHealth` - The player with the lowest health will be targeted automatically.
- **[NOT IMPLEMENTED]** 3 = `HighestHealth` - The player with the highest health will be targeted automatically.
-  **[EXPERIMENTAL]** 4 = `Random` - This method is here solely for comedic purposes. This will reassign the bosses target to a random player every tick. By default, this is 20 times a second.
- Default: `Nearest` (0)
### RespawnMethod
- 0 = `Individual` - Players will respawn at their own last used bench, 
- **[NOT IMPLEMENTED]** 1 = `Shared` - When a player sits at a bench, it is assigned as the global bench. All players respawn at the global bench.
- **[INCOMPLETE]** 2 = `PartyDeath` - Players who die will turn into ghosts until everyone dies. Once all players have died, all players will respawn according to `Individual`. If a player rests at a bench before this happens, the global bench will be reassigned and all players will respawn at the new location.
- **[INCOMPLETE]** 3 = `SharedPartyDeath` - PartyDeath, but using the `Shared` behavior as opposed to `Individual`.
- Default: `Individual` (0) 
### LoadGamePermission
- 0 = `Any` - Any client will be permitted.
- 1 = `Server` - Only the server will be permitted.
- 2 = `Client` - Only clients will be permitted.
- Default: `Any` (0)

### DistributedCocoonSilk
- `true` if a cocoon should provide silk for all players in the same area. **THIS OPTION IS CURRENTLY UNIMPLEMENTED**
- Default: `false` (Unused)

## Rollout Plan
Currently, SilkBound is planned to release in 3 major steps.

### 1. Open Alpha [CURRENT]
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
~~This section will cover a description of the frameworks SilkBound utilizes and provides.~~

**As a part of my high school capstone, this will be reserved until I finish my presentation in January. This will be the information I store in this section.**
