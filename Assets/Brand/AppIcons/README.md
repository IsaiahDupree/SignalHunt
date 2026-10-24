# Island Challenge app icons

This folder contains the canonical, editable SVG logo marks and their 1024 × 1024 PNG app-icon exports for:

- Signal Hunt
- Waypoint Rally
- Waypoint Wings
- Treasure Hunter

The marks deliberately omit words so they remain legible at iPhone icon size. Apple applies the rounded-corner mask; the source art remains full-bleed and square.

Regenerate PNG exports after editing an SVG:

```sh
for source in Assets/Brand/AppIcons/*.svg; do
  rsvg-convert --width 1024 --height 1024 "$source" --output "${source%.svg}.png"
done
```
