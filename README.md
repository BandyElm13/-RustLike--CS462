# -RustLike--CS462
CS462 Game Repository
Unity Version: Unity 6.6(6000.6.0f1)

RUSTLIKE DESIGN DOCUMENT
A game by BREWSLABS (Caleb Gillis, Tyler Harvey, Scott Hendren, Gideon Rank, and Leif Rush)

Questions we’d ask our sister team:
What do you think of the speed of gameplay and the general loop?
Do you have any advice on how we can make the game more fun? (anything to add or remove)
Can you think of any flaws in gaining win conditions that would break the game?
Introduction
Game Summary
RUSTLIKE is a FPS roguelike, featuring a PvE wave-based combat loop. Players start on the same starting planet every run, and fight progressively harder waves of monsters (characterized by the planet they’re currently on). As players progress, eventually the only option is to escape the current planet and go to the next to continue the run. Along the way, players can also find research components to upgrade weapons in current and future runs. Eventually the player will have visited all planets which is a general win condition, but players can choose to keep playing to reach new high scores.
Inspiration
The main inspirations we discussed as a team would be games like Megabonk, another 3D indie roguelike. The pace of this game intensifies as you live longer, which is what we’d like to achieve too. In addition, for movement, we’re interested in trying to achieve a quake-like movement system.
	For general aesthetic, we want the game to feel light-hearted and comedic, with inspirations like Futurama and Loons: The Fight for Fame, but still with a dystopian feel, maybe similar to Borderlands 2. 
Player Experience
We want players to have a fun action-packed, fast-paced experience. We’ll focus on movement and attack mechanics so the game feels satisfying to play, and we’ll focus on balancing weapons, abilities, and enemies to ensure an even progression and replayability. The UI and general interaction in the game will be simple, but the actions players take will be the defining factor in a given run.
Platform
	We’ll be developing RUSTLIKE for Windows and Mac, using Unity’s built-in project management tools. If we end up publishing the game, we think that mobile could also possibly be an option.
Development Software
	
We will be choosing Unity as our Game Engine for this project. Along with this, we’ll use VSCode and AI tools to help with software development for the project. We’ll be using GitHub for version control, and using ignores can avoid using LFS. In addition, we will use Kanban boards in GitHub to keep track of tasks. And Blender for asset creation.

Genre
	


Although RUSTLIKE is a combination of genres, the main is a wave-based roguelike. The general win condition is the longest time alive.



Target Audience
	Our primary audience is teens and young adults. If we had to rate this game, it’d probably be ‘T’. There will be mild gore and violence, but it will still be light-hearted and comedic as described earlier.



Concept
Gameplay overview
Explain general gameplay loop

Theme Interpretation
Styled similar to the game ‘Megabonk’, where the goal is to survive as long as possible by upgrading the player characters' weapons and abilities. Waves of enemies continuously grow and become more difficult so there will always be a need to increase the player's damage, health, and maneuverability.
Primary Mechanics
Roguelike: Each run starts from the same starting world, and death is permanent for a run. The only thing that persists through runs are the research components (used to upgrade weapons to make progression easier the more you play to get to where you left off)
Wave-based enemies: Players will face waves of different classes and difficulties of enemies, and each wave is more difficult than the last.
Procedural Generation: Parts of each planet will have procedural generation to keep exploring fresh
Fast-paced: Movement and general mechanics will be fast, (similar to Quake and Megabonk), and you can kill enemies, but also die, very quickly (depending on class and difficulty)
Endgame / Win condition: After players discover all the planets, the main win condition is just getting the highest score (longest time) in the run.
Secondary Mechanics
ENEMIES: See enemies section
WEAPONS:
Gun / Projectile Shooter (in upgradable order)
Planet 1: “Glock”, Plasma/EMP gun, Turret 
Planet 2: Laser Pistol, Laser Full Auto Rifle
Planet 3: Grenade Launcher, RPG
Melee
Constant melee ability (example: COD)
ABILITIES:
Jump Height / Bounce (1 time use w/ cooldown)
Movement Speed (for a certain duration w/ cooldown)
Increased Fire Rate (for a certain duration w/ cooldown)
Invoulnerability (Bubble for a certain duration)
Stimmy (short boost of health)
Other
Upgrades / Items / Pickups:
Weapons (guns/melee)
Health pots
Ability Swap (Changes current ability, see Ability section)
Ability Upgrade (Buffs current ability, EX: Movement Speed increased +5%)
Global Upgrades (Stay w/ player for the entire game)
Fire rate
Damage
Health
Armor
Movement Speed
Jump Height-Jetpack
Other
PLANETS: 
LEVEL 1: Robot 
LEVEL 2: Aliens
LEVEL 3: Rock
And so on…
RESEARCH COMPONENTS: Research components can be found around the map and persist between runs. Weapons can be purchased before each run and can also get upgraded with research components (possibly different kinds?) 
Level Design
Main Planets
Wave based enemies
Static worlds (No generation)
Central gravity (Player pulled to planet center)
Dungeons inside planets?
Separate Scenes to load and generate levels using ‘block based’ rooms to create a new experience inside each dungeon
Wave based or static spawned enemies


Art
Theme Interpretation
The game will have an art style similar to the TV show ‘Futurama’, low-poly, cartoonish to give the world a simple view from simple colors and shading.

Design
Simple, not overly complex upgrade trees or inventories, or menus
The UI will consist of a simple HUD displaying player health, weapon type, ability, etc. There will also be upgrade trees/inventories/menus styled in a similar way with not too many details, but displays enough to give the player the required information to understand it.
Audio
Music
Fun, immersive, thrill-seeking? music, cartoonish
The music style will be cartoony to immerse the player with thrill seeking, sometimes anxiety inducing, music.
Sound Effects
Sound effects will consist of obnoxious yet satisfying sound clips to make things like jumping, shooting, and killing enemies more enjoyable.
Game Experience
UI
Simple UI
No numbers, player should just need to glance at some part of of the screen to convey information faster. 
Red hue if player health < 25%

Controls
General WASD, SPACE to jump, E to use ability, F to Interact, and mouse to rotate camera
First person
Development Timeline	
Make a chart for general TEAM deadlines as per course due dates
We can add kanban board for specific task implementation in each “sprint”
Link kanban board here
Github


Roles
Team Lead: Scott
Programming: Caleb, Lief, Tyler, Scott
Modeling: Gideon, Leif, Tyler
Design (UI/Systems): Leif, Gideon
Art / Music: Caleb, Leif, Tyler
