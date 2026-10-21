# Stories OSC Unity Tool v0.5.10 TB18.2 — Contact Provenance Isolation

Protocol 21 remains unchanged. TB18.2 isolates canonical Stories damage tiers from third-party sender compatibility aliases.

- `SoY_HitWeak`, `SoY_HitAverage`, `SoY_HitStrong`, and `SoY_HitCritical` now listen only to their matching canonical Stories attack tag.
- External `Hands` uses local unsynced `SoY_ExternalHitWeak`; `Sword`/`Weapon` use local unsynced `SoY_ExternalHitAverage`. A Strong+Sword contact no longer creates a false canonical Average PvP receipt.
- Safe Managed Repair separates existing mixed TB18.1 Weak/Average receivers in place and preserves hitbox geometry and transforms. Foreign receiver objects remain untouched.
- Canonical tags take precedence in tag audits. The tool retains its existing Protocol 21 handshake, 121/122 beacon, raycasts, helpful items, spell registry and menu generator.
- **Requires Desktop OSC v0.8.22-prebuild.7+ to observe external-only hits.** The current v0.8.22-prebuild.6 Desktop does not recognize the new external parameters. Upgrade both components before deploying TB18.2 to active avatars.
- This change **does not resolve** Sam.py's true multi-attacker ambiguity, scroll unlocks, MP regeneration, raycast condition damage, Write Defaults settings, or Gesture Manager-like menu editing.

## Safe adoption

1. Back up your Unity project/FX controller and duplicate your avatar before authoring.
2. Import TB18.2, select the duplicated avatar, run Migrate / Validate and Safe Repair All.
3. Under `Stories Incoming Damage Contacts`, verify canonical and external receiver tags/parameters are separate.
4. With Desktop prebuild.7+, perform a one-hit Weak then Strong attack in both directions; verify no false Average canonical receipts.
5. Confirm external-only Sword/Hands contacts are separately categorized and ambiguous cases remain held.

Static GitHub verification does not constitute a Unity compile or an in-world VRChat test.
