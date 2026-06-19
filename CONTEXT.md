# ModifiedItemDrop

ModifiedItemDrop controls what player-carried items are lost, kept, restored, or claimed after an Unturned player death. This language keeps player-asset outcomes precise across death, respawn, disconnect, and server restart scenarios.

## Language

**Player Asset**:
An in-game item, clothing item, or clothing-contained item that belongs to the player outcome being managed by the plugin.
_Avoid_: item when discussing ownership outcomes, inventory stuff

**Player Asset Conservation**:
Every Player Asset entering death processing must end in exactly one allowed outcome: Drop, Keep/Pending Restore, Claim, Claim Recovery, or configured deletion.
_Avoid_: no item loss, item safety

**Player Asset Outcome**:
The final allowed state assigned to a Player Asset after death processing, such as Drop, Keep, Claim, Claim Recovery, or configured deletion.
_Avoid_: result, action when discussing asset conservation

**Death Session**:
The lifecycle that begins when a player death is processed and ends when all kept Player Assets are restored, durably claimed, dropped, or deleted.
_Avoid_: death event when referring to the multi-step lifecycle

**Drop**:
A player asset that becomes a world item at the relevant player/death position.
_Avoid_: lose, delete

**Keep**:
A player asset selected not to drop during death processing and therefore scheduled for restoration or claim.
_Avoid_: preserve, save, retain

**Pending Restore**:
A temporary set of kept player assets waiting to be returned after the death event flow completes.
_Avoid_: restore queue, temp items

**Claim**:
A persisted package of kept player assets that could not be immediately restored or must survive disconnect/restart.
_Avoid_: pending item, saved item, compensation

**Durable Claim**:
A Claim whose persisted representation has been successfully written and can be recovered after a server restart.
_Avoid_: saved claim when durability is uncertain, memory claim

**Claim Recovery**:
The act of returning player assets from a Claim to a player.
_Avoid_: claim, restore when referring specifically to persisted assets

**Respawn Grant**:
A configured Player Asset granted after a tracked death session reaches respawn.
_Avoid_: starter item, kit item, respawn item when discussing the grant event

**Clothing Content**:
A player asset stored inside a clothing container such as a backpack, vest, shirt, or pants.
_Avoid_: nested item, container item


**Inventory Capability**:
A configured change to a player's inventory capacity or layout, not a Player Asset Outcome.
_Avoid_: drop rule, outcome rule
