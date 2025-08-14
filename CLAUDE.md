# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**WuXiaSLG_Prototype** is a Unity-based Wuxia (武俠) strategy/tactical RPG game prototype built with Unity 6000.0.32f1. The game features turn-based combat with action point mechanics, character movement visualization, and a queue-based combat system inspired by martial arts themes.

## Build and Development Commands

### Unity Editor Operations
- **Open Project**: Open Unity Hub and select this project (requires Unity 6000.0.32f1)
- **Play Mode**: Press Play button in Unity Editor or use Ctrl+P
- **Build**: File > Build Settings > Build (target platform: PC Standalone)

### Testing
Since this is a Unity project, testing is primarily done through:
- **Play Mode Testing**: Run the game in Unity Editor
- **Unity Test Framework**: Window > General > Test Runner
- **Console Debugging**: Window > General > Console for runtime errors

## Architecture Overview

### Core Systems

#### Combat System (`Assets/GameCore/Control/`)
- **CombatCore.cs**: Main combat manager singleton that handles turn order, combat queue, and action resolution
- **CombatEntity.cs**: Base entity for all combat participants with speed and action value mechanics
- **CharacterCore.cs**: Player character controller with input handling, movement, and action recording
- **CombatAction.cs**: Encapsulates individual combat actions for replay/execution
- **ClonedCombatEntity.cs**: Lightweight copies for turn order prediction simulation

#### Character System (`Assets/GameCore/Character/`)
- Character prefabs with animator controllers
- Health component for damage tracking
- Visual representations for control and execution states

#### Enemy System (`Assets/GameCore/Enemy/`)
- **EnemyCore.cs**: Enemy-specific behaviors and AI
- **DamageDealer/DamageReceiver**: Components for damage interaction
- **PreviewReceiver.cs**: Shows damage preview during player targeting

#### UI System (`Assets/GameCore/UI/`)
- **SLGCoreUI.cs**: Main UI controller singleton
- **TurnOrderUIController.cs**: Manages turn order display
- **APBar.cs**: Action point visualization
- **FloatingUIDamageNumber.cs**: Damage number popups
- **UIFollowWorldObject.cs**: UI elements that track 3D world positions

### Key Design Patterns

1. **Singleton Pattern**: Used for CombatCore and SLGCoreUI for global access
2. **State Machine**: CharacterCore uses states (ControlState, ExecutionState, UsingSkill, ExecutingSkill)
3. **Command Pattern**: CombatAction records and replays character actions
4. **Observer Pattern**: Implicit through Unity's component system and events

### Input System
- Uses Unity's new Input System package
- Input actions defined in `Assets/GameCore/InputAsset/PlayerControl.inputactions`
- Character movement and action confirmation handled through InputSystem events

### Rendering Pipeline
- Uses Universal Render Pipeline (URP) 17.0.3
- Render settings in `Assets/Settings/` with PC and Mobile configurations
- Post-processing volume profiles for visual effects

## Important Technical Details

### Turn Order Calculation
The game uses a speed-based action value system where:
- Each entity accumulates action points based on their speed
- When action value reaches threshold (500), entity can act
- Turn order prediction simulates future turns using cloned entities

### Character Movement
- Movement leaves a trail using LineRenderer
- Action points decrease based on distance moved
- Movement is recorded as CombatActions for replay

### Unity-Specific Considerations
- Scene file: `Assets/Scenes/SampleScene.unity`
- Uses TextMeshPro for UI text
- Character Controller component for physics-based movement
- Animator components for character animations

## Dependencies

Key Unity packages (from manifest.json):
- Unity Input System 1.11.2
- Universal RP 17.0.3
- TextMeshPro (included)
- Unity AI Navigation 2.0.8
- Character Controller 1.2.4

## Development Tips

1. **Hot Reload**: Unity supports hot reload for script changes while in Play Mode
2. **Prefab Workflow**: Modify prefabs in Prefab Mode to affect all instances
3. **Inspector Debugging**: Use public fields or [SerializeField] for runtime value inspection
4. **Version Control**: .meta files are crucial for Unity - always commit them with their associated assets
5. **Performance**: Use Unity Profiler (Window > Analysis > Profiler) for optimization
- 有編譯錯誤的話繼續做
- 都用繁體中文跟我溝通