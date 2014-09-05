﻿/* 
Simulate, Revert & Launch v0.10
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

namespace SRL {

	[KSPAddon(KSPAddon.Startup.EditorAny | KSPAddon.Startup.TrackingStation | KSPAddon.Startup.Flight | KSPAddon.Startup.SpaceCentre, false)]
	public class SRL : MonoBehaviour {

		// Initialiser les variables
		private ApplicationLauncherButton SRLSim;
		private GUIStyle SRLtext;
		private VesselRecoveryButton SRLrecoverybutton = null;
		[KSPField(isPersistant = true)]
		public static bool isSimulate = false;
		private bool SRLlastIsSimulate = !isSimulate;

		// Préparer les variables et les évènements
		private void Awake() {
			GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Add (OnLaunch);
			this.SRLtext = new GUIStyle ();
			this.SRLtext.stretchWidth = true;
			this.SRLtext.stretchHeight = true;
			this.SRLtext.alignment = TextAnchor.UpperCenter;
			this.SRLtext.fontSize = (Screen.height/20);
			this.SRLtext.fontStyle = FontStyle.Bold;
			this.SRLtext.normal.textColor = Color.red;
		}

		// Afficher le bouton de simulation
		private void OnGUIApplicationLauncherReady() {
			if (ApplicationLauncher.Ready) {
				this.SRLSim = ApplicationLauncher.Instance.AddModApplication (this.SRLSimOn, this.SRLSimOff, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/sim", false));
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION) {
					isSimulate = false;
				}
			}
		}

		// Bloquer l'accès au bouton de simulation après le lancement de la fusée
		private void OnLaunch(EventReport EventReport) {
			if (ApplicationLauncher.Ready) {
				this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
			}
		}

		private void SRLSimOn() {
			isSimulate = true;
		}

		private void SRLSimOff() {
			isSimulate = false;
		}

		// Activer la simulation
		private void SRLon() {
			if (ApplicationLauncher.Ready) {
				if (this.SRLSim.State != RUIToggleButton.ButtonState.TRUE) {
					this.SRLSim.SetTrue ();
				}
			}
			InputLockManager.RemoveControlLock ("SRLquicksave");
			InputLockManager.RemoveControlLock ("SRLquickload");
			InputLockManager.SetControlLock (ControlTypes.EVA_INPUT, "SRLevainput");
			InputLockManager.SetControlLock (ControlTypes.VESSEL_SWITCHING, "SRLvesselswitching");
			HighLogic.CurrentGame.Parameters.Flight.CanRestart = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor = true;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = true;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToTrackingStation = false;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsNear = false;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar = false;
			HighLogic.CurrentGame.Parameters.Flight.CanEVA = false;
			HighLogic.CurrentGame.Parameters.Flight.CanBoard = false;
			HighLogic.CurrentGame.Parameters.Flight.CanAutoSave = false;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = false;
			print ("SRL: Simulation ON");
		}

		// Désactiver la simulation
		private void SRLoff() {
			if (ApplicationLauncher.Ready) {
				if (this.SRLSim.State != RUIToggleButton.ButtonState.FALSE) {
					this.SRLSim.SetFalse ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (FlightGlobals.ActiveVessel.landedAt == "") {
						this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
					}
				}
			}
			InputLockManager.SetControlLock (ControlTypes.QUICKSAVE, "SRLquicksave");
			InputLockManager.SetControlLock (ControlTypes.QUICKLOAD, "SRLquickload");
			InputLockManager.RemoveControlLock ("SRLevainput");
			InputLockManager.RemoveControlLock ("SRLvesselswitching");
			HighLogic.CurrentGame.Parameters.Flight.CanRestart = false;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor = false;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = false;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = false;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToTrackingStation = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsNear = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar = true;
			HighLogic.CurrentGame.Parameters.Flight.CanEVA = true;
			HighLogic.CurrentGame.Parameters.Flight.CanBoard = true;
			HighLogic.CurrentGame.Parameters.Flight.CanAutoSave = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = true;
			print ("SRL: Simulation OFF");
		}

		// Supprimer le bouton de simulation et les évènements
		private void OnDestroy() {
			GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Remove (OnLaunch);
			if (this.SRLSim != null) {
				ApplicationLauncher.Instance.RemoveModApplication (this.SRLSim);
				this.SRLSim = null;
			}
		}

		// Mettre à jours les variables de simulation et désactiver le bouton de récupération si la fusée est au sol de Kerbin
		public void Update() {
			if (HighLogic.LoadedSceneIsGame) {
				if (HighLogic.CurrentGame.Parameters.Flight.CanRestart == HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter) {
					this.SRLlastIsSimulate = !isSimulate;
				}
				if (ApplicationLauncher.Ready) {
					if (this.SRLlastIsSimulate != isSimulate) {
						if (isSimulate) {
							this.SRLon ();
						} else {
							this.SRLoff ();
						}
						this.SRLlastIsSimulate = isSimulate;
					}
					if (HighLogic.LoadedSceneIsFlight) {
						if (FlightGlobals.ActiveVessel.LandedOrSplashed && FlightGlobals.ActiveVessel.mainBody.name == "Kerbin") {
							this.SRLrecoverybutton = (VesselRecoveryButton)GameObject.FindObjectOfType (typeof(VesselRecoveryButton));
							if (isSimulate && this.SRLrecoverybutton.slidingTab.toggleMode != ScreenSafeUISlideTab.ToggleMode.EXTERNAL) {
								this.SRLrecoverybutton.slidingTab.toggleMode = ScreenSafeUISlideTab.ToggleMode.EXTERNAL;
								this.SRLrecoverybutton.slidingTab.Collapse ();
								this.SRLrecoverybutton.ssuiButton.Lock ();
								print ("SRL: Recovery locked");
							} else if (!isSimulate && this.SRLrecoverybutton.slidingTab.toggleMode == ScreenSafeUISlideTab.ToggleMode.EXTERNAL) {
								this.SRLrecoverybutton = (VesselRecoveryButton)GameObject.FindObjectOfType (typeof(VesselRecoveryButton));
								this.SRLrecoverybutton.slidingTab.toggleMode = ScreenSafeUISlideTab.ToggleMode.HOVER;
								this.SRLrecoverybutton.ssuiButton.Unlock ();
								print ("SRL: Recovery unlocked");
							}
						}
					}
				}
			}
		}

		// Afficher l'activation de la simulation
		public void OnGUI() {
			if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
				if (isSimulate) {
					GUILayout.BeginArea (new Rect (0, (Screen.height / 10), Screen.width, 80), this.SRLtext);
					GUILayout.Label ("SIMULATION", this.SRLtext);
					GUILayout.EndArea ();
				}
			}
		}
	}
}