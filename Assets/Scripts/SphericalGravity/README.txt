code written with help of claude

Instructions for set up:

Player object setup:

Create the Player, add a CapsuleCollider, then GravityBody, then FirstPersonController (the Rigidbody gets added automatically).
On GravityBody, drag your sphere into Attractor (or leave it empty — it'll auto-grab the first attractor in the scene).
Add a Camera as a child at head height. The controller finds it automatically.
Optional: add an empty child under the camera and drop it into Weapon Socket for later.

Rigidbody settings that matter — and this ties directly to all the tunneling we fought earlier: set Collision Detection → Continuous and Linear Drag → 0. The controller sets its own velocity each physics step, so drag would just fight it, and Continuous keeps you from punching through the sphere surface at high fall speed.

The gun hook (what "modular" means here): the controller deliberately doesn't know anything about weapons, but it exposes everything a future gun script will need — Camera, CameraTransform, WeaponSocket, IsSprinting, IsMoving, and an AimRay (a ray straight out of the camera for raycasting shots). So your later gun component just parents itself to the socket, raycasts along AimRay to hit things, and can read IsSprinting to lower the weapon / block firing while running — no edits to this controller needed.

Use both input systems in project manager