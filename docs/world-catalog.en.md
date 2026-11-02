# Town, regions and flag definitions — test reference

Read-only export from the English game on 2026-11-02: **42 tables, 2,416 rows, zero read errors**. One area-name resource returned the game missing-language message; it is marked unresolved rather than treated as an English name. Counts measure loaded parameter definitions, not validated unlock events.

## Coverage

| Source | Tables/groups | Rows | Status |
| --- | ---: | ---: | --- |
| areaNameDataArray | 16 | 161 | extracted |
| digiviceAreaDataArray | 16 | 180 | extracted |
| digiviceMapData | 1 | 16 | extracted |
| flagSet | 1 | 1738 | extracted |
| mapNameData | 1 | 16 | extracted |
| townAdditionDp | 1 | 141 | extracted |
| townAreaChgData | 1 | 21 | extracted |
| townBlockData | 1 | 7 | extracted |
| townGradeUpData | 1 | 21 | extracted |
| townGradeUpMaterialsData | 1 | 56 | extracted |
| townJumpData | 1 | 28 | extracted |
| townJumpDestinationData | 1 | 31 | extracted |

## Town facilities / upgrade definitions

The table includes aggregate upgrades as well as facilities. Each ID below has eight raw flag-set reference slots and four material-recipe reference slots. Slots are not automatically interpreted as levels, NPC recruitments or AND/OR conditions.

| ID | English name | Kind | Flag-set slots (0–7) | Recipe slots (1–4) |
| ---: | --- | ---: | --- | --- |
| 1943318422 | All Central Grade Up | 0 | 4011245244, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1664772913, 84696367, 84696367, 84696367 |
| 1943318421 | Jijimon's House | 1 | 4011245244, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1664772913, 1664772914, 84696367, 84696367 |
| 1943318420 | Square | 2 | 4011245244, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1748660972, 1748660971, 1748660970, 1748660969 |
| 1943318419 | Warehouse | 3 | 4212119345, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1664772915, 1664772916, 1664772917, 1664772918 |
| 1943318418 | Sender | 4 | 635112489, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1664772919, 1664772920, 84696367, 84696367 |
| 1943318417 | Builder | 5 | 1039752471, 288582549, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1664772921, 1681550499, 84696367, 84696367 |
| 1943318416 | Item Shop | 6 | 2272649181, 1691011400, 3395249699, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1681550498, 1681550497, 1681550496, 1681550503 |
| 1943318431 | Hospital | 7 | 3225944759, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1681550502, 1681550501, 84696367, 84696367 |
| 1943318430 | Restaurant | 8 | 752831598, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1681550500, 1681550507, 1681550506, 1698328086 |
| 1926540836 | Stock Market | 9 | 906108610, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1698328087, 1698328084, 84696367, 84696367 |
| 1926540837 | Advanced Item Shop | 10 | 3278533120, 4062113854, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1698328085, 1698328082, 84696367, 84696367 |
| 1926540838 | Field | 11 | 4011245244, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1698328083, 1698328080, 1698328081, 1698328094 |
| 1926540839 | Lab | 12 | 854107969, 1784734470, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1698328095, 1715105673, 1715105672, 1715105675 |
| 1926540832 | Digivolution Dojo | 13 | 1573395465, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1715105674, 1715105677, 84696367, 84696367 |
| 1926540833 | Training Hall | 14 | 4011245244, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1715105676, 1715105679, 84696367, 84696367 |
| 1926540834 | Fishing Hole | 15 | 2779022360, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1715105678, 1715105665, 1715105664, 1731883388 |
| 1926540835 | Colosseum | 16 | 3264186736, 1573395465, 1902619063, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1731883389, 1731883390, 84696367, 84696367 |
| 1926540844 | Arena | 17 | 2627274218, 941079076, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1731883391, 1731883384, 84696367, 84696367 |
| 1926540845 | Museum | 18 | 2240259585, 1441010315, 3156220330, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1731883385, 1731883386, 84696367, 84696367 |
| 1976873725 | Treasure Hunter | 19 | 3204214985, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1731883387, 1731883380, 84696367, 84696367 |
| 1976873724 | Spacetime Transporter | 20 | 101904181, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926, 1402660926 | 1731883381, 1748660975, 1748660974, 1748660973 |

Material IDs are joined to the English item catalogue in [town-links.en.json](../data/town-links.en.json). Original slots and unresolved references are preserved, including zero-quantity entries. No placeholder/sentinel value is silently removed.

## Region names

| Map number from mapNameData | English name |
| ---: | --- |
| 1 | Floatia |
| 2 | Nigh Plains |
| 3 | Server Desert |
| 4 | Ohguino Wastelands |
| 5 | Logic Volcano |
| 6 | MOD Cape |
| 7 | Bony Resort |
| 8 | Faulty Ex Machina |
| 9 | Absolute Zero |
| 10 | Infinite Cauldron |
| 81 | Dimension M |
| 82 | Dimension G |
| 83 | Dimension T |
| 84 | Dimension P |
| 85 | Dimension E |
| 86 | Dimension Z |

