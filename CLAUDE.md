# CLAUDE.md

This is a game project by BarkingBird studio. For more information , load the [Project.md](Project.md) file.

## Folder Structure
```
Bust and Build/
├── Claud.md                                 ← You are here (always loaded)
├── `Claud/`                                 ← Folder for claude related files
│   ├── `learnings/`                         ← Your knowledge database for internal use
│   ├── Project.md                           ← Task router
│   └── Input.md                             ← User inputed prompt
│
├── `Assets/`                                ← Assets for Unity
│   ├── `!_Game/`                            ← Assets made in BarkingBird studio
│   │   ├── `!_Scripts/`                     ← Scripts folder
│   │   │   ├── `Components/`                ← ECS component structs + Authoring MonoBehaviours
│   │   │   ├── `Systems/`                   ← All ECS systems (Burst-compiled)
│   │   │   ├── `Core/`                      ← Core systems of the game
│   │   │   │   ├── DI/                      ← Reflex installers + extension helpers
│   │   │   │   ├── GameLoop/                ← GameLoopManager, GameManager, listener interfaces
│   │   │   │   ├── Events/                  ← EventManager (static event hub)
│   │   │   │   └── Utilities/               ← MathHelper, PhysicsUtility, AssetService, LoadingService, Log
│   │   │   ├── `MonoWorld/`                 ← Gameplay systems that are not requeired to be ECS
│   │   │   ├── `Scenes/`                    ← Scene flow and scene data scripts
│   │   │   ├── `Settings/`                  ← Various JSON configs and blob assets
│   │   │   └── `Editor/`                    ← Editor scripts
│   │   ├── `Resources/`                    
│   │   │   └── Config.json                  ← Immutable (after game starts) configs
│   │   └── Tasks.md                         ← Task management memos
```