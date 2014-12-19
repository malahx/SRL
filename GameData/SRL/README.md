#### Simulate, Revert & Launch
#### A plugin for Kerbal Space Program 0.90.0
#### Copyright 2014 Malah

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>. 


#### What is it?

SRL is a plugin which adds the possibility to:
- simulate a vessel from all celestial bodies while in orbit without save/return to spacecenter/recover,
- launch a vessel from Kerbin without revert/quicksave/quickload.

This mod will change your save game, I suggest you back up your save game before you install it.

#### How to install it?

Unzip all files. Put the SRL folder in your KSP/GameData folder.
If you updating the mod, you don't need to uninstall before to install the mod. You can just copie and remplace the existing files (in that way, you can keep your previous configurations). 

#### How to uninstall it?

On the space center, you need to open the SRL configuration panel and disable SRL, now you can shutdown your game and delete the SRL folder in your KSP/GameData folder.

#### How does it work?

Concretely this mod will change several variables of your save game as:
- CanRestart
- CanLeaveToEditor
- CanQuickLoad
- CanQuickSave
- CanLeaveToTrackingStation
- CanAutoSave
- CanLeaveToSpaceCenter

#### Troubleshooting?

This mod will not work well with:
- all other mods that will lock/unlock the recovery button, such as multiplayer's mods,
- mods which change planets, such as PlanetFactory.

#### Changelog

v1.32 - 2014.12.19
- Updated to 0.90

v1.31 - 2014.11.01
- Fix: Corrected the loading of the configurations files for others solar systems,
- Fix: Corrected a parameter of RSS configurations files, the Moon will be properly unlocked for a simulation.

v1.30 - 2014.10.31
- New: Added an option to unlock the celestial bodies for a simulation with the tech tree,
- New: Added the configurations files for others solar systems (RealSolarSystem and Kerbol 6.4x),
- New: Added an automatic unlock for the achievements on existing savegames,
- New: Added an option making that the amount of costs of a simulation is influenced by the penalties game difficulty,
- New: Added an option to change the skin of the windows,
- Fix: The achievements to unlock simulation can be made when a vessel changed SOI,
- Fix: Corrected the panel so that it does not auto hide in the editor,
- Fix: The orbital simulation will always be in the sunlight,
- Fix: Deleted messages when you unlock a landed simulation (that's not yet implemented on SRL),
- Fix: The last update blocked the quickload on simulation,
- Fix: Some other minor fixes.
- Now, this mod is CKAN compatible.

v1.20 - 2014.10.16
- New: Added an option making that the amount of costs of a simulation is influenced by the vessel's price,
- New: Added an option making that the amount of costs of a simulation is influenced by the celestial body,
- Fix: Disabled the possibility to enable/disable a simulation after the vessel moved on the launch pad / runway,
- Fix: Sometime a simulation could be disabled after the launch,
- Fix: Sometime the simulation cost could be automatically set to 0,
- Fix: Automatically detect the atmosphere altitude of a celestial body to avoid an error on the solar systems configurations files (which is planned for a next update),
- Fix: Keep the last configuration after an update,
- Fix: Some other minor fixes.

v1.10 - 2014.10.09
- New: Added the simulation while in orbit on all celestial bodies (integrate HyperEdit 1.2.4.2),
- New: Added the revert to editor (integrate QuickRevert 1.10),
- Fix: Disabled quicksave at prelaunch,
- Fix: You can switch to far vessel, the revert will always be for the current simulation,
- Updated to 0.25

v1.00 - 2014.09.14
First release
- New: Added a configuration panel.
- New: Added a persistent file config per saved game.
- New: Added the management of the credits, reputation and science.
- Fix: EVA on the launchpad have been corrected.
- Fix: allowed EVA and switch near vessel in simulation.

v0.20 - 2014.09.07
Second beta release
Fix:
Corrected the quickload:
- now after a quickload, you can revert to the launch (meaning you can disable the simulation and go to the space center, revert to the editor doesn't work though),
- you can't quickload from another simulation (with the GUI this is yet possible),
- you can't disable a simulation after a quickload.
The simulation's button is disabled when you are in EVA on the launchpad.

v0.10 - 2014.09.05
Initial beta release.

#### Planned updates/new features

- Add the landed simulation,
- Add an automatic backup to the simulation for when KSP crashes,
- Tweak the configurations files to easy include others solar systems with default value,
- Integrate to the contracts,
- Integrate others mods such as Blizzy78 toolbar.

#### Thanks!

to regex for his HardMode, 
to magico13 for his KCT,
to Team HyperEdit and Ezriilc for HyperEdit,
to all others mods developers which make this game really huge,
to my friend Neimad who corrects my bad english ...
and to Squad for this awesome game.

#### Links

- http://forum.kerbalspaceprogram.com/threads/93722
- http://forum.kerbalspaceprogram.com/threads/92973
- http://beta.kerbalstuff.com/mod/145
- http://kerbal.curseforge.com/ksp-mods/224071
- https://github.com/malahx/SRL

- HyperEdit: http://forum.kerbalspaceprogram.com/threads/37756
- QuickRevert: http://forum.kerbalspaceprogram.com/threads/95168
- RealSolarSystem: http://forum.kerbalspaceprogram.com/threads/55145
- 6.4x Kerbol System: http://forum.kerbalspaceprogram.com/threads/90088
- HardMode: http://forum.kerbalspaceprogram.com/threads/78895
- KCT: http://forum.kerbalspaceprogram.com/threads/69310
- CKAN: http://forum.kerbalspaceprogram.com/threads/97434
- KSP-AVC: http://forum.kerbalspaceprogram.com/threads/79745