✅ - Done
Ⓖ - Gemaddiev
Ⓔ - Efimov
## Core - Camera & Controls

| Status | Task         | Subtask                                                                                                                                            |
| ------ | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------- |
|        | Scenes       |                                                                                                                                                    |
|    ✅   |              | Create a greybox island with probuilder                                                                                                            |
|    ✅   |              | All scenes must run simultaneously, but frustum cull visual and some logic                                                                         |
|        | Camera types |                                                                                                                                                    |
|   ✅    |              | Map scene - set up camera position for bird's eye view, slightly tilted isometric                                                                  |
|   ✅    |              | Battleground scene - orthogonal 2.5d                                                                                                               |
|   ✅    |              | City scene - isometric set up                                                                                                                      |
|        | Controls     |                                                                                                                                                    |
|   ✅    |              | Detect a click on ground or unit                                                                                                                   |
|   ✅    |              | While mouse button is held, calculate the latest speed and direction of movement                                                                   |
|   ✅    |              | If ground - move camera, if object - move object                                                                                                   |
|   ✅    |              | On release, call event to pass the values to object\camera movement                                                                                |
|   ✅    |              | Same for RMB (in case of RMB calculations of speed and direction, but no movement, just call event on release)                                     |
|   ✅    |              | Zoom In/Out transition event                                                                                                                       |
|        | Physics      |                                                                                                                                                    |
|   ✅    |              | Objects on the ground are still, if not affected by other forces (or moving themselves)                                                            |
|   ✅    |              | Objects in the air fall down due to gravity, they have settable weight                                                                             |
|   ✅     |              | Objects in the air collide with the screen borders. Caution - since camera can move, so will the borders                                           |
|        |              | Airborne objects collide in 2D world                                                                                                               |
|    ✅    |              | Objects on the ground collide in 3D world                                                                                                          |
|        |              | Airborne objects falling to the ground can collide with objects on the ground with the 3D logic                                                    |
|   ✅     |              | Object that has collided with another object, will bounce, depending on the force and the bounciness parameter of object and it's collision object |
|        |              | Flick trajectory prediction system for debugging                                                                                                   |
|   ✅     |              | Screen borders disappear on scene change event                                                                                                     |
## Scenes

### Battleground Scene

| Status | Task          | Subtask                                                                                                                                          |
| ------ | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
|        | Base unit AI  |                                                                                                                                                  |
|   ✅    |               | Has a faction                                                                                                                                    |
|   ✅    |               | Can walk (walks to Target till desired range)                                                                                                    |
|        |               | Has performable Action and cooldown, for now only one type of Action - attack                                                                    |
|   ✅     |               | Has Target (if no target, receives default Target based on faction)                                                                              |
|   ✅   |               | Looks for nearby targets                                                                                                                         |
|        |               | Has Health                                                                                                                                       |
|   ✅   |               | If grabbed, all other logic is stopped                                                                                                           |
|        | Health system |                                                                                                                                                  |
|        |               | Has max health                                                                                                                                   |
|        |               | Health can be restored                                                                                                                           |
|        | Damage system |                                                                                                                                                  |
|        |               | Damage is dealt on events                                                                                                                        |
|        |               | Damage event: collision                                                                                                                          |
|        |               | Damage events: unit attack                                                                                                                       |
|        | Enemy unit AI |                                                                                                                                                  |
|    ✅    |               | Default Target is set to Beacon                                                                                                                  |
|    ✅    |               | Calls death event on unit death                                                                                                                  |
|        | Ally unit AI  |                                                                                                                                                  |
|        |               | Stays in front of wall                                                                                                                           |
|        |               | Target can be a spot in front of the wall                                                                                                        |
|        |               | If Target is a spot, changes target after 5s of reaching previous target                                                                         |
|        |               | If health drops to zero, runs to barrack                                                                                                         |
|        | Castle        |                                                                                                                                                  |
|    ✅    |               | Wall segments with health                                                                                                                        |
|        |               | Walls have slots for ally units                                                                                                                  |
|        |               | Wall at zero health is sending event, units on it fall down, it stops being counted as Target for Enemies, visuals are replaced with broken wall |
|        |               | Barracks heal ally units that are close                                                                                                          |
|        |               | Barracks have limited space                                                                                                                      |
|        |               | Barraks have health, but placed further from Beacon                                                                                              |
|        | Beacon        |                                                                                                                                                  |
|        |               | Can be interracted with to start wave                                                                                                            |
|        |               | Starts daylight event                                                                                                                            |
|     ✅   |               | Has health                                                                                                                                       |
|        |               | On zero health stops daylight event                                                                                                              |
|        | Daylight      |                                                                                                                                                  |
|   ✅   |               | Starts daylight cycle                                                                                                                            |
|        |               | If event is finished earlier, cycle is forcefully completed                                                                                      |
|        |               | Daylight starts enemy waves                                                                                                                      |
|        | Resources     |                                                                                                                                                  |
|        |               | Enemy death event spawns pearls                                                                                                                  |
|        |               | Pearls can be picked up by cursor                                                                                                                |
|        |               | Resource manager counts pearls                                                                                                                   |
|        |               | Resource UI                                                                                                                                      |
|        |               | Resource can be used                                                                                                                             |
|        |               | Temporary feature - heal wall with pearls                                                                                                        |




