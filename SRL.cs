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

namespace SRL {

	[KSPAddon(KSPAddon.Startup.EditorAny | KSPAddon.Startup.TrackingStation | KSPAddon.Startup.Flight | KSPAddon.Startup.SpaceCentre, false)]
	public class SRL : MonoBehaviour {

		public const string VERSION = "0.30";

		// Initialiser les variables
		private ApplicationLauncherButton SRLSim;
		private GUIStyle SRLtexts;
		private VesselRecoveryButton SRLrecoverybutton = null;
		private int SRLindex = 0;
		private string SRLtext = "SIMULATION";
		private bool SRLwindow = false;
		private Texture SRLsimtexture = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/sim", false);
		private Texture SRLslrtexture = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/srl", false);
		protected String SRLfileconfig = KSPUtil.ApplicationRootPath + "GameData/SRL/config.txt";

		// Variables de la simulation
		[KSPField(isPersistant = true)]
		public static bool isSimulate = false;
		[KSPField(isPersistant = true)]
		private static GameBackup SRLPostInitState;
		[KSPField(isPersistant = true)]
		private static GameBackup SRLPreLaunchState;
		[KSPField(isPersistant = true)]
		private static Game SRLFlightStateCache;
		private bool SRLlastIsSimulate = !isSimulate;

		// Variables de la configuration
		[Persistent]
		private bool SRLenable;
		[Persistent]
		private bool SRLironman;
		[Persistent]
		private bool SRLsimulate;
		/*[Persistent]
		private bool SRLmoney;
		[Persistent]
		private bool SRLreputation;
		[Persistent]
		private bool SRLlongsimprice;
		[Persistent]
		private bool SRLearnscience;
		[Persistent]
		private bool SRLtechtree;
		[Persistent]
		private bool SRLcontracts;*/

		// Préparer les variables et les évènements
		private void Awake() {
			GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Add (OnLaunch);
			GameEvents.onGameStateSaved.Add (OnGameStateSaved);
			GameEvents.onFlightReady.Add (OnFlightReady);
			GameEvents.onCrewOnEva.Add (OnCrewOnEva);
			GameEvents.onCrewBoardVessel.Add (OnCrewBoardVessel);
			this.SRLtexts = new GUIStyle ();
			this.SRLtexts.stretchWidth = true;
			this.SRLtexts.stretchHeight = true;
			this.SRLtexts.alignment = TextAnchor.UpperCenter;
			this.SRLtexts.fontSize = (Screen.height/20);
			this.SRLtexts.fontStyle = FontStyle.Bold;
			this.SRLtexts.normal.textColor = Color.red;
		}

		// Afficher le bouton de simulation
		private void OnGUIApplicationLauncherReady() {
			if (System.IO.File.Exists (this.SRLfileconfig)) {
				ConfigNode SRLTemp = ConfigNode.Load (this.SRLfileconfig);
				ConfigNode.LoadObjectFromConfig (this, SRLTemp);
			} else {
				this.SRLwindow = false;
				this.SRLenable = true;
				this.SRLironman = true;
				this.SRLsimulate = true;
				/*this.SRLmoney = false;
				this.SRLreputation = false;
				this.SRLlongsimprice = false;
				this.SRLearnscience = false;
				this.SRLtechtree = false;
				this.SRLcontracts = false;*/
			}
			if (ApplicationLauncher.Ready) {
				this.SRLSim = ApplicationLauncher.Instance.AddModApplication (this.SRLSimOn, this.SRLSimOff, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, this.SRLslrtexture);
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION) {
					isSimulate = false;
				}
				if (this.SRLenable && this.SRLsimulate && HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
					this.SRLSim.SetTexture(this.SRLsimtexture);
				}
				if (!this.SRLenable || !this.SRLsimulate) {
					this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER;
				}
			}
		}

