/* 
QuickRevert
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
//
// modified by Malah for Simulate, Revert & Launch
//

using System;
using KSP;
using UnityEngine;

namespace SRL {
	[KSPAddon(KSPAddon.Startup.MainMenu | KSPAddon.Startup.EditorAny | KSPAddon.Startup.TrackingStation | KSPAddon.Startup.Flight | KSPAddon.Startup.SpaceCentre, false)]
	public class QuickRevert_fct : MonoBehaviour {
		public const string VERSION = "1.10";
		private static bool isdebug = true;

		[KSPField(isPersistant = true)]
		public static Game Save_FlightStateCache;
		[KSPField(isPersistant = true)]
		public static GameBackup Save_PostInitState;
		[KSPField(isPersistant = true)]
		public static GameBackup Save_PreLaunchState;
		[KSPField(isPersistant = true)]
		private static ConfigNode Save_ShipConfig;
		[KSPField(isPersistant = true)]
		public static Guid Save_Vessel_Guid = Guid.Empty;

		public static bool VesselExist(Guid _guid, out Vessel vessel) {
			vessel = null;
			foreach (Vessel _vessel in FlightGlobals.Vessels.ToArray()) {
				if (_vessel.id == _guid) {
					vessel = _vessel;
					return true;
				}
			}
			return false;
		}

		private void Awake() {
			GameEvents.onFlightReady.Add (OnFlightReady);
			GameEvents.onLevelWasLoaded.Add (OnLevelWasLoaded);
		}

		private void OnDestroy() {
			GameEvents.onFlightReady.Remove (OnFlightReady);
			GameEvents.onLevelWasLoaded.Remove (OnLevelWasLoaded);
		}

		private void OnFlightReady() {
			if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH) {
				Save_Vessel_Guid = Guid.Empty;
				try {
					Save_FlightStateCache = FlightDriver.FlightStateCache;
				} catch {
					Debug ("Can't save Save_FlightStateCache !");
				}
				try {
					Save_PostInitState = FlightDriver.PostInitState;
				} catch {
					Debug ("Can't save Save_PostInitState !");
				}
				try {
					Save_PreLaunchState = FlightDriver.PreLaunchState;
				} catch {
					Debug ("Can't save Save_PreLaunchState try an other solution !");
					try {
						Save_PreLaunchState = new GameBackup(Save_FlightStateCache);
					} catch {
						Debug ("Can't save Save_PreLaunchState !");
					}
				}
				try {
					Save_ShipConfig = ShipConstruction.ShipConfig;
				} catch {
					Debug ("Can't save Save_ShipConfig !");
				}
				Save_Vessel_Guid = FlightGlobals.ActiveVessel.id;
				Debug("Revert saved");
			} else {
				if (Save_Vessel_Guid != Guid.Empty && HighLogic.CurrentGame.Parameters.Flight.CanRestart) {
					try {  
						FlightDriver.FlightStateCache = Save_FlightStateCache;
					} catch {
						Debug ("Can't load Save_FlightStateCache !");
					}
					try { 
						FlightDriver.PostInitState = Save_PostInitState;
					} catch {
						Debug ("Can't load Save_PostInitState !");
					}
					try { 
						FlightDriver.PreLaunchState = Save_PreLaunchState;
					} catch {
						Debug ("Can't load Save_PreLaunchState !");
					}
					try { 
						ShipConstruction.ShipConfig = Save_ShipConfig;
					} catch {
						Debug ("Can't load Save_ShipConfig !");
					}
					Debug ("Revert loaded");
				}
			}
		}
			
		private void OnLevelWasLoaded(GameScenes gamescenes) {
			if (HighLogic.LoadedScene != GameScenes.FLIGHT) {
				Save_Vessel_Guid = Guid.Empty;
			}
		}

		private static void Debug(string _string) {
			if (isdebug) {
				print ("QuickRevert" + VERSION + ": " + _string);
			}
		}
	}
}