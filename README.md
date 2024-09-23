<p align="center"><img src="img/Viconia_Soundset_Header.png?raw=true" alt="Header image"/></p>

# About

A mod for Owlcat's Pathfinder: Wrath of the Righteous.

Adds a custom, standalone soundset for female PCs and mercenaries in Wrath of the Righteous. Does not overwrite or replace any of the vanilla soundsets.

Also includes a custom portrait set, if required.

# Install
1. Download and install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) and set it up for WOTR ("Pathfinder Second Adventure").
1. Download [PC Female Viconia DeVir Soundset](https://github.com/DarthParametric/WOTR_Custom_Soundset_Viconia_DeVir/releases/latest).
1. Drag the mod zip into Unity Mod Manager.
1. Run your game.
1. There is an optional setting in UMM that allows you to adjust the frequency of the movement barks, as these are quite infrequent by default:
   <p align="center"><img src="img/Viconia_Soundset_UMM_Options.png?raw=true" alt="UMM bark config options screenshot" width="500" height="115"/></p>
1. The custom soundset will appear in the character creator Voice list for females after all the vanilla soundsets:
   <p align="center"><img src="img/Viconia_Soundset_Character_Creator_List.png?raw=true" alt="Character creator voice selection screenshot" width="288" height="400"/></p>

# Notes
- Certain lines may be overly loud due to the nature of the source audio and the normalisation process. Please report anything obnoxious so it can be manually adjusted.
- The source audio lacks much in the way of whispering or quiet lines, so there is minimal diversity in the stealth lines, along with some regular lines with the volume reduced.
- Similarly, there's a lack of diversity or even anything appropriate at all for certain lines. If anyone skilled with AI voice generation is willing to take a crack at creating new custom lines, let me know.
- Some Grey DeLisle audio from other games has been used to bulk out certain barks (mostly non-verbal attack grunts, pain cries, etc.)
- If you wish to use the optional custom portraits, copy the folder into `%UserProfile%\AppData\LocalLow\Owlcat Games\Pathfinder Wrath of the Righteous\Portraits` (on Windows).

# Thanks & Acknowledgements
- Uses the `wrathsoundvoicemod` mod template from [OwlcatNuGetTemplates](https://github.com/xADDBx/OwlcatNuGetTemplates) as a basis.
- microsoftenator2022 - Provided lots of help with troubleshooting the original Wwise and project setup, leading to the creation of the dedicated `wrathsoundvoicemod` template (as well as extensive work on the original `wrathsoundmod` template).
- Everyone in the `#mod-dev-technical` channel of the Owlcat Discord server for various modding-related discussions and suggestions, help troubleshooting issues, and answering general questions.
- Original audio taken from Bioware's Baldur's Gate, Baldur's Gate II, Star Wars The Old Republic, Beamdog's BG1 Siege of Dragonspear, and Obsidian's Star Wars Knights of the Old Republic II.
