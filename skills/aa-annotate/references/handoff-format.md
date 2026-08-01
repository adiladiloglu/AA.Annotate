# AA.Annotate handoff format

Use these rules only when exact artifact or coordinate interpretation is required.

## Review Markdown

Each `## Capture N` is an independent screen state.

```text
Image: <primary exported image>
Annotated image: <numbered overview>

1. x=<left>, y=<top>, width=<width>, height=<height>
   Image: <annotation snippet>
   <comment>
```

- `Image:` is the primary exported image after privacy masking and export scaling.
- When a crop exists, the primary image is the cropped image.
- `Annotated image:` draws annotation rectangles and numbers on the privacy-safe primary image.
- An annotation's indented `Image:` is its focused snippet. Privacy masks are applied before snippets are generated.
- Black rectangles labeled `Privacy mask` are intentional redactions.

## Coordinates

- Uncropped coordinates are relative to the full exported `Image:`.
- Cropped coordinates are relative to the cropped exported `Image:`.
- Scaled coordinates are relative to the scaled exported `Image:`.
- `Crop:` records original screenshot coordinates and is only for mapping back to the original display.
- Export removes annotations fully outside the crop, clips partially intersecting annotations, and renumbers the remaining annotations within each capture.
- Privacy masks are clipped to the crop and scaled with the exported image.
- If metadata declares a non-full crop but `Image:` is an uncropped screenshot, report an inconsistent handoff rather than guessing.

## JSON

Resolve relative JSON paths from the directory containing `annotations.json`.

- `captures[].screenshotPath`: primary exported image.
- `captures[].croppedPath`: present when the primary image came from a crop.
- `captures[].annotatedImagePath`: privacy-safe numbered overview.
- `captures[].cropRect`: crop in original screenshot coordinates.
- `captures[].annotations[].boxRect`: rectangle relative to the primary exported image.
- `captures[].annotations[].imagePath`: focused annotation snippet.
- `captures[].privacyMasks[].boxRect`: mask relative to the primary exported image.
- `captures[].exportScalePercent`: scale applied to the image and exported coordinates.
- `captures[].screenshotPixelSize`: original capture size, not necessarily the primary exported image size.

Do not open an original unscaled or private image when an exported privacy-safe image is available.