## Area names by source group

The source-group index is kept distinct from a map number. Matching names to navigation records can be ambiguous; no +1 offset or destination interpretation is assumed. Digivice data can describe display/selection conditions rather than physical entrance gates.

### Source group 0

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 10 | **Unresolved resource** | 2414990469 |
| 11 | Jijimon's House | 2414990468 |
| 12 | Training Hall | 2414990471 |
| 13 | Builder | 2414990470 |
| 20 | Business District | 2364657612 |
| 21 | Item Shop | 2364657613 |
| 22 | Hospital | 2364657614 |
| 23 | Restaurant | 2364657615 |
| 24 | Advanced Item Shop | 2364657608 |
| 30 | Research District | 2381435327 |
| 31 | Lab 1F | 2381435326 |
| 32 | Lab 2F | 2381435325 |
| 33 | Lab 3F | 2381435324 |
| 34 | Digivolution Dojo | 2381435323 |
| 35 | Training Hall | 2381435322 |
| 40 | Entertainment District | 2331102438 |
| 41 | Colosseum | 2331102439 |
| 42 | Arena | 2331102436 |
| 43 | Museum | 2331102437 |
| 50 | Agricultural District | 2347880025 |
| 60 | Dimensional District | 2297547136 |

### Source group 1

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Vast Plateau | 2297400104 |
| 2 | Forest Path | 2297400107 |
| 3 | Cave Entrance | 2297400106 |
| 4 | Old Cableway | 2297400109 |
| 5 | Power Plant #1 | 2297400108 |
| 6 | Power Plant #2 | 2297400111 |
| 7 | Ruins Lake | 2297400110 |

### Source group 2

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Noise Storm | 2196587297 |
| 2 | Server Cemetery | 2196587298 |
| 3 | To Hallowed Hall | 2196587299 |
| 4 | Stepstep | 2196587300 |
| 5 | Dead End Town | 2196587301 |
| 6 | Tower Road | 2196587302 |
| 7 | Bus Terminal | 2196587303 |
| 8 | Server Tower | 2196587304 |
| 9 | Server Tower Interior | 2196587305 |

### Source group 3

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Guts Wastes | 2365496286 |
| 2 | Secret Graveyard | 2365496285 |
| 3 | Dori Tunnel Hub 1 | 2365496284 |
| 4 | Dori Tunnel Hub 2 | 2365496283 |
| 5 | Palace of Thorns | 2365496282 |
| 6 | Generator Prison Room | 2365496281 |
| 7 | Rose Palace | 2365496280 |
| 8 | Rosemon's Room | 2365496279 |

### Source group 4

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Fire Wall | 117148247 |
| 2 | Gateway | 117148244 |
| 3 | Flame Lands | 117148245 |
| 4 | Fire Circuit | 117148242 |
| 5 | Dragon's Trident | 117148243 |
| 6 | Garbage Pile | 117148240 |
| 7 | Crimson Shrine | 117148241 |
| 8 | Taomon's Medit. Place | 117148254 |

### Source group 5

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Inlet Cape | 553219212 |
| 2 | MOD Ship 1.0 | 553219215 |
| 3 | MOD Ship 2.0 | 553219214 |
| 4 | Ship 2.0 Cabin | 553219209 |
| 5 | MOD Ship 3.0 | 553219208 |
| 6 | Ship 3.0 Cabin | 553219211 |
| 7 | Flag MOD Ship | 553219210 |
| 8 | Captain's Cabin | 553219205 |

### Source group 6

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Bony Drive | 452406405 |
| 2 | Celeb Red Room | 452406406 |
| 3 | Celeb Green Room | 452406407 |
| 4 | Celeb Blue Room | 452406400 |
| 5 | Pink Stone House | 452406401 |
| 6 | Pink Stone House Int. | 452406402 |
| 7 | Corpseway | 452406403 |
| 8 | Graveyard | 452406412 |
| 9 | Night Church | 452406413 |

### Source group 7

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | West Coast | 84431618 |
| 2 | Greenery Island | 84431617 |
| 3 | Boulder Island | 84431616 |
| 4 | Flowerbed Island | 84431623 |
| 5 | Pathfork Island | 84431622 |
| 6 | Control Island | 84431621 |
| 7 | Control Room | 84431620 |
| 8 | East Coast | 84431627 |
| 9 | North Coast | 84431626 |
| 10 | Drainage Path | 67654032 |
| 11 | KingSukamon's Chamber | 67654033 |

### Source group 8

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Freeze Way | 2667934779 |
| 2 | Frost Cathedral | 2667934776 |
| 3 | Ice Sculpture Room | 2667934777 |
| 4 | Icy Hall | 2667934782 |
| 5 | Frozen Divider | 2667934783 |
| 6 | Frozen Way | 2667934780 |
| 7 | Icy Way Temple | 2667934781 |
| 8 | Eternal Glacier | 2667934770 |
| 9 | Concealed Gorge | 2667934771 |

