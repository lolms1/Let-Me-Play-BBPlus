# Let Me Play Baldi's Basics Plus

A BepInEx mod for **Baldi's Basics Plus** that makes silhouettes of characters slide across the screen mid-game — then spawns whoever just appeared in front of you.

---

## What it does

While you're playing, the mod periodically triggers an animation cycle: dark silhouettes sweep across the screen from right to left. After the sequence ends, the **main silhouette** stops in the center — and the character it represents spawns near the player.

The **Type 2 animation cycle** goes further: the standard animation plays, then silhouettes replay at different speeds (fast then slow, but in fact it depends on which animation sequence has audio in AnimEditor), fog fades in and out between passes, and then the character appears. 
Basically (games), it's more like creating a TikTok edit, but it happens right inside the game itself.

### Gameplay effects

- **Time scale manipulation** - the game world and NPCs slow down or speed up during the cycle via `ReplayManager`. The player's movement speed is adjusted to match.
- **Replay effect** — all entity positions (NPCs and players) are saved at the start of the cycle. Between replay passes, entities are teleported back to those positions, creating a rewind illusion.
- **Fog flashes** — fog fades in and out between replay passes for visual effect.
- **Shader glitch** — wall geometry glitches on beat, with configurable intensity, interval and decay speed. Like in null bossfight 
- **Light control** — lights can be disabled and restored as part of a sequence.
- **Character spawning** — the main silhouette determines which character gets spawned. If the character already exists in the level, they're teleported to the player instead.

## Install

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) and [MTM101BaldAPI](https://gamebanana.com/mods/383711).
2. Copy `BepInEx` and `BALDI_Data` folders into game files.
---

## Configuration

### `Config.json`

Controls the core timing and movement parameters of the silhouette system. The config also contains debugging settings if you want to select your own music, you can turn off `RandomAudioSelecting` and picking your audio by `AudioIndex` (Only for CycleType2 audio!!)

---

### `AnimEditor.json`

Defines a custom animation sequence for each audio track. When a Type 2 cycle plays, the mod picks an audio track and runs the sequence assigned to it — so every track can have a completely different feel.

**Structure:**

```json
{
  "sequences": {
    "animAudioType2_0": {
      "parameters": {
        "phase1Duration": null,
        "spawnInterval": null,
        "silhouetteSpeed": null,
        "mainStopTime": null,
        "pauseAtEdgeTime": null
      },
      "steps": [
        {
          "type": "Phase1"
        },
        {
          "type": "FogFlash",
          "speedMultiplier": 2.0,
          "enabled": true
        },
        {
          "type": "Replay",
          "speedMultiplier": 2.0
        },
        {
          "type": "FogFlash",
          "speedMultiplier": 0.7,
          "enabled": true
        },
        {
          "type": "Replay",
          "speedMultiplier": 0.7
        },
        {
          "type": "Phase2"
        },
        {
          "type": "SpawnCharacter"
        }
      ]
    }
  }
}
```
**Parameters** — override the `Config.json` values for the duration of this sequence only. Set a field to `null` to use the base value from `Config.json`.
 
**Steps** — a sequence is built from an ordered list of step types. Each type only accepts the fields that are relevant to it — extra fields are ignored. There are over 16 step types in total; a full reference with all fields and defaults is located inside the mod folder at:
 
```
BALDI_Data/StreamingAssets/Modded/lolms.bbplusmod.letmeplaybbplus/
```
 
**Rules enforced at load time:**
- `Replay` cannot appear before `Phase1` — replaced with `Phase1` automatically.
- `SpawnCharacter` cannot appear before `Phase2` — replaced with `Phase2` automatically.
 
New audio files added to `Audio/AnimationCycleType2/` are detected automatically on the next launch and get a default sequence entry in `AnimEditor.json`.

---

## Credits

- [MTM101BaldAPI](https://github.com/benjaminpants/MTM101BMDE) by benjaminpants
- [Baldi_ShootState](https://github.com/lolms1/Baldi-shoot-mod) by me
