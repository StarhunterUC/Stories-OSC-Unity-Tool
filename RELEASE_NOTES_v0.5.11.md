# Stories OSC Unity Tool v0.5.11 â€” Stable

Protocol 21 is unchanged. Desktop/Sam.py compatibility marker and contact transport remain P21.

## Changes
- Helpful physical items: contact-activated self/other use and head receiver bus.
- Editable Helpful Item head receiver transform and contact radius; rebuild preservation.
- Optional Parent Constraint mode for newly created receivers; no automatic migration.
- Per-tool Write Defaults ON/OFF preference and explicit apply to Stories-managed states.
- Helpful Item read-only diagnostics and editor-only simulation.
- Existing TB18.2 contact provenance isolation, action gates, Movement Lock and resource FX retained.

## Limitations
- Full multiplayer target attribution, item debit, and server acknowledgement require live end-to-end tests.
- Editor simulation is not an authoritative OSC or inventory test.
- Existing v0.5.10 tag is retained for rollback.