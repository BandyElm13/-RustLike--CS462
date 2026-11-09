# Unity Procedural Dungeon Starter

This starter kit creates a connected, branching dungeon from reusable room prefabs.
It uses ScriptableObjects for room definitions and configuration, so adding room
prefabs does not require changing the generator.

## Included

- `DungeonGenerator`: creates a random grid layout and instantiates room prefabs.
- `DungeonRoom`: room component, connection lookup, optional spawn points, and an
  overridable initialization method.
- `RoomConnection`: marks a doorway and its cardinal direction.
- `RoomDefinition`: ScriptableObject describing a prefab, type, weight, and depth range.
- `RoomDatabase`: collection of room definitions.
- `DungeonSettings`: room count, grid spacing, and seed configuration.

## Important first-version constraints

1. All rooms must use the same grid footprint.
2. Rooms are not rotated by the generator.
3. The initial layout is a connected branching tree; it does not create loops.
4. Room prefabs must provide a connection for every neighboring occupied grid cell.
5. Extra prefab connections are considered unused exits. Assign a closed-door/blocker
   prefab to the generator if those exits should be physically blocked.
6. This package implements generation, not the overworld/dungeon scene transition.
   The generator exposes `PlayerSpawnPoint` so a scene controller can position the
   player after generation.

## Unity setup

### 1. Import scripts

Copy the `Scripts` folder into `Assets/Scripts/Dungeon/`.

### 2. Create room prefabs

For each room prefab:

- Make the room root the center/origin of the room.
- Keep the room's outside dimensions consistent.
- Add `DungeonRoom` to the root.
- Add child GameObjects at doorway centers.
- Add `RoomConnection` to each doorway child.
- Set each doorway's direction in the Inspector.
- Add a child Transform for the player spawn point to the `DungeonRoom` component.
- Optionally assign enemy and treasure spawn point arrays.

Use these direction conventions:
- North = positive local Z
- East = positive local X
- South = negative local Z
- West = negative local X

The room's doorway opening should be centered on the room edge. For example, if a
room is 20 units wide and 20 units deep, doorway connection points are typically
at local X = +/-10 or local Z = +/-10.

### 3. Create ScriptableObject assets

In the Project window, right-click and create:

- Create > Dungeon > Room Definition (one asset per prefab variant)
- Create > Dungeon > Room Database
- Create > Dungeon > Dungeon Settings

Add your room definitions to the Room Database. Set each definition's type,
prefab, weight, and depth range.

The first generator version needs compatible doorway variants. For example, a
room placed at the end of a branch may only need one connection, while a room
in the middle of a corridor needs two. A prefab with all four connections can
serve many positions, with unused exits blocked by the optional blocker prefab.

For a reliable first test, make:
- A Start room with all four cardinal connections. The layout grows outward from
  the start in random directions, so this avoids restricting the first branch.
- A Boss room with at least one connection (the farthest room is a leaf in this
  first-version tree layout).
- At least one ordinary room prefab with all four connections. It can be reused
  in corridor, branch, and junction positions; unused exits are blocked.

You can later create more specialized doorway variants (one-exit dead ends,
two-exit corridors, three-exit junctions, etc.). The generator checks that a
candidate prefab has every connection required by its grid neighbors.

### 4. Configure settings

Set:
- Room Count: desired number of rooms.
- Room Spacing X/Y: exact distance between room prefab origins.
- Use Random Seed: enable for a new dungeon each run.
- Seed: disable Use Random Seed to reproduce a layout.

### 5. Add the generator

In the Dungeon scene:

- Create an empty GameObject named `DungeonGenerator`.
- Add the `DungeonGenerator` component.
- Assign the Dungeon Settings and Room Database assets.
- Optionally assign a closed-door/blocker prefab.
- Set `Generated Rooms Parent` if you want generated rooms grouped under another object.
- Leave Generate On Start enabled for testing.

Press Play. The generator creates the dungeon when the scene starts.

## Adding a new room type

1. Create the room prefab.
2. Add `DungeonRoom` and its `RoomConnection` children.
3. Create a Room Definition asset.
4. Choose its room type, prefab, weight, and depth limits.
5. Add the definition to the Room Database.

No changes to `DungeonGenerator` are needed for a new ordinary room prefab.

## Extending room behavior

For room-specific behavior, subclass `DungeonRoom` and override `InitializeRoom`:

```csharp
using UnityEngine;

namespace ProceduralDungeons
{
    public class CombatDungeonRoom : DungeonRoom
    {
        public override void InitializeRoom(int depth, int dungeonSeed)
        {
            // Spawn enemies based on depth, or initialize encounter logic.
        }
    }
}
```

Put the derived component on the room prefab root instead of the base component.
It inherits the connection and spawn-point functionality.

## Notes

- The start room must have a Room Definition of type `Start`.
- If there is more than one generated room, the farthest room is assigned type `Boss`.
  A compatible Boss definition is therefore required.
- The generator chooses ordinary rooms from compatible non-Start/non-Boss definitions,
  preferring `Combat`.
- If generation reports that no compatible room definition exists, check the
  connection directions on the prefabs and ensure the database has matching variants.
- The current implementation uses Unity's standard 3D X/Z plane. If your dungeon
  should use a different orientation or your spherical-world gravity system, the
  placement and player-spawn integration will need adapting.
