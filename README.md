# Unity DOTS MOBA Tutorial

A multiplayer MOBA-style game built with Unity's Data-Oriented Technology Stack (DOTS) and Entity Component System (ECS) architecture.

## Attribution

This project is based on the tutorial series by **[TurboMakesGames](https://www.youtube.com/@TurboMakesGames)**. Most of the materials, assets, and foundational code were created as part of their excellent tutorial content. Please check out their channel for the full tutorial series and support their work!

## Overview

This project demonstrates a client-server multiplayer game using Unity's modern DOTS architecture:

- **Data-Oriented Design** with ECS for high-performance gameplay
- **Client-Server Networking** using Unity Netcode for Entities
- **Prediction and Rollback** for responsive networked gameplay
- **Team-Based MOBA Mechanics** with champions, abilities, and combat

## Tech Stack

- **Unity Version**: 6000.2.10f1
- **Rendering**: Universal Render Pipeline (URP) 17.2.0
- **Networking**: Unity Netcode for Entities 1.9.3
- **Architecture**: DOTS/ECS with Entities 1.x

## Pending Feature Updates
We will be making customizations to this project in an effort to learn
more about NFE and ECS:
- **Auto Attack**: Right click will set automatic ranged attack
- **Minion Target Lock**: Minions will no longer attack the closest target
- **Minion Chasing**: While the target is in range the minions will follow
- **Terrain**: Add walls and paths between lanes
- **Tower Target Lock**: Towers will fire at the first target till they leave range
- **Homing projectiles**: Tower and Minion projectiles will follow the target they were fired at

## Features

- Client-server multiplayer architecture
- Character movement with input prediction
- Ability system (AOE and skill-shot abilities)
- Combat system with damage detection and health management
- Network-synchronized gameplay with ghost entities

## Project Structure

```
Assets/
├── _Scripts/
│   ├── Client/          # Client-only systems (input, camera, UI)
│   ├── Server/          # Server authoritative logic
│   └── Common/          # Shared systems and components
├── Scenes/
│   └── ConnectionScene.unity   # Entry point
├── Prefabs/             # Champion, abilities, minions, structures
└── Settings/            # Input actions and configuration
```

## Getting Started

### Prerequisites

- Unity 6000.2.10f1 or compatible version
- Basic understanding of Unity DOTS/ECS

### Running the Project

1. Open the project in Unity
2. Load `Assets/Scenes/ConnectionScene.unity`
3. Enter Play Mode
4. Select your role (Host/Server/Client) and team
5. Click "Enter Game" to spawn your champion

### Controls

- **Right Mouse Button**: Set movement target
- **Q**: Use AOE ability
- **W**: Prepare skill-shot ability
- **E**: Dash ability - does not hit structures
- **Left Mouse Button**: Confirm skill-shot direction

## Key Systems

- **Movement**: Click-to-move with distance clamping
- **Abilities**: AOE sphere and directional skill-shot
- **Combat**: Trigger-based damage detection with health system
- **Networking**: Server-authoritative with client prediction

## Learning Resources

For a complete walkthrough of this project's development, visit the [TurboMakesGames YouTube channel](https://www.youtube.com/@TurboMakesGames).

## License

Please refer to the original tutorial creator's licensing terms for asset usage and distribution.
