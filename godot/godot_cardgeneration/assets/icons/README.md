# Ikoner

Denne mappen inneholder SVG-ikoner som Godot-kortverktøyet bruker i kort, menyer og eksport.

Ikoner skal ligge her når de trengs av verktøyet, i stedet for å blandes med kortbilder eller placeholder-bilder.

Retning:

* `elements/` brukes for glyph-only element- og ressursikoner. Medallionens felt, svak elementfarge og outline tegnes av renderer.
* `symbols/` brukes for glyph-only kortsymboler, for eksempel styrke og pil. Power-medallionens felt og outline tegnes av renderer.
* SVG er foretrukket kildeformat for ikoner fordi det skalerer godt til både preview og print.
* Decks kan velge egne glyph-textures per element uten å endre kortdata.
