# Third-Party Notices and Credits

EcoBalance (team 99 Bugs) uses the following third-party software, data, fonts and audio.
Each item stays under its own licence. Rows marked **TODO** still need their source confirmed.

## AI voice (mascot placeholder VO)

| Item | Source | Licence | Notes |
|---|---|---|---|
| Kokoro-82M text-to-speech model and voice `af_heart` | [hexgrad/Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) | Run offline on a PC by `tools/voice/generate_mascot_voice.py`. Only the generated clips ship (`Assets/EcoBalance/Resources/MascotVoice/`); the model is not included in this repository or the app. Clips with `source = kokoro` in `MascotVoice/manifest.csv` are AI placeholders to be replaced by recorded team VO. |

## Map and nature data

| Item | Source | Licence / terms | Notes |
|---|---|---|---|
| Elevation tiles (terrarium) | [AWS Terrain Tiles](https://registry.opendata.aws/terrain-tiles/) (Mapzen) | [Attribution required](https://github.com/tilezen/joerd/blob/master/docs/attribution.md) | Tiles shipped with the app for offline play. |
| Rivers and lakes | © [OpenStreetMap](https://www.openstreetmap.org/copyright) contributors, via the Overpass API | [ODbL 1.0](https://opendatacommons.org/licenses/odbl/) | Water data shipped with the app for offline play. |
| Species conservation status | [Wikidata](https://www.wikidata.org/) | CC0 1.0 | Queried at runtime. |
| Species sightings | [GBIF.org](https://www.gbif.org/) occurrence search | Per dataset (CC0 / CC BY) | Queried at runtime; cite GBIF.org. |
| Weather | [Open-Meteo.com](https://open-meteo.com/) forecast API | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) (free, no API key) | Queried at runtime. Weather data by Open-Meteo.com. |

## Fonts

| Font | Licence |
|---|---|
| Chewy | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) (Google Fonts) |
| Playpen Sans | [SIL Open Font License 1.1](https://openfontlicense.org/) (Google Fonts) |
| Liberation Sans (TextMesh Pro default) | SIL Open Font License 1.1 (bundled with Unity TextMesh Pro) |

## Audio

| File / folder | Source | Licence / terms |
|---|---|---|
| Animal SFX (`Audio/SFX/BlueSheep`, `Sheep SFX`, `Snow Leopard SFX`, `SnowLeopard`), river loop, UI clicks (`Audio/SFX/UI`, `Resources/UISound`) | Sound library provided by DigiPen Institute of Technology Singapore (audio course) | Licensed to DigiPen for student projects. Used in the game only; not for reuse outside this project. |
| Music and ambience (`Audio/BGM`), `Audio/Temp Assets` | Provided by DigiPen (audio course) or made by the team | As above |
| Mascot voice (`Resources/MascotVoice`) | Generated with Kokoro-82M (see "AI voice" above); recorded team VO replaces clips marked `recorded` in `manifest.csv` | Apache-2.0 model; clips made by the team |

## Art and 3D models

| File / folder | Source | Licence |
|---|---|---|
| `Art & Design/Art Assets/`, UI icons, earth mascot | **TODO** (team-made by UXGD?) | **TODO** |
| Animal models and animations (`Prefabs/Animals`, `Resources/Motion`) | **TODO** | **TODO** |

## Engine and tools

Built with [Unity](https://unity.com/) 6000.6.0f1 (Unity Companion License for Unity packages).
CI uses [GameCI](https://game.ci/) (MIT).

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
