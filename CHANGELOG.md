# v0.3.1
- Support [Fontifier](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier) v1.1.0
- Fix windows issues

# v0.3.0
- Switch to [Fontifier](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier)

# v0.2.0
- Make it work for remote players.
	- Currently, the text does not scale based on distance.

# v0.1.3
- No longer uses RMAPI.
	- Gets everthing via HarmonyPatch.
- Attempt to make health numbers show for remote players.
	- Dosen't seem to work yet.

# v0.1.2
- Fix issues when the user is client.
	- It now grabs the UI Bar from the local player instead of any player.
- Various technical changes.
	- Now passes the current health instead of getting it from the player.
	- No longer stores the UI Bar, instead opting to store the text.

# v0.1.1
- Change GameObject.Find according to the new rumble version.
- Get rid of asset bundles in favor of a font file.

# v0.1.0
- Initial release
