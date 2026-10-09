# Stories OSC Unity Tool v0.5.10 TB18.1

## Light Bearer Magick correction

- Replace obsolete Yggdrasil Light Magick school with Light Magick.
- Add Light Lance, Radiant Fracture, Light Imbuement, Dawnfall, Sacred Flame, Radiant Horizon, Light Atomic (IDs 145-151).
- Retire older Chakra Heal and Aura Shielding IDs 98/99 without reassigning.
- No OSC protocol changes.

## Protocol 21 — Physical Helpful Items + Automatic PvP Identity

TB18 starts the Protocol 21 test line.

### Physical helpful items

- Per-item prop picker inside Item Automation.
- Left/right gesture hand.
- Grab/Toggle Gesture and separate Use Gesture.
- Self / Others targeting controls.
- Optional Success and Failure/Empty presentation clips.
- Generated gesture latch toggles the prop rather than requiring a held gesture.
- Generated local Self/Other Head detectors.
- Generated outgoing helpful-item ID bus and target-side Head receiver bus.
- FaceEmo assets are not edited; TB18 reads the built-in Gesture parameters only.
- Selector intent equips the item; Sam.py inventory is consumed only after an authoritative successful physical use.

### Automatic Player-to-Player attribution

- Existing Stories Attack volumes receive local-only remote-humanoid touch detectors.
- Weak/Average/Strong/Critical attempts are reported separately.
- VRChat built-in body tags provide physical proof that the local attack touched another humanoid.
- Target identity is never inferred from a Contact tag.
- Desktop/Sam.py Protocol 21 pairs authenticated attacker attempts with authenticated target hit receipts.
- Ambiguous simultaneous matches fail closed.

### Compatibility

- Unity Protocol: 21
- Marker beacon: 121 / 122
- Minimum Desktop: 0.8.22-prebuild.1
- Required Sam.py OSC API for new interactions: 0.8.19
- Existing TB17 Animator-integrity and marker-metadata protections remain.
