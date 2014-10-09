/* 
Simulate, Revert & Launch
Copyright 2014 Malah

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
*/

using System;
using UnityEngine;
using KSP;
using DeadlyReentry;
using SRL;

namespace SRL {
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class DeadlyReentry_wrapper : MonoBehaviour {

		public const string VERSION = "0.10";

		private double crewGClamp;
		private double crewGPower;
		private double crewGMin;
		private double crewGWarn;
		private double crewGLimit;
		private double crewGKillChance;

		private bool DREC_toload = false;

		// Préparer les évènements
		private void Awake() {
			GameEvents.onLevelWasLoaded.Add (OnLevelWasLoaded);
		}

		// Désactiver DeadlyReentry
		private void OnLevelWasLoaded(GameScenes gamescenes) {
			if ((SRL.orbit || SRL.CelestialBody != 1) && SRL.CanSimulate && HighLogic.LoadedSceneIsFlight && SRL.isSimulate) {
				DREC_toload = true;
				crewGClamp = DeadlyReentry.ModuleAeroReentry.crewGClamp;
				crewGPower = DeadlyReentry.ModuleAeroReentry.crewGPower;
				crewGMin = DeadlyReentry.ModuleAeroReentry.crewGMin;
				crewGWarn = DeadlyReentry.ModuleAeroReentry.crewGWarn;
				crewGLimit = DeadlyReentry.ModuleAeroReentry.crewGLimit;
				crewGKillChance = DeadlyReentry.ModuleAeroReentry.crewGKillChance;

				DeadlyReentry.ModuleAeroReentry.crewGClamp = 10^90;
				DeadlyReentry.ModuleAeroReentry.crewGPower = 10^90;
				DeadlyReentry.ModuleAeroReentry.crewGMin = 10^90;
				DeadlyReentry.ModuleAeroReentry.crewGWarn = 10^90;
				DeadlyReentry.ModuleAeroReentry.crewGLimit = 10^90;
				DeadlyReentry.ModuleAeroReentry.crewGKillChance = 10^90;

				print ("SRL-DREC" + VERSION + ": Save variables");
			}
		}

		// Réactiver DeadlyReentry
		public void Update() {
			if ((SRL.orbit || SRL.CelestialBody != 1) && SRL.CanSimulate && HighLogic.LoadedSceneIsFlight && SRL.isSimulate) {
				if (DREC_toload && InputLockManager.GetControlLock("SRLall") != ControlTypes.All) {
					DREC_toload = false;
					DeadlyReentry.ModuleAeroReentry.crewGClamp = crewGClamp ;
					DeadlyReentry.ModuleAeroReentry.crewGPower = crewGPower;
					DeadlyReentry.ModuleAeroReentry.crewGMin = crewGMin;
					DeadlyReentry.ModuleAeroReentry.crewGWarn = crewGWarn;
					DeadlyReentry.ModuleAeroReentry.crewGLimit = crewGLimit;
					DeadlyReentry.ModuleAeroReentry.crewGKillChance = crewGKillChance;
					print ("SRL-DREC" + VERSION + ": Load variables");
				} 
			}
		}

		// Supprimer les évènements
		private void OnDestroy() {
			GameEvents.onLevelWasLoaded.Remove (OnLevelWasLoaded);
		}
	}
}

