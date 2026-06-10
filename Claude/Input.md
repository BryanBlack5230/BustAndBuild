problem 1. is solved by IsInvulnerable - it is going to be added to all entities that should not die, but raise some kind of event when they do

problem 2 and 3 are work in progress, they will be eventually

put a pin to problem 4, i want to discuss it 

problem 5 - yes to snapshots of Target, before launching TargetScorerJob

6. fixed myself
7. pause is an open question, since i want to implement a "soft" pause, where it's not stopping time completely, but slowing it down to very small number
8. yes to PearlsSpawnUtility
9. let's add this one together to problem 4
10. will think about that one, after we do more unit types
11. the potential lost of pearls is neglactable. how would you change WorldCurrency into persistant data? Keep in mind, that there are plans to have several resource currencies
12. clamp to max health

Low risks:
- Cache EntityQuery in InteractController
- change GameManager.StartGame to UniTaskVoid with Forget
- update docs with new position of Runtime.asmdef
- add .idea/ to .gitignore
- dispose of BlobContainer's persistent blob