### Source group 9

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Layer 01 | 375174449 |
| 2 | Layer 02 | 375174450 |
| 3 | Layer 03 | 375174451 |
| 4 | Layer 04 | 375174452 |
| 5 | Layer 05 | 375174453 |
| 6 | Layer 06 | 375174454 |
| 7 | Core 02 | 375174455 |
| 8 | Core 01 | 375174456 |
| 9 | Core 00 | 375174457 |

### Source group 10

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Memory 1 | 118864299 |
| 2 | Memory 2 | 118864296 |
| 3 | Memory 3 | 118864297 |
| 4 | Memory 4 | 118864302 |
| 5 | Memory 5 | 118864303 |
| 6 | Memory 6 | 118864300 |
| 7 | Memory 7 | 118864301 |
| 8 | Memory 8 | 118864290 |
| 9 | Memory 9 | 118864291 |
| 10 | Memory 10 | 135641885 |

### Source group 11

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Memory 1 | 2165483552 |
| 2 | Memory 2 | 2165483555 |
| 3 | Memory 3 | 2165483554 |
| 4 | Memory 4 | 2165483557 |
| 5 | Memory 5 | 2165483556 |
| 6 | Memory 6 | 2165483559 |
| 7 | Memory 7 | 2165483558 |
| 8 | Memory 8 | 2165483561 |
| 9 | Memory 9 | 2165483560 |
| 10 | Memory 10 | 2148705870 |

### Source group 12

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Memory 1 | 2064670777 |
| 2 | Memory 2 | 2064670778 |
| 3 | Memory 3 | 2064670779 |
| 4 | Memory 4 | 2064670780 |
| 5 | Memory 5 | 2064670781 |
| 6 | Memory 6 | 2064670782 |
| 7 | Memory 7 | 2064670783 |
| 8 | Memory 8 | 2064670768 |
| 9 | Memory 9 | 2064670769 |
| 10 | Memory 10 | 2081448363 |

### Source group 13

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Memory 1 | 2233579734 |
| 2 | Memory 2 | 2233579733 |
| 3 | Memory 3 | 2233579732 |
| 4 | Memory 4 | 2233579731 |
| 5 | Memory 5 | 2233579730 |
| 6 | Memory 6 | 2233579729 |
| 7 | Memory 7 | 2233579728 |
| 8 | Memory 8 | 2233579743 |
| 9 | Memory 9 | 2233579742 |
| 10 | Memory 10 | 2216802148 |

### Source group 14

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Memory 1 | 2132766959 |
| 2 | Memory 2 | 2132766956 |
| 3 | Memory 3 | 2132766957 |
| 4 | Memory 4 | 2132766954 |
| 5 | Memory 5 | 2132766955 |
| 6 | Memory 6 | 2132766952 |
| 7 | Memory 7 | 2132766953 |
| 8 | Memory 8 | 2132766950 |
| 9 | Memory 9 | 2132766951 |
| 10 | Memory 10 | 2149544513 |

### Source group 15

| Area number | English name | Language hash |
| ---: | --- | ---: |
| 1 | Memory 1 | 2031954148 |
| 2 | Memory 2 | 2031954151 |
| 3 | Memory 3 | 2031954150 |
| 4 | Memory 4 | 2031954145 |
| 5 | Memory 5 | 2031954144 |
| 6 | Memory 6 | 2031954147 |
| 7 | Memory 7 | 2031954146 |
| 8 | Memory 8 | 2031954157 |
| 9 | Memory 9 | 2031954156 |
| 10 | Memory 10 | 2015176562 |
| 11 | Memory 11 | 2015176563 |
| 12 | Memory 12 | 2015176560 |

## Flag-set reference and limits

1,738 flag sets are present, each with an ID and eight signed integer slots. The numbers alone do not identify a named recruitment, quest or unlock. Values such as 4096 and repeated unresolved reference IDs are preserved pending analysis. A flag set with only such values is not labelled always-on or empty without verifying game code.

141 town development-point definitions are extracted without NPC names. They are not yet a named recruitment list. Town names and improvements are parameter definitions; effects and save/reload behavior are unverified.

## Test protocol

1. Pick one facility or gate, record its displayed name, location, prosperity, materials and current save slot.
2. Capture scenario flags, town grades and current map/area before the action with dedicated instrumentation; the catalogue exporter does not yet monitor these changes.
3. Perform one normal recruitment, construction, upgrade or gate-opening action. Compare state changes with the candidate IDs and flag sets; separate requests from successful completion.
4. Save/reload and confirm persistence. Do not force arbitrary flags or skip associated story events during this first validation.

Data: [world-catalog.en.json](../data/world-catalog.en.json), [town-links.en.json](../data/town-links.en.json). Export method: [export-town-regions-flags.md](export-town-regions-flags.md). No recruitment, region unlock, scene skip or progression mutation was performed by the exporter.
