# Spell Audit — Unity Tool v0.5.2

## Added

The v0.5.2 registry contains **144 stable spell IDs** across **12 schools**:

- White Magick
- Black Magick
- Green Magick
- Time Magick
- Arcane Magick
- Synergist Magick
- Illusion Magick
- Dream Magick
- Nature Magick
- Chaos Magick
- Abyssal Curses
- Yggdrasil Light Magick

Existing v0.5.1 IDs remain unchanged:

- White Magick: `1–25`
- Black Magick: `101–127`

Spells shared by several schools reuse one ID. For example, Sleep remains ID `122` in Black, Green, and Dream Magick.

## Source handling

Named spells were taken from the current class-license data and the current Sam.py fight-system spell support. Both `Darka` and `Darkra` remain separate entries because the current boards contain both spell names.

## Not assigned an ID yet

These current license entries describe capabilities but do not provide canonical spell names:

- `Holy Magicks I`
- `Holy Magicks II`
- `Holy Magicks III`
- `Nature Magick 1` basic rites

They were intentionally not converted into invented spell names. Once canonical names are added to `licenses.json`, they can receive stable IDs without renumbering the existing registry.