		// Bloquer l'accès au bouton de simulation après le lancement de la fusée
		private void OnLaunch(EventReport EventReport) {
			if (this.SRLenable && this.SRLsimulate && ApplicationLauncher.Ready) {
				this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
			}
		}

		// Activer le quickload après un quicksave
		private void OnGameStateSaved(Game game) {
			if (this.SRLenable && this.SRLsimulate && isSimulate && HighLogic.LoadedSceneIsFlight) {
				HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = true;
				print ("SRL"+VERSION+": Quickload ON");
			}
		}

		// Activer le revert après un quickload
		private void OnFlightReady() {
			if (this.SRLenable && this.SRLsimulate) {
				if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH) {
					SRLFlightStateCache = FlightDriver.FlightStateCache;
					SRLPostInitState = FlightDriver.PostInitState;
					SRLPreLaunchState = FlightDriver.PreLaunchState;
					print ("SRL" + VERSION + ": Revert saved");
				} else {
					FlightDriver.FlightStateCache = SRLFlightStateCache;
					FlightDriver.PostInitState = SRLPostInitState;
					FlightDriver.PreLaunchState = SRLPreLaunchState;
					print ("SRL" + VERSION + ": Revert loaded");
				}
			}
		}

		// Supprimer le bouton de simulation à l'EVA
		private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> part) {
			if (this.SRLenable && this.SRLsimulate && part.from.vessel.situation == Vessel.Situations.PRELAUNCH && ApplicationLauncher.Ready) {
				this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
			}
		}

		// Remettre le bouton de simulation après l'EVA
		private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> part) {
			if (this.SRLenable && this.SRLsimulate && part.from.vessel.situation == Vessel.Situations.PRELAUNCH && ApplicationLauncher.Ready) {
				this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
			}
		}

		// Supprimer le bouton de simulation et les évènements
		private void OnDestroy() {
			GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Remove (OnLaunch);
			GameEvents.onGameStateSaved.Remove (OnGameStateSaved);
			GameEvents.onFlightReady.Remove (OnFlightReady);
			GameEvents.onCrewOnEva.Remove (OnCrewOnEva);
			GameEvents.onCrewBoardVessel.Remove (OnCrewBoardVessel);
			if (this.SRLSim != null) {
				ApplicationLauncher.Instance.RemoveModApplication (this.SRLSim);
				this.SRLSim = null;
			}
		}

		// Activer le bouton
		private void SRLSimOn() {
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				this.SRLwindow = true;
			} else {
				isSimulate = true;
			}
		}

		// Désactiver le bouton
		private void SRLSimOff() {
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				this.SRLwindow = false;
				ConfigNode SRLTemp = ConfigNode.CreateConfigFromObject(this, new ConfigNode());
				SRLTemp.Save(this.SRLfileconfig);
			} else {
				isSimulate = false;
			}
		}

		// Panneau de configuration
		private void SRLconfig(int id) {
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			this.SRLenable = GUILayout.Toggle(this.SRLenable,new GUIContent("Enable Simulate, Revert & Launch"));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			GUILayout.Box("Difficulty", GUILayout.Width(470),GUILayout.Height(30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.SRLironman = GUILayout.Toggle(this.SRLironman,"Ironman (hardmode)", GUILayout.Width(235));
			GUILayout.Space(5);
			HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = GUILayout.Toggle(HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels,"Allow Stock Vessels", GUILayout.Width(200));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.SRLsimulate = GUILayout.Toggle(this.SRLsimulate,"Simulate", GUILayout.Width(235));
			GUILayout.Space(5);
			HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn = GUILayout.Toggle(HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn,"Missing Crews Respawn", GUILayout.Width(200));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			/*GUILayout.BeginHorizontal();
			GUILayout.Box("Finance", GUILayout.Width(480),GUILayout.Height(30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.SRLmoney = GUILayout.Toggle(this.SRLmoney,"Simulations will cost money", GUILayout.Width(235));
			GUILayout.Space(5);
			this.SRLreputation = GUILayout.Toggle(this.SRLreputation,"Simulations will cost reputation", GUILayout.Width(200));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.SRLlongsimprice = GUILayout.Toggle(this.SRLlongsimprice,"Cost of a simulation will be a function of the time passed", GUILayout.Width(400));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.SRLearnscience = GUILayout.Toggle(this.SRLearnscience,"When a simulation will go far away on Kerbol you will earn science", GUILayout.Width(400));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			GUILayout.Box("Integrate", GUILayout.Width(480),GUILayout.Height(30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.SRLtechtree = GUILayout.Toggle(this.SRLtechtree,"with Tech tree", GUILayout.Width(235));
			this.SRLtechtree = false;
			this.SRLcontracts = GUILayout.Toggle(this.SRLcontracts,"with Contracts", GUILayout.Width(200));
			this.SRLcontracts = false;
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);*/
			GUILayout.EndVertical();
			if (GUILayout.Button ("Close", GUILayout.Width (470),GUILayout.Height(30))) {
				if (ApplicationLauncher.Ready) {
					this.SRLSim.SetFalse ();
				}
			}
			if (!this.SRLenable) {
				HighLogic.CurrentGame.Parameters.Flight.CanRestart = true;
				HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor = true;
				HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = true;
				HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
				HighLogic.CurrentGame.Parameters.Flight.CanLeaveToTrackingStation = true;
				HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsNear = true;
				HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar = true;
				HighLogic.CurrentGame.Parameters.Flight.CanEVA = true;
				HighLogic.CurrentGame.Parameters.Flight.CanBoard = true;
				HighLogic.CurrentGame.Parameters.Flight.CanAutoSave = true;
				HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = true;
				InputLockManager.RemoveControlLock ("SRLquickload");
				InputLockManager.RemoveControlLock ("SRLquicksave");
				//InputLockManager.RemoveControlLock ("SRLevainput");
				InputLockManager.RemoveControlLock ("SRLvesselswitching");
				this.SRLironman = false;
				this.SRLsimulate = false;
			}
			if (this.SRLironman || this.SRLsimulate) {
				this.SRLenable = true;
			}
			if (!this.SRLironman && !this.SRLsimulate) {
				this.SRLenable = false;
			}
		}

		// Activer la simulation
		private void SRLon() {
			if (ApplicationLauncher.Ready && !this.SRLwindow) {
				if (this.SRLSim.State != RUIToggleButton.ButtonState.TRUE) {
					this.SRLSim.SetTrue ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (FlightGlobals.ActiveVessel.landedAt == "") {
						this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
					}
					FlightDriver.CanRevertToPostInit = true;
					FlightDriver.CanRevertToPrelaunch = true;
					QuickSaveLoad.fetch.AutoSaveOnQuickSave = false;
				}
			}
			InputLockManager.RemoveControlLock ("SRLquicksave");
			InputLockManager.RemoveControlLock ("SRLquickload");
			//InputLockManager.SetControlLock (ControlTypes.EVA_INPUT, "SRLevainput");
			InputLockManager.SetControlLock (ControlTypes.VESSEL_SWITCHING, "SRLvesselswitching");
			HighLogic.CurrentGame.Parameters.Flight.CanRestart = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor = true;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = false;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToTrackingStation = false;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsNear = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar = false;
			HighLogic.CurrentGame.Parameters.Flight.CanEVA = true;
			HighLogic.CurrentGame.Parameters.Flight.CanBoard = true;
			HighLogic.CurrentGame.Parameters.Flight.CanAutoSave = false;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = false;
			print ("SRL"+VERSION+": Simulation ON");
		}

		// Désactiver la simulation
		private void SRLoff() {
			if (ApplicationLauncher.Ready && !this.SRLwindow) {
				if (this.SRLSim.State != RUIToggleButton.ButtonState.FALSE) {
					this.SRLSim.SetFalse ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (FlightGlobals.ActiveVessel.landedAt == "") {
						this.SRLSim.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
					}
					if (this.SRLironman) {
						FlightDriver.CanRevertToPostInit = false;
						FlightDriver.CanRevertToPrelaunch = false;
						QuickSaveLoad.fetch.AutoSaveOnQuickSave = false;
					} else {
						FlightDriver.CanRevertToPostInit = true;
						FlightDriver.CanRevertToPrelaunch = true;
						QuickSaveLoad.fetch.AutoSaveOnQuickSave = true;
					}
				}
			}
			if (this.SRLironman) {
				HighLogic.CurrentGame.Parameters.Flight.CanRestart = false;
				HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor = false;
				HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = false;
				HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = false;
				InputLockManager.SetControlLock (ControlTypes.QUICKSAVE, "SRLquicksave");
				InputLockManager.SetControlLock (ControlTypes.QUICKLOAD, "SRLquickload");
			} else {
				HighLogic.CurrentGame.Parameters.Flight.CanRestart = true;
				HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor = true;
				HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = true;
				HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
				InputLockManager.RemoveControlLock ("SRLquickload");
				InputLockManager.RemoveControlLock ("SRLquicksave");
			}
			//InputLockManager.RemoveControlLock ("SRLevainput");
			InputLockManager.RemoveControlLock ("SRLvesselswitching");
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToTrackingStation = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsNear = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar = true;
			HighLogic.CurrentGame.Parameters.Flight.CanEVA = true;
			HighLogic.CurrentGame.Parameters.Flight.CanBoard = true;
			HighLogic.CurrentGame.Parameters.Flight.CanAutoSave = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = true;
			this.SRLindex = 0;
			print ("SRL"+VERSION+": Simulation OFF");
		}

		// Mettre à jours les variables de simulation et désactiver le bouton de récupération si la fusée est au sol de Kerbin
		public void Update() {
			if (this.SRLenable) {
				if (HighLogic.LoadedSceneIsGame) {
					if (this.SRLironman && HighLogic.CurrentGame.Parameters.Flight.CanRestart == HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter) {
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
									print ("SRL" + VERSION + ": Recovery locked");
								} else if (!isSimulate && this.SRLrecoverybutton.slidingTab.toggleMode == ScreenSafeUISlideTab.ToggleMode.EXTERNAL) {
									this.SRLrecoverybutton = (VesselRecoveryButton)GameObject.FindObjectOfType (typeof(VesselRecoveryButton));
									this.SRLrecoverybutton.slidingTab.toggleMode = ScreenSafeUISlideTab.ToggleMode.HOVER;
									this.SRLrecoverybutton.ssuiButton.Unlock ();
									print ("SRL" + VERSION + ": Recovery unlocked");
								}
							}
						}
					}
				}
			}
		}

		// Afficher l'activation de la simulation et le panneau de configuration
		public void OnGUI() {
			if (this.SRLenable && this.SRLsimulate) {
				if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
					if (isSimulate) {
						GUILayout.BeginArea (new Rect (0, (Screen.height / 10), Screen.width, 80), this.SRLtexts);
						GUILayout.Label (this.SRLtext.Substring (0, (this.SRLindex / 2)), this.SRLtexts);
						GUILayout.EndArea ();
						if (this.SRLindex < (this.SRLtext.Length * 2)) {
							this.SRLindex++;
						}
					}
				}
			}
			if (this.SRLwindow) {
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
					GUI.skin = HighLogic.Skin;
					//GUILayout.Window (554, new Rect ((Screen.width -505), 36, 505, 425), this.SRLconfig, new GUIContent("Simulate, Revert & Launch v"+VERSION), GUILayout.Width(505), GUILayout.Height(425));
					GUILayout.Window (554, new Rect ((Screen.width -505), 36, 505, 220), this.SRLconfig, new GUIContent("Simulate, Revert & Launch v"+VERSION), GUILayout.Width(505), GUILayout.Height(220));
				}
			}
		}
	}
}