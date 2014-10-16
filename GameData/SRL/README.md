#### Simulate, Revert & Launch
#### A plugin for Kerbal Space Program 0.25.0
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


#### What is it ?

SRL is a plugin which adds the possibility to:
- simulate a vessel from all celestial bodies while in orbit without save/return to spacecenter/recover,
- launch a vessel from Kerbin without revert/quicksave/quickload.

This mod will change your save game, I suggest you back up your save game before you install it.

#### How to install it ?

Unzip all files. Put the SRL folder in your KSP/GameData folder.

#### How to uninstall it ?

On the space center, you need to open the SRL configuration panel and disable SRL, now you can shutdown your game and delete the SRL folder in your KSP/GameData folder.

#### How does it work ?

Concretely this mod will change several variables of your save game as:
- CanRestart
- CanLeaveToEditor
- CanQuickLoad
- CanQuickSave
- CanLeaveToTrackingStation
- CanAutoSave
- CanLeaveToSpaceCenter

#### Troubleshooting ?

This mod will not work well with:
- all other mods that will lock/unlock the recovery button, such as multiplayer's mods,
- mods which change planets, such as RSS or PlanetFactory.

#### Changelog

v1.20 - 2014.10.16
New: Added an option making that the amount of costs of a simulation is influenced by the vessel's price,
New: Added an option making that the amount of costs of a simulation is influenced by the celestial body,
Fix: Disabled the possibility to enable/disable a simulation after the vessel moved on the launch pad / runway,
Fix: Sometime a simulation could be disabled after the launch,
Fix: Sometime the simulation cost could be automatically set to 0,
Fix: Automatically detect the atmosphere altitude of a celestial body to avoid an error on the solar systems configurations files (which is planned for a next update),
Fix: Keep the last configuration after an update,
Fix: Some others minor fix.

v1.10 - 2014.10.09
- New: Added the simulation while in orbit on all celestial bodies (integrate HyperEdit),
- New: Added the revert to editor (integrate QuickRevert),
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
- Add configurations files for other system (RSS ...),
- Automatic backup the simulation for when KSP crashes,
- Integrate to the contracts,
- Integrate to the tech tree,
- Integrate others mods such as Blizzy78 toolbar.

#### Thanks !

to regex for his HardMode, 
to magico13 for his KCT,
to Team HyperEdit and Ezriilc for HyperEdit,
to all others mods developers which render this game really huge,
to my friend Neimad who corrects my bad english ...
and to Squad for this awesome game.

#### Links

http://forum.kerbalspaceprogram.com/threads/93722
http://forum.kerbalspaceprogram.com/threads/92973
http://beta.kerbalstuff.com/mod/145
http://kerbal.curseforge.com/ksp-mods/224071
https://github.com/malahx/SRL

HyperEdit:
http://forum.kerbalspaceprogram.com/threads/37756

QuickRevert:
http://forum.kerbalspaceprogram.com/threads/95168