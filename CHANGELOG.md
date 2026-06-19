## v0.4.2
- Update to [RMAPI v5.3.0](https://thunderstore.io/c/rumble/p/UlvakSkillz/RumbleModdingAPI/v/5.3.0)
- Update to [MelonLoader](https://github.com/lavagang/melonloader) v0.7.2
- Partial [ReplayMod](https://thunderstore.io/c/rumble/p/ERRORMODS/ReplayMod) support
	- Health text updates correctly with replays
	- Known issues:
		- Health text can clip into nameplate

## v0.4.1
- Update to [RMAPI v5.1.1](https://thunderstore.io/c/rumble/p/UlvakSkillz/RumbleModdingAPI/v/5.1.1)

## v0.4.0
- Update to [Fontifier v1.1.2](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier/v/1.1.2)
- Uses [RMAPI](https://thunderstore.io/c/rumble/p/UlvakSkillz/RumbleModdingAPI) again
- Opponent health text scales with distance to camera
- Make opponent health text fade in and out

## v0.3.1
- Support [Fontifier v1.1.0](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier/v/1.1.0)
- Fix windows issues

## v0.3.0
- Switch to [Fontifier](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier)

## v0.2.0
- Make it work for remote players
	- Currently, the text does not scale based on distance

## v0.1.3
- No longer uses [RMAPI](https://thunderstore.io/c/rumble/p/UlvakSkillz/RumbleModdingAPI)
	- Gets everything via HarmonyPatches
- Attempt to make health numbers show for remote players
	- Doesn't seem to work yet

## v0.1.2
- Fix issues when the user is client
	- It now grabs the UI Bar from the local player instead of any player
- Various technical changes
	- Now passes the current health instead of getting it from the player
	- No longer stores the UI Bar, instead opting to store the text

## v0.1.1
- Change `GameObject.Find` according to the new rumble version
- Get rid of asset bundles in favor of a font file

## v0.1.0
- Initial release
