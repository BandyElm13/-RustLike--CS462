Instructions for using the WaveManager System:

1) Create an empty game object in the scene
2) Attach the script to the object
3) In the component settings in the inspector, there is a list for spawns. Create how every many number of spawns you would like by pressing the plus '+' icon. 
4) Creating Spawn points:
  - Create child objects under the planet object that just have a transform component (should be the default when creating an empty object) 
    and adjust the transform to roughly where you want the enemies to have a possible spawn at.
  - Drag and drop the transform objects (spawn points that are child objects in the planet hierarchy) into the list of the WaveManager (Spawn points list)
5) Drag enemy prefab into the enemy prefab slot in the WaveManager component
IMPORTANT
- An enemy prefab is needed to use this script right now. I can probably get this system to work once we have our different enemy types after Demo 1

Enemy Prefab
- Uses my version of enemy scripts (...AI / ...Stats)
