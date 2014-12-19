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
using System.IO;
using System.Collections.Generic;
using System.Timers;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KSP;
using UnityEngine;

namespace SRL {

	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class SRL : MonoBehaviour {

		// Initialiser les variables

		// Variables internes
		public const string VERSION = "1.32";

		private static bool isdebug = true;
		private static bool ready = false;

		// Variables des textures et des dossiers
		private Texture Button_texture_sim = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/sim", false);
		private Texture Button_texture_srl = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/srl", false);
		private Texture Button_texture_insim = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/insim", false);
		private Texture2D Loading_Background = (Texture2D)GameDatabase.Instance.GetTexture ("SRL/Textures/loading", false);
		private string Path_settings = KSPUtil.ApplicationRootPath + "GameData/SRL/PluginData/SRL/";
		private string Path_techtree = KSPUtil.ApplicationRootPath + "GameData/SRL/TechTree/";
		private string Path_system = KSPUtil.ApplicationRootPath + "GameData/SRL/System/";

		// Variables du système solaire
		private string DefaultBody;
		private string DefaultRealBody;
		private string[] BodyNames;
		private int[] DefaultAltitude;
		private double[] MinimumAltitude;
		private double[] SimOrbitPrice;
		private double[] SimLandedPrice;
		private string[] SimTechTreeUnlock;
		private Vector3d[] LandedPos;

		// Variables sauvegardées par session
		[KSPField(isPersistant = true)]
		public static bool isSimulate = false;
		[KSPField(isPersistant = true)]
		private static bool orbit;
		[KSPField(isPersistant = true)]
		private static int CelestialBody_tosim;
		[KSPField(isPersistant = true)]
		private static double altitude;

		// Variables divers
		private ApplicationLauncherButton Button;
		private GUIStyle Text_Style_simulate;
		private GUIStyle Text_Style_info;
		private GUIStyle Text_Style_loading;
		private GUIStyle Text_Style_label_system;
		private GUIStyle Text_Style_label_settings;
		private int Index = 0;
		private Rect Window_Rect_settings;
		private bool Window_settings = false;
		private bool Window_info = false;
		private bool Window_simulate = false;
		private bool loading = false;
		private bool last_isSimulate = !isSimulate;
		private double last_time = 0;
		private double Time_FlightReady = 0;
		private bool Loadachievements = false;
		private bool N_plus1 = false;
		private Timer timer = new Timer(10000);
		private Timer sim_timer = new Timer (2000);
		private string[] GUIWhiteList = {
			"GameSkin",
			"MainMenuSkin",
			"KSP window 1",
			"KSP window 2",
			"KSP window 3",
			"KSP window 5",
			"KSP window 7"
		};

		// Variables sauvegardées par parties
		[Persistent]
		private string VERSION_config;
		[Persistent]
		private bool enable;
		[Persistent]
		private bool ironman;
		[Persistent]
		private bool simulate;
		[Persistent]
		private bool Credits;
		[Persistent]
		private bool Sciences;
		[Persistent]
		private bool Reputations;
		[Persistent]
		private int Cost_credits;
		[Persistent]
		private int Cost_reputations;
		[Persistent]
		private int Cost_sciences;
		[Persistent]
		private bool Simulation_fct_duration;
		[Persistent]
		private bool Simulation_fct_reputations;
		[Persistent]
		private bool Simulation_fct_body;
		[Persistent]
		private bool Simulation_fct_vessel;
		[Persistent]
		private bool Simulation_fct_penalties;
		[Persistent]
		private double price_factor_body;
		[Persistent]
		private double price_factor_vessel;
		[Persistent]
		private int N_launch;
		[Persistent]
		private int N_quickload;
		[Persistent]
		private int Simulation_duration;
		[Persistent]
		private bool Unlock_achievements;
		[Persistent]
		private bool Unlock_techtree;
		[Persistent]
		private List<String> Achiev_orbit = new List<String> {};
		[Persistent]
		private List<String> Achiev_land = new List<String> {};
		[Persistent]
		private string SelectSystem = string.Empty;
		[Persistent]
		private string ActiveGUI = HighLogic.Skin.name;


		private bool isUnlocked(CelestialBody body, string situation) {
			if (Unlock_achievements) {
				switch (situation) {
				case "orbit":
					return Achiev_orbit.Contains (body.bodyName);
				case "landed":
					return Achiev_land.Contains (body.bodyName);
				case "all":
					return Achiev_orbit.Contains (body.bodyName) || Achiev_land.Contains (body.bodyName);
				}
			} else if (Unlock_techtree) {
				List<String> _bodys = new List<String> {};
				for (int i = 0; i < System.IO.Directory.GetFiles(Path_techtree).Length; i++) {
					ConfigNode _node = ConfigNode.Load (Path_techtree + "TechTree-" + i + ".cfg");
					string _techreq = _node.GetNode("PART").GetValue("TechRequired");
					if (ResearchAndDevelopment.GetTechnologyState (_techreq) == RDTech.State.Available) {
						_bodys.AddRange(SimTechTreeUnlock [i].Split(' '));
					}
				}
				switch (situation) {
				case "orbit":
					return _bodys.Contains (body.bodyName) || _bodys.Contains (body.bodyName + "_orbit");
				case "landed":
					return _bodys.Contains (body.bodyName) || _bodys.Contains (body.bodyName + "_landed");
				case "all":
					return _bodys.Contains (body.bodyName) || _bodys.Contains (body.bodyName + "_landed") || _bodys.Contains (body.bodyName + "_orbit");
				}
			}
			return false;
		}

		private bool CanSimulate {
			get {
				return enable && simulate;
			}
		}
		private bool isIronman {
			get {
				return enable && ironman;
			}
		}
		private bool isFunded {
			get {
				if (useCredits) {
					if (isPrelaunch) {
						return (Funding.Instance.Funds + Convert.ToInt32 (VesselCost) + CreditsCost) > 0;
					} else {
						return (Funding.Instance.Funds + CreditsCost) > 0;
					}
				} else {
					return true;
				}
			}
		}
		private bool isFunded_sciences {
			get {
				if (useSciences) {
					return (ResearchAndDevelopment.Instance.Science + SciencesCost) > 0;
				} else {
					return true;
				}
			}
		}
		private bool isPrelaunch {
			get {
				if (HighLogic.LoadedSceneIsFlight) {
					if (FlightGlobals.ready) {
						if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH) {
							return true;
						}
					}
				}
				return false;
			}
		}
		private bool useCredits {
			get {
				return Credits && HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
			}
		}
		private bool useReputations {
			get {
				return Reputations && HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
			}
		}
		private bool useSciences {
			get {
				return Sciences && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);
			}
		}
		private bool useTimeCost {
			get {
				return Simulation_fct_duration && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);
			}
		}
		private bool useSimulationCost {
			get {
				return useCredits || useReputations || useSciences;
			}
		}
		private float CreditsCost {
			get {
				if (useCredits) {
					int _N = N_launch;
					if (N_plus1 && Planetarium.GetUniversalTime() > (last_time +10)) {
						_N++;
					}
					double _cost = 0;
					if (Credits) {
						_cost -= Cost_credits * _N + (Cost_credits / 2 * N_quickload);
					}
					if (Simulation_fct_duration) {
						_cost -= Cost_credits / 20 * (Simulation_duration / (GetKerbinTime * 3600));
					}
					if (Simulation_fct_reputations) {
						_cost *= (1 - Reputation.UnitRep / 2);
					}
					if (Simulation_fct_body) {
						if (price_factor_body > 0 && Simulation_duration > 0) {
							_cost *= price_factor_body / Simulation_duration;
						} else {
							if (orbit) {
								_cost *= SimOrbitPrice [CelestialBody_tosim];
							} else {
								_cost *= SimLandedPrice [CelestialBody_tosim];
							}
						}
					}
					if (Simulation_fct_vessel) {
						if (price_factor_vessel > 0 && Simulation_duration > 0) {
							_cost *= price_factor_vessel / Simulation_duration;
						} else if (VesselCost > 0) {
							_cost *= (1 + VesselCost / 100000) ;
						}
					}
					if (Simulation_fct_penalties) {
						_cost *= HighLogic.CurrentGame.Parameters.Career.FundsLossMultiplier;
					}
					return Convert.ToSingle(Math.Round(_cost));
				} else {
					return 0;
				}
			}
		}
		private float ReputationsCost {
			get {
				if (useReputations) {
					int _N = N_launch;
					if (N_plus1 && Planetarium.GetUniversalTime() > (last_time +10)) {
						_N++;
					}
					double _cost = 0;
					if (Reputations) {
						_cost -= Cost_reputations * _N + (Cost_credits / 2 * N_quickload);
					}
					if (Simulation_fct_duration) {
						_cost -= Cost_reputations / 20 * (Simulation_duration / (GetKerbinTime * 3600));
					}
					if (Simulation_fct_reputations) {
						_cost *= (1 - Reputation.UnitRep / 2);
					}
					if (Simulation_fct_body) {
						if (price_factor_body > 0 && Simulation_duration > 0) {
							_cost *= price_factor_body / Simulation_duration;
						} else {
							if (orbit) {
								_cost *= SimOrbitPrice [CelestialBody_tosim];
							} else {
								_cost *= SimLandedPrice [CelestialBody_tosim];
							}
						}
					}
					if (Simulation_fct_vessel) {
						if (price_factor_vessel > 0 && Simulation_duration > 0) {
							_cost *= price_factor_vessel / Simulation_duration;
						} else if (VesselCost > 0) {
							_cost *= (1 + VesselCost / 100000) ;
						}
					}
					if (Simulation_fct_penalties) {
						_cost *= HighLogic.CurrentGame.Parameters.Career.RepLossMultiplier;
					}
					return Convert.ToSingle(Math.Round(_cost));
				} else {
					return 0;
				}
			}
		}
		private float SciencesCost {
			get {
				if (useSciences) {
					int _N = N_launch;
					if (N_plus1 && Planetarium.GetUniversalTime() > (last_time +10)) {
						_N++;
					}
					double _cost = 0;
					if (Sciences) {
						_cost -= Cost_sciences * _N + (Cost_sciences / 2 * N_quickload);
					}
					if (Simulation_fct_duration) {
						_cost -= Cost_sciences / 20 * (Simulation_duration / (GetKerbinTime * 3600));
					}
					if (Simulation_fct_reputations) {
						_cost *= (1 - Reputation.UnitRep / 2);
					}
					if (Simulation_fct_body) {
						if (price_factor_body > 0 && Simulation_duration > 0) {
							_cost *= price_factor_body / Simulation_duration;
						} else {
							if (orbit) {
								_cost *= SimOrbitPrice [CelestialBody_tosim];
							} else {
								_cost *= SimLandedPrice [CelestialBody_tosim];
							}
						}
					}
					if (Simulation_fct_vessel) {
						if (price_factor_vessel > 0 && Simulation_duration > 0) {
							_cost *= price_factor_vessel / Simulation_duration;
						} else if (VesselCost > 0) {
							_cost *= (1 + VesselCost / 100000) ;
						}
					}
					return Convert.ToSingle(Math.Round(_cost));
				} else {
					return 0;
				}
			}
		}
		private float VesselCost {
			get {
				float _dryCost = 0, _fuelCost = 0, _VesselCost = 0;
				if (HighLogic.LoadedSceneIsEditor) {
					EditorLogic.fetch.ship.GetShipCosts (out _dryCost, out _fuelCost);
					return _dryCost + _fuelCost;
				} else if (HighLogic.LoadedSceneIsFlight) {
					if (FlightGlobals.ready) {
						List<ProtoPartSnapshot> _parts = FlightGlobals.ActiveVessel.protoVessel.protoPartSnapshots;
						foreach (ProtoPartSnapshot _part in _parts) {
							ShipConstruction.GetPartCosts (_part, _part.partInfo, out _dryCost, out _fuelCost);
							_VesselCost += _dryCost + _fuelCost;
						}
					}
					return _VesselCost;
				} 
				return 0;
			}
		}
		private bool Button_isTrue {
			get {
				return Button.State == RUIToggleButton.ButtonState.TRUE;
			}
		}
		private bool Button_isFalse {
			get {
				return Button.State == RUIToggleButton.ButtonState.FALSE;
			}
		}
		private int GetKerbinTime {
			get {
				if (GameSettings.KERBIN_TIME) {
					return 6;
				} else {
					return 24;
				}
			}
		}
		private CelestialBody body (ScienceSubject subject) {
			List<CelestialBody> _bodies = FlightGlobals.Bodies;
			foreach (CelestialBody _body in _bodies) {
				if (subject.IsFromBody(_body)) {
					return _body;
				}
			}
			return null;
		}
		private string Realbody (CelestialBody body) {
			int _i = FlightGlobals.Bodies.FindIndex(b => b == body);
			return BodyNames[_i];
		}
		private string TabClean (string str) {
			return Regex.Replace(str.Trim().Replace("\t"," "),"[ ]+"," ");
		}

		// Préparer les variables et les évènements
		private void Awake() {
			GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Add (OnLaunch);
			GameEvents.onFlightReady.Add (OnFlightReady);
			GameEvents.onCrewOnEva.Add (OnCrewOnEva);
			//GameEvents.onGameStateLoad.Add (OnGameStateLoad);
			GameEvents.onLevelWasLoaded.Add (OnLevelWasLoaded);
			GameEvents.onVesselGoOffRails.Add (OnVesselGoOffRails);
			GameEvents.onVesselSOIChanged.Add (OnVesselSOIChanged);
			GameEvents.onVesselSituationChange.Add (OnVesselSituationChange);

			Window_Rect_settings = new Rect ((Screen.width - 515), 40, 515, 0);

			Text_Style_simulate = new GUIStyle ();
			Text_Style_simulate.stretchWidth = true;
			Text_Style_simulate.stretchHeight = true;
			Text_Style_simulate.alignment = TextAnchor.UpperCenter;
			Text_Style_simulate.fontSize = (Screen.height/20);
			Text_Style_simulate.fontStyle = FontStyle.Bold;
			Text_Style_simulate.normal.textColor = Color.red;

			Text_Style_info = new GUIStyle ();
			Text_Style_info.stretchWidth = true;
			Text_Style_info.stretchHeight = true;
			Text_Style_info.wordWrap = true;
			Text_Style_info.alignment = TextAnchor.MiddleLeft;

			Text_Style_loading = new GUIStyle ();
			Text_Style_loading.stretchWidth = true;
			Text_Style_loading.stretchHeight = true;
			Text_Style_loading.alignment = TextAnchor.MiddleCenter;
			Text_Style_loading.fontSize = (Screen.height/20);
			Text_Style_loading.fontStyle = FontStyle.Bold;
			Text_Style_loading.normal.textColor = Color.red;
			Text_Style_loading.normal.background = Loading_Background;

			Text_Style_label_system = new GUIStyle ();
			Text_Style_label_system.stretchWidth = true;
			Text_Style_label_system.stretchHeight = true;
			Text_Style_label_system.alignment = TextAnchor.MiddleCenter;
			Text_Style_label_system.fontStyle = FontStyle.Bold;
			Text_Style_label_system.normal.textColor = AssetBase.GetGUISkin(ActiveGUI).toggle.normal.textColor;

			Text_Style_label_settings = new GUIStyle ();
			Text_Style_label_settings.stretchWidth = true;
			Text_Style_label_settings.stretchHeight = false;
			Text_Style_label_settings.alignment = TextAnchor.UpperLeft;
			Text_Style_label_settings.fontStyle = FontStyle.Normal;
			Text_Style_label_settings.normal.textColor = AssetBase.GetGUISkin(ActiveGUI).toggle.normal.textColor;

			timer.Elapsed += new ElapsedEventHandler(OnTimer);
			sim_timer.Elapsed += new ElapsedEventHandler(OnSim_Timer);
		}

		// Supprimer le bouton de simulation et les évènements
		private void OnDestroy() {
			GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Remove (OnLaunch);
			GameEvents.onFlightReady.Remove (OnFlightReady);
			GameEvents.onCrewOnEva.Remove (OnCrewOnEva);
			//GameEvents.onGameStateLoad.Remove (OnGameStateLoad);
			GameEvents.onLevelWasLoaded.Remove (OnLevelWasLoaded);
			GameEvents.onVesselGoOffRails.Remove (OnVesselGoOffRails);
			GameEvents.onVesselSOIChanged.Remove (OnVesselSOIChanged);
			GameEvents.onVesselSituationChange.Remove (OnVesselSituationChange);
			if (Button != null) {
				ApplicationLauncher.Instance.RemoveModApplication (Button);
				Button = null;
			}
		}

		// Afficher le bouton de simulation
		private void OnGUIApplicationLauncherReady() {
			if (ApplicationLauncher.Ready) {
				Button = ApplicationLauncher.Instance.AddModApplication (Button_On, Button_Off, Button_OnHover, Button_OnHoverOut, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.MAPVIEW, Button_texture_srl);
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION) {
					isSimulate = false;
					CelestialBody_tosim = Array.FindIndex (BodyNames, item => item == DefaultBody);
					altitude = DefaultAltitude[CelestialBody_tosim];
					orbit = false;
				}
				if (!CanSimulate) {
					Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER;
				} else if (HighLogic.LoadedSceneIsEditor) {
					Button.SetTexture(Button_texture_sim);
				}
			}
		}

		// Charger la simulation.
		private void OnLevelWasLoaded(GameScenes gamescenes) {
			ready = true;
			if (HighLogic.LoadedSceneIsGame) {
				Load ();
				if (CanSimulate) {
					if (isSimulate) {
						if (HighLogic.LoadedSceneIsFlight) {
							if (orbit || BodyNames [CelestialBody_tosim] != DefaultBody) {
								if (QuickRevert_fct.Save_Vessel_Guid == Guid.Empty) {
									loading = true;
									new UI_Toggle ();
									InputLockManager.SetControlLock (ControlTypes.All, "SRLall");
								}
								HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = false;
								HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = false;
								Debug ("Loading simulation ...");
							} 
						}
					} else {
						sim_timer.Enabled = false;
						loading = false;
						InputLockManager.RemoveControlLock ("SRLall");
					}
					if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
						N_plus1 = true;
					}
				}
			}
		}

		private void OnVesselGoOffRails (Vessel vessel) {
			Vessel _vessel = FlightGlobals.ActiveVessel;
			if (CanSimulate && isSimulate && isPrelaunch && vessel == _vessel) {
				CelestialBody _body = FlightGlobals.Bodies [CelestialBody_tosim];
				if (orbit) {
					launch ();
					Vector3d _sun_pos = FlightGlobals.Bodies [0].position;
					// HyperEdit functions
					Orbit _orbit;
					Orbit _orbit1 = HyperEdit_fct.CreateOrbit (0, 0, altitude * 1000 + _body.Radius, 0, 0, 0, 0, _body);
					Orbit _orbit2 = HyperEdit_fct.CreateOrbit (0, 0, altitude * 1000 + _body.Radius, 180, 0, 0, 0, _body);
					double _distance1 = Vector3d.Distance (_sun_pos, _orbit1.getPositionAtUT(Planetarium.GetUniversalTime()));
					double _distance2 = Vector3d.Distance (_sun_pos, _orbit2.getPositionAtUT(Planetarium.GetUniversalTime()));
					if (_distance1 < _distance2) {
						_orbit = _orbit1;
					} else {
						_orbit = _orbit2;
					}
					HyperEdit_fct.Set (_vessel.orbitDriver.orbit, _orbit);
					//
					sim_timer.Enabled = true;
					Debug("Simulate to orbit of " + _body.bodyName);
				} else if (_body.bodyName != DefaultRealBody) {
					launch ();
					//VesselTeleport (_vessel, _body);
					Debug("Simulate landed on " + _body.bodyName);
				} else {
					Debug("Simulate landed on " + _body.bodyName);
				}
			}
		}

		// Téléporter la fusée
		// Doesn't work as i want, can't choose the position of a launch, that always the position of the launchpad.
		private void VesselTeleport (Vessel vessel, CelestialBody body) {
			Vector3d _land_pos;
			Vector3d _land_pos1 = LandedPos [CelestialBody_tosim * 2];
			Vector3d _land_pos2 = LandedPos [CelestialBody_tosim * 2+1];
			Vector3d _sun_pos = FlightGlobals.Bodies [0].position;
			//Vector3d _body_pos = body.getPositionAtUT(Planetarium.GetUniversalTime());
			double _distance1 = Vector3d.Distance (_sun_pos, body.GetRelSurfacePosition (_land_pos1.x, _land_pos1.y, _land_pos1.z));
			double _distance2 = Vector3d.Distance (_sun_pos, body.GetRelSurfacePosition (_land_pos2.x, _land_pos2.y, _land_pos2.z));
			if (_distance1 > _distance2) {
				_land_pos = _land_pos1;
			} else {
				_land_pos = _land_pos2;
			}
			Vector3d _vessel_pos = body.GetWorldSurfacePosition (_land_pos.x, _land_pos.y,_land_pos.z);
			vessel.GoOnRails();
			vessel.transform.position = _vessel_pos;
			vessel.GoOffRails();
			vessel.ChangeWorldVelocity(-vessel.obt_velocity);
			vessel.landedAt = string.Empty;
			sim_timer.Enabled = true;
		}

		// Activer le revert après un quickload
		private void OnFlightReady() {
			if (CanSimulate) {
				Time_FlightReady = Planetarium.GetUniversalTime ();
				if (isPrelaunch) {
					if (ApplicationLauncher.Ready) {
						if (N_plus1) {
							Button.SetTexture (Button_texture_sim);
						} else {
							Button.SetTexture (Button_texture_insim);
						}
					}
					HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = false;
				} else if (isSimulate) {
					if (QuickRevert_fct.Save_Vessel_Guid == FlightGlobals.ActiveVessel.id) {
						Button.SetTexture (Button_texture_insim);
						N_quickload++;
						N_plus1 = false;
						Save ();
						HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
					} else {
						N_plus1 = false;
					}
				}
				if (isSimulate) {
					if (System.IO.File.Exists (KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs")) {
						if (GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, true, false).UniversalTime > QuickRevert_fct.Save_PreLaunchState.UniversalTime) {
							GamePersistence.SaveGame (FlightDriver.PreLaunchState, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
							Debug("Keep the save");
						}
					}
				}
			}
		}

		// Bloquer l'accès au bouton de simulation après le lancement de la fusée
		private void OnLaunch(EventReport EventReport) {
			if (CanSimulate) {
				if (isSimulate) {
					if (FlightGlobals.ActiveVessel.mainBody.bodyName == DefaultRealBody && FlightGlobals.ActiveVessel.situation != Vessel.Situations.ORBITING) {
						CelestialBody_tosim = Array.FindIndex (BodyNames, item => item == DefaultBody);
						altitude = DefaultAltitude[CelestialBody_tosim];
						orbit = false;
					}
					launch ();
				} else {
					if (ApplicationLauncher.Ready) {
						Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
						N_plus1 = false;
					}
				}				
			}
		}
			
		// Modifier les variables après un lancement
		private void launch() {
			if (ApplicationLauncher.Ready) {
				Button.SetTexture (Button_texture_insim);
				if (Button_isTrue) {
					Button.SetFalse ();
					isSimulate = true;
				}
			}
			if (useCredits) {
				Funding.Instance.AddFunds (Convert.ToInt32 (VesselCost), TransactionReasons.Vessels);
			}
			last_time = Planetarium.GetUniversalTime ();
			if (N_plus1) {
				N_launch++;
			}
			N_plus1 = false;
			Time_FlightReady = 0;
			Save ();
			HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
			Debug("Launch");
		}

		// Supprimer le bouton de simulation à l'EVA
		private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> part) {
			if (CanSimulate && part.from.vessel.situation == Vessel.Situations.PRELAUNCH && ApplicationLauncher.Ready) {
				Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
				isSimulate = false;
			}
		}

		// Ajouter les achievements lorsque l'on change d'orbite
		private void OnVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> vessel) {
			CheckAchievements (vessel.host);
		}

		// Ajouter les achievements lorsque l'on se pose
		private void OnVesselSituationChange (GameEvents.HostedFromToAction<Vessel, Vessel.Situations> vessel) {
			CheckAchievements (vessel.host);
		}

		// Effacer certains messages temporaires
		private void OnTimer(object sender, ElapsedEventArgs e) {
			timer.Enabled = false;
			Index = 0;
			Debug("Message off");
		}

		// Enlever l'écran de chargement
		private void OnSim_Timer(object sender, ElapsedEventArgs e) {
			sim_timer.Enabled = false;
			loading = false;
			new UI_Toggle ();
			InputLockManager.RemoveControlLock ("SRLall");
		}

		// Activer le bouton
		private void Button_On() {
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				Window_settings = true;
				InputLockManager.SetControlLock (ControlTypes.KSC_FACILITIES, "SRLkscfacilities");
			} else if (HighLogic.LoadedSceneIsFlight) {
				if (isSimulate && !N_plus1 && isFunded && isFunded_sciences) {
					Window_info = true;
				} else {
					isSimulate = true;
				}
			} else if (HighLogic.LoadedSceneIsEditor) {
				Window_simulate = true;
				Window_info = true;
				isSimulate = true;
				if (EditorLogic.fetch.launchBtn.controlIsEnabled) {
					InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK, "SRLeditor");
				}
			}
		}

		// Désactiver le bouton
		private void Button_Off() {
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				Window_settings = false;
				InputLockManager.RemoveControlLock("SRLkscfacilities");
				Save ();
				LoadSystem ();
			} else {
				if (isSimulate && !N_plus1 && HighLogic.LoadedSceneIsFlight) {
					Window_info = false;
				} else {
					Window_simulate = false;
					Window_info = false;
					isSimulate = false;
					InputLockManager.RemoveControlLock ("SRLeditor");
				}
			}
		}

		// Passer la souris sur le bouton
		private void Button_OnHover() {
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor) {
				if (isSimulate && isFunded && isFunded_sciences) {
					Window_info = true;
					if (HighLogic.LoadedSceneIsEditor) {
						Window_simulate = true;
						if (EditorLogic.fetch.launchBtn.controlIsEnabled) {
							InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK, "SRLeditor");
						}
					}
				}
			}
		}

		// Enlever la souris du bouton
		private void Button_OnHoverOut() {
			if (((isSimulate && Button_isFalse) || N_plus1) && HighLogic.LoadedSceneIsFlight) {
				Window_info = false;
			}
		}				

		// Activer la simulation
		private void Simulation_on() {
			if (ApplicationLauncher.Ready) {
				if ((HighLogic.LoadedSceneIsEditor || isPrelaunch) && Button_isFalse) {
					Button.SetTrue ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (!isPrelaunch) {
						Button.SetTexture (Button_texture_insim);
					}
					FlightGlobals.ActiveVessel.isPersistent = false;
					FlightDriver.CanRevertToPostInit = true;
					FlightDriver.CanRevertToPrelaunch = true;
					QuickSaveLoad.fetch.AutoSaveOnQuickSave = false;
					FlightDriver.fetch.bypassPersistence = true;
				}
				if (HighLogic.LoadedSceneIsEditor) {
					N_plus1 = true;
				}
			}
			InputLockManager.RemoveControlLock ("SRLquicksave");
			InputLockManager.RemoveControlLock ("SRLquickload");
			InputLockManager.SetControlLock (ControlTypes.VESSEL_SWITCHING, "SRLvesselswitching");
			HighLogic.CurrentGame.Parameters.Flight.CanRestart = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor = true;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = false;
			HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = false;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToTrackingStation = false;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsNear = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar = true;
			HighLogic.CurrentGame.Parameters.Flight.CanEVA = true;
			HighLogic.CurrentGame.Parameters.Flight.CanBoard = true;
			HighLogic.CurrentGame.Parameters.Flight.CanAutoSave = false;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = false;
			Debug("Simulation ON");
		}

		// Désactiver la simulation
		private void Simulation_off() {
			if (ApplicationLauncher.Ready) {
				if (Button_isTrue && !Window_settings) {
					Button.SetFalse ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (!isPrelaunch) {
						Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
						N_plus1 = false;
					}
					if (isIronman) {
						FlightDriver.CanRevertToPostInit = false;
						FlightDriver.CanRevertToPrelaunch = false;
						QuickSaveLoad.fetch.AutoSaveOnQuickSave = false;
						FlightGlobals.ActiveVessel.isPersistent = true;
						FlightDriver.fetch.bypassPersistence = false;
					} else {
						FlightGlobals.ActiveVessel.isPersistent = true;
						FlightDriver.CanRevertToPostInit = true;
						FlightDriver.CanRevertToPrelaunch = true;
						QuickSaveLoad.fetch.AutoSaveOnQuickSave = true;
						FlightDriver.fetch.bypassPersistence = false;
					}
				}
			}
			if (isIronman) {
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
			InputLockManager.RemoveControlLock ("SRLvesselswitching");
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToTrackingStation = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsNear = true;
			HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar = true;
			HighLogic.CurrentGame.Parameters.Flight.CanEVA = true;
			HighLogic.CurrentGame.Parameters.Flight.CanBoard = true;
			HighLogic.CurrentGame.Parameters.Flight.CanAutoSave = true;
			HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = true;
			Simulation_pay ();
			Debug("Simulation OFF");
		}

		// Payer la simulation
		private void Simulation_pay() {
			if (CanSimulate && !isSimulate && N_launch >= 1 && !HighLogic.LoadedSceneIsFlight && MessageSystem.Ready) {
				if (useSimulationCost) {
					if (N_plus1) {
						N_plus1 = false;
					}
					string _string;
					_string = string.Format ("<b>In simulation mode, you did:\nmake <#8BED8B>{0}</> launch(s),\nuse <#8BED8B>{1}</> quickload(s),\nspend <#8BED8B>{2}</> day(s).</>\n\nThe simulations have cost:", N_launch, N_quickload, Convert.ToInt32 (Simulation_duration / (GetKerbinTime * 3600)));
					float _float;
					if (useCredits) {
						_float = CreditsCost;
						_string += " <#B4D455>£" + _float + "</>";
						Funding.Instance.AddFunds (_float, TransactionReasons.Vessels);
						if (useReputations && useSciences) {
							_string += ", ";
						} else if (useReputations || useSciences) {
							_string += " and";
						}
					}
					if (useReputations) {
						_float = ReputationsCost;
						_string += " <#E0D503>¡" + _float + "</>";
						Reputation.Instance.AddReputation (_float, TransactionReasons.Vessels);
						if (useSciences) {
							_string += " and";
						}
					}
					if (useSciences) {
							_float = SciencesCost;
						_string += " <#6DCFF6>" + _float + "</>";
						ResearchAndDevelopment.Instance.AddScience (_float, TransactionReasons.Vessels);
					}
					_string += ".";
					MessageSystem.Instance.AddMessage (new MessageSystem.Message ("Simulation ended", _string, MessageSystemButton.MessageButtonColor.ORANGE, MessageSystemButton.ButtonIcons.ALERT));
				}
				if (HighLogic.LoadedSceneIsEditor) {
					N_plus1 = true;
				}
				N_launch = 0;
				N_quickload = 0;
				Simulation_duration = 0;
				price_factor_body = 0;
				price_factor_vessel = 0;
				Save ();
				Debug("Simulation paid");
			}
		}

		// Mettre à jours les variables de simulation et désactiver le bouton de récupération si la fusée est au sol de Kerbin
		public void Update() {
			if (enable) {
				if (ready && HighLogic.LoadedSceneIsGame) {
					if (Loadachievements && ResearchAndDevelopment.Instance) {
						LoadAchievements ();
					}
					if (isIronman && HighLogic.CurrentGame.Parameters.Flight.CanRestart == HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter) {
						last_isSimulate = !isSimulate;
					}
					if (ApplicationLauncher.Ready) {
						if (last_isSimulate != isSimulate) {
							if (isSimulate) {
								Simulation_on ();
							} else {
								Simulation_off ();
							}
							last_isSimulate = isSimulate;
						}
						if (HighLogic.LoadedSceneIsFlight) {
							if (FlightGlobals.ready) {
								Vessel _vessel = FlightGlobals.ActiveVessel;
								if (!isSimulate) {
									CheckAchievements (_vessel);
									if (Planetarium.GetUniversalTime() >= Time_FlightReady+5 && Time_FlightReady > 0 && N_plus1 && _vessel.srfSpeed >= 0.1) {
										Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
										N_plus1 = false;
										Time_FlightReady = 0;
									}
								} else {
									if (Planetarium.GetUniversalTime() >= Time_FlightReady+5 && Time_FlightReady > 0 && N_plus1 && _vessel.srfSpeed >= 0.1) {
										launch ();
									}
									if (GameSettings.QUICKSAVE.GetKeyDown () && !HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad) {
										HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = true;
										Debug("Quickload ON");
									}
									if (!isPrelaunch) {
										double _double;
										if (last_time == 0) {
											last_time = Planetarium.GetUniversalTime ();
										}
										_double = Planetarium.GetUniversalTime () - last_time;
										if (_double > 60) {
											Simulation_duration += Convert.ToInt32 (_double);
											if (Simulation_fct_vessel) {
												price_factor_vessel += (1 + VesselCost / 100000) * _double;
											}
											if (Simulation_fct_body) {
												if (orbit) {
													price_factor_body += SimOrbitPrice [CelestialBody_tosim] * _double;
												} else {
													price_factor_body += SimLandedPrice [CelestialBody_tosim] * _double;
												}
											}
											Save ();
											last_time = Planetarium.GetUniversalTime ();
										}
										if (isSimulate && useTimeCost && (!isFunded || !isFunded_sciences)) {
											Window_info = true;
										}
									}
								}
								if (_vessel.LandedOrSplashed && _vessel.mainBody.bodyName == DefaultRealBody) {
									AltimeterSliderButtons _Recovery_button = (AltimeterSliderButtons)GameObject.FindObjectOfType (typeof(AltimeterSliderButtons));
									if (isSimulate && _Recovery_button.slidingTab.enabled) {
										_Recovery_button.slidingTab.enabled = false;
										Debug("Recovery locked");
									} else if (!isSimulate && !_Recovery_button.slidingTab.enabled) {
										_Recovery_button.slidingTab.enabled = true;
										Debug("Recovery unlocked");
									}
								}
							}	
						} else {
							Simulation_pay ();
						}
					}
				}
			}
		}

		// Afficher l'activation de la simulation, le panneau d'information et le panneau de configuration
		public void OnGUI() {
			if (CanSimulate) {
				if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
					if (loading) {
						GUILayout.BeginArea (new Rect (0, 0, Screen.width, Screen.height), Text_Style_loading);
						GUILayout.Label ("LOADING ...", Text_Style_loading);
						GUILayout.EndArea ();
					}
					if (isSimulate || Index > 0) {
						string _message = "";
						if (isFunded && isFunded_sciences) {
							_message = "SIMULATION";
						} else {
							if (!isFunded) {
								_message = "You need more credits to make a simulation";
							} else {
								_message = "You need more science to make a simulation";
							}
							timer.Enabled = true;
							isSimulate = false;
							Index = _message.Length * 2;
						}
						int _int;
						if (HighLogic.LoadedSceneIsEditor) {
							_int = 255;
						} else {
							_int = 0;
						}
						GUILayout.BeginArea (new Rect (_int, (Screen.height / 10), Screen.width - _int, 160), Text_Style_simulate);
						GUILayout.Label (_message.Substring (0, (Index / 2)), Text_Style_simulate);
						GUILayout.EndArea ();
						if (!isSimulate && isFunded && isFunded_sciences) {
							Index--;
						} 
						if ((isSimulate && Index < (_message.Length * 2))) {
							Index++;
						}
					}
					if (Window_info) {
						int _height, _width, _guiheight, _guiwidth, _i;
						if (useSimulationCost) {
							if (HighLogic.LoadedSceneIsEditor) {
								_guiheight = 230;
								_guiwidth = 250;
								_height = Screen.height - (_guiheight + 40);
								_width = Screen.width - (_guiwidth + 70);
							} else {
								if (!isPrelaunch && HighLogic.LoadedSceneIsFlight && useTimeCost && (!isFunded || !isFunded_sciences)) {
									_guiheight = 300;
									_guiwidth = 300;
									_height = (Screen.height - _guiheight) / 2;
									_width = (Screen.width - _guiwidth) / 2;
								} else {
									_guiheight = 230;
									_guiwidth = 250;
									_height = 40;
									_width = Screen.width - _guiwidth;
								}
							}
						} else {
							_guiheight = 150;
							_guiwidth = 250;
							if (HighLogic.LoadedSceneIsEditor) {
								_height = Screen.height - (_guiheight + 40);
								_width = Screen.width - (_guiwidth + 70);
							} else {
								_height = 40;
								_width = Screen.width - _guiwidth;
							}
						}
						if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) {
							_i = 125;
						} else {
							_i = 165;
						}
						if (HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsFlight && (Mouse.screenPos.x < Screen.width - _i || Mouse.screenPos.y > 40))) {
							GUI.skin = AssetBase.GetGUISkin(ActiveGUI);
							GUILayout.Window (554, new Rect (_width, _height, _guiwidth, _guiheight), DrawInfo, "Simulate, Revert & Launch", GUILayout.Width(_guiwidth), GUILayout.Height(_guiheight));
						}
					}
					if (Window_simulate) {
						if (HighLogic.LoadedSceneIsEditor) {
							if (EditorLogic.fetch.launchBtn.controlIsEnabled) {
								int _height, _width, _guiheight, _guiwidth, _guiinfo_height, _i;
								if (useSimulationCost) {
									_guiinfo_height = 230;
								} else {
									_guiinfo_height = 150;
								}
								_guiheight = 140;
								_guiwidth = 250;
								_height = Screen.height - (_guiheight + 40 + _guiinfo_height + 5);
								_width = Screen.width - (_guiwidth + 70);
								if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) {
									_i = 115;
								} else {
									_i = 155;
								}
								if (Mouse.screenPos.x > _width - Screen.width / 10 && Mouse.screenPos.y > _height - Screen.height / 10 && (Mouse.screenPos.x < Screen.width - _i || Mouse.screenPos.y < Screen.height - 40)) {
									GUI.skin = AssetBase.GetGUISkin(ActiveGUI);
									GUILayout.Window (555, new Rect (_width, _height, _guiwidth, _guiheight), DrawSim, "Select the body to simulate:", GUILayout.Width (_guiwidth), GUILayout.ExpandHeight(true));
								} else {
									InputLockManager.RemoveControlLock ("SRLeditor");
									Window_simulate = false;
									Window_info = false;
								}
							}
						}
					}
				}
			}
			if (Window_settings) {
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
					if (Button_isFalse) {
						Button.SetTrue ();
					}
					GUI.skin = AssetBase.GetGUISkin(ActiveGUI);
					Window_Rect_settings = GUILayout.Window (554, Window_Rect_settings, DrawSettings, "Simulate, Revert & Launch v" + VERSION, GUILayout.Width (Window_Rect_settings.width), GUILayout.ExpandHeight(true));
				}
			}
		}

		// Fenêtre pour choisir le lieu de la simulation
		private void DrawSim(int id) {
			string _tmp;
			CelestialBody _body = FlightGlobals.Bodies [CelestialBody_tosim];
			Vector2 _vector2 = new Vector2();
			GUILayout.BeginVertical ();
			GUILayout.BeginScrollView (_vector2, false, false);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("◄", GUILayout.Width(20))) {
				CelestialBody_tosim--;
				if (CelestialBody_tosim <= -1) {
					CelestialBody_tosim = BodyNames.Length - 1;
				}
				_body = FlightGlobals.Bodies [CelestialBody_tosim];
				if (Unlock_achievements || Unlock_techtree) {
					while (!isUnlocked(_body,"all")) {
						CelestialBody_tosim--;
						if (CelestialBody_tosim <= -1) {
							CelestialBody_tosim = BodyNames.Length - 1;
						}
						_body = FlightGlobals.Bodies [CelestialBody_tosim];
					}
					if (!isUnlocked(_body,"orbit") && orbit) {
						orbit = false;
					} 
					if (!isUnlocked(_body,"landed") && !orbit) {
						orbit = true;
					} 
				}
				if (_body.atmosphere) {
					double _atm_alt = Math.Round (_body.atmosphereScaleHeight * 1000 * Math.Log (1e6) / 10000)*10;
					if (DefaultAltitude[CelestialBody_tosim] < _atm_alt && MinimumAltitude[CelestialBody_tosim] == -1) {
						altitude = _atm_alt;
						Debug("ERROR IN THE DEFAULT ALTITUDE OF " + _body.bodyName + ".");
					} else {
						altitude = DefaultAltitude [CelestialBody_tosim];
					}
				} else {
					altitude = DefaultAltitude [CelestialBody_tosim];
				}
			}
			if (GUILayout.Button (BodyNames[CelestialBody_tosim])) {
				EditorLogic.fetch.launchVessel ();
			}
			if (GUILayout.Button ("►", GUILayout.Width(20))) {
				CelestialBody_tosim++;
				if (CelestialBody_tosim >= BodyNames.Length) {
					CelestialBody_tosim = 0;
				}
				_body = FlightGlobals.Bodies [CelestialBody_tosim];
				if (Unlock_achievements || Unlock_techtree) {
					while (!isUnlocked(_body,"all")) {
						CelestialBody_tosim++;
						if (CelestialBody_tosim >= BodyNames.Length) {
							CelestialBody_tosim = 0;
						}
						_body = FlightGlobals.Bodies [CelestialBody_tosim];
					}
					if (!isUnlocked(_body,"orbit") && orbit) {
						orbit = false;
					} 
					if (!isUnlocked(_body,"landed") && !orbit) {
						orbit = true;
					} 
				}
				if (_body.atmosphere) {
					double _atm_alt = Math.Round (_body.atmosphereScaleHeight * 1000 * Math.Log (1e6) / 10000)*10;
					if (DefaultAltitude[CelestialBody_tosim] < _atm_alt && MinimumAltitude[CelestialBody_tosim] == -1) {
						altitude = _atm_alt;
					} else {
						altitude = DefaultAltitude [CelestialBody_tosim];
					}
				} else {
					altitude = DefaultAltitude [CelestialBody_tosim];
				}
			}
			GUILayout.EndHorizontal ();
			if ((!Unlock_achievements && !Unlock_techtree) || isUnlocked(_body,"orbit")) {
				GUILayout.BeginHorizontal (); 
				orbit = GUILayout.Toggle (orbit, "In orbit", GUILayout.Width (90));
				GUILayout.Space (5);
				double _atm_alt = Math.Round (_body.atmosphereScaleHeight * 1000 * Math.Log (1e6) / 10000)*10;
				if (altitude < _atm_alt && MinimumAltitude [CelestialBody_tosim] == -1 && _body.atmosphere) {
					altitude = _atm_alt;
				} else if (MinimumAltitude [CelestialBody_tosim] == -1 && !_body.atmosphere) {
					altitude = DefaultAltitude [CelestialBody_tosim];
					Debug("ERROR IN THE MINIMUM ALTITUDE OF " + _body.bodyName);
				} else if (altitude < MinimumAltitude [CelestialBody_tosim]) {
					altitude = MinimumAltitude [CelestialBody_tosim];
				} else if (altitude > Math.Round (((_body.sphereOfInfluence - _body.Radius) / 1000) - 1)) {
					altitude = Math.Round (((_body.sphereOfInfluence - _body.Radius) / 1000) - 1);
				}
				_tmp = GUILayout.TextField (altitude.ToString ());
				try {
					altitude = Convert.ToDouble (_tmp);
				} catch {
					if (_tmp == null) {
						altitude = MinimumAltitude [CelestialBody_tosim];
					} else {
						altitude = DefaultAltitude [CelestialBody_tosim];
					}
				}
				GUILayout.Space (5);
				GUILayout.Label ("km");
				GUILayout.EndHorizontal ();
			} else {
				orbit = false;
			}
			if ((!Unlock_achievements && !Unlock_techtree) || isUnlocked(_body,"landed")) {
				GUILayout.BeginHorizontal ();
				if (LandedPos [CelestialBody_tosim*2] != new Vector3d (-1, -1, -1)) {
					orbit = !GUILayout.Toggle (!orbit, "Landed");
				} else {
					orbit = true;
				}
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();
			GUILayout.Space(5);
			GUILayout.EndVertical ();
		}

		// Fenètre d'information de la simulation
		private void DrawInfo(int id) {
			string _string, _string2;
			int _N = N_launch;
			if (N_plus1) {
				_N++;
			}
			GUILayout.BeginVertical ();
			Vector2 _vector2 = new Vector2();
			if (useCredits || useSciences) {
				if (!isFunded || !isFunded_sciences) {
					GUILayout.BeginScrollView (_vector2, false, false);
					_string = "<color=#FF0000><b>You have lost all your ";
					if (!isFunded) {
						_string += "credits";
						if (!isFunded_sciences) {
							_string += "and ";
						}
					} 
					if (!isFunded_sciences) {
						_string += "science";
					}
					_string += ", you can't continue this simulation</b></color>";
					GUILayout.Label (_string,GUILayout.Height(50));
					GUILayout.EndScrollView ();
					GUILayout.Space(5);
				}
			}
			GUILayout.BeginScrollView(_vector2, false, false);
			if (isSimulate && !isPrelaunch && HighLogic.LoadedSceneIsFlight && useTimeCost && (!isFunded || !isFunded_sciences)) {
				_string2 = "<color=#FFFFFF>In simulation mode, you did:</color>";
				_string = "The simulations have cost:";
			} else {
				_string2 = "<color=#FFFFFF>In simulation mode, you will:</color>";
				_string = "<color=#FFFFFF>The simulations will cost:</color>";
			}
			_string2 += "\n<color=#FFFFFF>make </color><color=#8BED8B><b>" + _N + "</b></color><color=#FFFFFF> launch(s),</color>";
			_string2 += "\n<color=#FFFFFF>use </color><color=#8BED8B><b>" + N_quickload + "</b></color><color=#FFFFFF> quickload(s),</color>";
			_string2 += "\n<color=#FFFFFF>spend </color><color=#8BED8B><b>" + Convert.ToInt32(Simulation_duration / (GetKerbinTime * 3600)) + "</b></color><color=#FFFFFF> day(s).</color>";
			GUILayout.Label (_string2, Text_Style_info,GUILayout.Height(80));
			GUILayout.EndScrollView ();
			if (useSimulationCost) {
				GUILayout.BeginScrollView (_vector2, false, false);
				if (useCredits) {
					_string += "\n<color=#FFFFFF>credits: </color><color=#B4D455><b>" + CreditsCost + "</b></color><color=#FFFFFF>.</color>";
				}
				if (useReputations) {
					_string += "\n<color=#FFFFFF>reputation: </color><color=#E0D503><b>" + ReputationsCost + "</b></color><color=#FFFFFF>.</color>";
				}
				if (useSciences) {
					_string += "\n<color=#FFFFFF>science: </color><color=#6DCFF6><b>" + SciencesCost + "</b></color><color=#FFFFFF>.</color>";
				}
				GUILayout.Label (_string, Text_Style_info, GUILayout.Height (80));
				GUILayout.EndScrollView ();
			}
			GUILayout.Space(5);
			if (!isPrelaunch && HighLogic.LoadedSceneIsFlight && useTimeCost && (!isFunded || !isFunded_sciences)) {
				if (!FlightDriver.Pause) {
					FlightDriver.SetPause (true);
					Save ();
				}
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Revert to the editor.", GUILayout.Height (30))) {
					Window_info = false;
					FlightDriver.SetPause (false);
					FlightDriver.RevertToPrelaunch (EditorFacility.VAB);
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space(5);
			}
			GUILayout.EndVertical ();
		}

		// Panneau de configuration
		private void DrawSettings(int id) {
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			enable = GUILayout.Toggle (enable, "Enable Simulate, Revert & Launch");
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			GUILayout.Box ("Difficulty", GUILayout.Height (30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			ironman = GUILayout.Toggle (ironman, "Ironman (hardmode)", GUILayout.Width (235));
			GUILayout.Space(5);
			simulate = GUILayout.Toggle (simulate, "Simulate", GUILayout.Width (200));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) {
				GUILayout.BeginHorizontal ();
				GUILayout.Box ("Finance", GUILayout.Height (30));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				string _tmp;
				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
					GUILayout.BeginHorizontal ();
					Credits = GUILayout.Toggle (Credits, "Simulations will cost credits", GUILayout.Width (250));
					_tmp = GUILayout.TextField (Cost_credits.ToString());
					try {
						Cost_credits = Convert.ToInt32(_tmp);
					} catch {
						Cost_credits = 1000;
					}
					GUILayout.EndHorizontal ();
					GUILayout.Space (5);
					GUILayout.BeginHorizontal ();
					Reputations = GUILayout.Toggle (Reputations, "Simulations will cost reputation", GUILayout.Width (250));
					_tmp = GUILayout.TextField (Cost_reputations.ToString());
					try {
						Cost_reputations = Convert.ToInt32(_tmp);
					} catch {
						Cost_reputations = 50;
					}
					GUILayout.EndHorizontal ();
					GUILayout.Space (5);
				}
				GUILayout.BeginHorizontal ();
				Sciences = GUILayout.Toggle (Sciences, "Simulations will cost science", GUILayout.Width (250));
				_tmp = GUILayout.TextField (Cost_sciences.ToString());
				try {
					Cost_sciences = Convert.ToInt32(_tmp);
				} catch {
					Cost_sciences = 20;
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				GUILayout.BeginHorizontal ();
				Simulation_fct_duration = GUILayout.Toggle (Simulation_fct_duration, "The time passed in simulation will cost credit, reputation or science.", GUILayout.ExpandWidth(true));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				GUILayout.BeginHorizontal ();
				GUILayout.Space (10);
				GUILayout.Label("The amount of costs is influenced by:", Text_Style_label_settings, GUILayout.Width (400));
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Space(40);
				Simulation_fct_reputations = GUILayout.Toggle (Simulation_fct_reputations, "the reputation", GUILayout.Width (220));
				Simulation_fct_body = GUILayout.Toggle (Simulation_fct_body, "the celestial body", GUILayout.ExpandWidth(true));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				GUILayout.BeginHorizontal ();
				GUILayout.Space(40);
				Simulation_fct_vessel = GUILayout.Toggle (Simulation_fct_vessel, "the price of the vessel", GUILayout.Width (220));
				Simulation_fct_penalties = GUILayout.Toggle (Simulation_fct_penalties, "the penalties game difficulty", GUILayout.ExpandWidth(true));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
			}
			GUILayout.BeginHorizontal();
			GUILayout.Box("Unlock the simulations",GUILayout.Height(30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			if (Unlock_achievements && Unlock_techtree) {
				Unlock_achievements = !Unlock_achievements;
				Unlock_techtree = !Unlock_techtree;
			}
			if (GUILayout.Toggle (!Unlock_achievements && !Unlock_techtree, "All unlocked", GUILayout.ExpandWidth(true))) {
				Unlock_achievements = false;
				Unlock_techtree = false;
			}
			GUILayout.Space(5);
			if (GUILayout.Toggle (Unlock_achievements, "with achievements", GUILayout.ExpandWidth(true))) {
				Unlock_achievements = true;
				Unlock_techtree = false;
			}
			if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) {
				GUILayout.Space (5);
				if (GUILayout.Toggle (Unlock_techtree, "with tech tree", GUILayout.ExpandWidth(true))) {
					Unlock_achievements = false;
					Unlock_techtree = true;
				}
			} else if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX && Unlock_techtree) {
				Unlock_techtree = false;
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			GUILayout.Box("Others options",GUILayout.Height(30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button ("◄", GUILayout.Width(20), GUILayout.Height(25))) {
				String[] _systems = System.IO.Directory.GetFiles (Path_system);
				int _i = Array.FindIndex (_systems, item => item == Path_system + SelectSystem + ".cfg");
				_i--;
				if (_i < 0) {
					_i = System.IO.Directory.GetFiles (Path_system).Length -1;
				}
				SelectSystem = System.IO.Path.GetFileNameWithoutExtension(_systems[_i]);
			}
			GUILayout.Label ("System: " + SelectSystem, Text_Style_label_system, GUILayout.ExpandWidth(true), GUILayout.Height(30));
			if (GUILayout.Button ("►", GUILayout.Width(20), GUILayout.Height(25))) {
				String[] _systems = System.IO.Directory.GetFiles (Path_system);
				int _i = Array.FindIndex (_systems, item => item == Path_system + SelectSystem + ".cfg");
				_i++;
				if (_i >= System.IO.Directory.GetFiles (Path_system).Length) {
					_i = 0;
				}
				SelectSystem = System.IO.Path.GetFileNameWithoutExtension(_systems[_i]);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button ("◄", GUILayout.Width(20), GUILayout.Height(25))) {
				int _i = Array.FindIndex (GUIWhiteList, item => item == ActiveGUI);
				_i--;
				if (_i < 0) {
					_i = GUIWhiteList.Length -1;
				}
				ActiveGUI = GUIWhiteList[_i];
				GUI.skin = AssetBase.GetGUISkin(ActiveGUI);
				Window_Rect_settings = new Rect ((Screen.width - 515), 40, 515, 0);
			}
			GUILayout.Label ("Skin: " + ActiveGUI, Text_Style_label_system, GUILayout.ExpandWidth(true), GUILayout.Height(30));
			if (GUILayout.Button ("►", GUILayout.Width (20), GUILayout.Height(25))) {
				int _i = Array.FindIndex (GUIWhiteList, item => item == ActiveGUI);
				_i++;
				if (_i >= GUIWhiteList.Length) {
					_i = 0;
				}
				ActiveGUI = GUIWhiteList[_i];
				GUI.skin = AssetBase.GetGUISkin(ActiveGUI);
				Window_Rect_settings = new Rect ((Screen.width - 515), 40, 515, 0);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			if (GUILayout.Button ("Close",GUILayout.Height(30))) {
				if (ApplicationLauncher.Ready && Button_isTrue) {
					Button.SetFalse ();
				}
			}
			GUILayout.BeginHorizontal();
			GUILayout.Space(5);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			if (!enable) {
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
				InputLockManager.RemoveControlLock ("SRLvesselswitching");
			}
		}

		// Sauvegarde des paramètres
		public void Save() {
			ConfigNode _temp = ConfigNode.CreateConfigFromObject(this, new ConfigNode());
			_temp.Save(Path_settings + HighLogic.SaveFolder + "-config.txt");
			Debug("Save");
		}

		// Charger les variables
		//private void OnGameStateLoad (ConfigNode confignode) {
		//	Load ();
		//}

		// Gérer les Achievements
		private void CheckAchievements (Vessel vessel) {
			if (isSimulate || vessel.vesselType == VesselType.Debris || vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Unknown || Loadachievements) {
				return;
			}
			string _bodyName = vessel.mainBody.bodyName;
			if ((vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ESCAPING) && !Achiev_orbit.Contains (_bodyName)) {
				Achiev_orbit.Add (_bodyName);
				if (Unlock_achievements && MessageSystem.Ready) {
					MessageSystem.Instance.AddMessage (new MessageSystem.Message ("Simulate, Revert & Launch", "You can now make a simulation while <#8BED8B><b>in orbit around " + Realbody(vessel.mainBody) + ".</b></>", MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.ALERT));
				}
				Debug ("Orbital achievement: " + vessel.name + " / " + _bodyName);
				Save();
			}
			if (vessel.LandedOrSplashed && !Achiev_land.Contains (_bodyName)) {
				Achiev_land.Add (_bodyName);
				int _i = FlightGlobals.Bodies.FindIndex (b => b == vessel.mainBody);
				Vector3d _land_pos1 = LandedPos [_i * 2];
				Vector3d _land_pos2 = LandedPos [_i * 2 + 1];
				if (Unlock_achievements && _land_pos1.x != -1 && _land_pos2.x != -1 && MessageSystem.Ready) {
					MessageSystem.Instance.AddMessage (new MessageSystem.Message ("Simulate, Revert & Launch", "You can now make a simulation while <#8BED8B><b>landed on " + Realbody(vessel.mainBody) + ".</b></>", MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.ALERT));
				}
				Debug ("Landed achievement: " + vessel.name + " / " + _bodyName);
				Save();
			}
		}

		private void CheckAchievements (CelestialBody body, bool landed) {
			if (landed) {
				if (!Achiev_land.Contains (body.bodyName)) {
					Achiev_land.Add (body.bodyName);
				}
			}
			CelestialBody _Sun = FlightGlobals.Bodies [0];
			if (body.referenceBody.bodyName != DefaultRealBody && body.bodyName != DefaultRealBody) {
				if (!Achiev_orbit.Contains (_Sun.bodyName)) {
					Achiev_orbit.Add (_Sun.bodyName);
				}
				if (!Achiev_orbit.Contains (DefaultRealBody)) {
					Achiev_orbit.Add (DefaultRealBody);
				}
			}
			while (body != _Sun) {
				if (!Achiev_orbit.Contains (body.bodyName)) {
					Achiev_orbit.Add (body.bodyName);
				}
				body = body.referenceBody;
			}
		}

		// Charger les achievements effectué
		private void LoadAchievements() {
			Loadachievements = false;
			if (ResearchAndDevelopment.Instance != null) {
				List<ScienceSubject> _Subjects = ResearchAndDevelopment.GetSubjects ();
				if (_Subjects.Count > 0) {
					foreach (ScienceSubject _subject in _Subjects) {
						CelestialBody _body = body (_subject);
						if (_body != null) {
							continue;
						}
						CheckAchievements (_body, _subject.IsFromSituation (ExperimentSituations.SrfLanded) || _subject.IsFromSituation (ExperimentSituations.SrfSplashed));
					}
					Debug ("Sciences OK! " + _Subjects.Count);
				}
			}
			if (FlightGlobals.Vessels.Count > 0) {
				List<Vessel> _vessels = FlightGlobals.Vessels;
				foreach (Vessel _vessel in _vessels) {
					if (_vessel.vesselType == VesselType.Debris || _vessel.vesselType == VesselType.SpaceObject || _vessel.vesselType == VesselType.Unknown || Loadachievements) {
						continue;
					}
					CelestialBody _body = _vessel.mainBody; 
					CheckAchievements (_body, _vessel.LandedOrSplashed);
				}
				Debug ("Vessels OK! " + FlightGlobals.Vessels.Count);
			}
			Debug ("Achievements loaded");
			Save();
		}

		// Charger le système solaire sélectionné
		private void LoadSystem() {
			if (SelectSystem == string.Empty) {
				SelectSystem = "Kerbol";
			}
			if (!System.IO.File.Exists (Path_system + SelectSystem + ".cfg")) {
				Debug ("THE SYSTEM CONFIG FILE DON'T EXIST: " + Path_system + SelectSystem + ".cfg");
				return;
			}
			ConfigNode _temp = ConfigNode.Load (Path_system + SelectSystem + ".cfg");
			if (!_temp.HasNode("SRL")) {
				Debug ("ERROR IN THE SYSTEM CONFIG FILE: " + Path_system + SelectSystem + ".cfg");
				return;
			}
			BodyNames = TabClean(_temp.GetNode ("SRL").GetValue ("BodyNames")).Split (' ');
			DefaultBody = _temp.GetNode ("SRL").GetValue ("DefaultBody");
			if (_temp.GetNode ("SRL").HasValue ("DefaultRealBody")) {
				DefaultRealBody = _temp.GetNode ("SRL").GetValue ("DefaultRealBody");
			} else {
				DefaultRealBody = DefaultBody;
			}
			DefaultAltitude = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetValue ("DefaultAltitude")).Split (' '), element => int.Parse (element));
			MinimumAltitude = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetValue ("MinimumAltitude")).Split (' '), element => double.Parse (element));
			SimOrbitPrice = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetValue ("SimOrbitPrice")).Split (' '), element => double.Parse (element));
			SimLandedPrice = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetValue ("SimLandedPrice")).Split (' '), element => double.Parse (element));
			SimTechTreeUnlock = TabClean(_temp.GetNode ("SRL").GetValue ("SimTechTreeUnlock")).Split (',');
			double[] _x1 = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetNode ("LandedPos").GetValue ("x1")).Split (' '), element => double.Parse (element));
			double[] _y1 = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetNode ("LandedPos").GetValue ("y1")).Split (' '), element => double.Parse (element));
			double[] _z1 = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetNode ("LandedPos").GetValue ("z1")).Split (' '), element => double.Parse (element));
			double[] _x2 = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetNode ("LandedPos").GetValue ("x2")).Split (' '), element => double.Parse (element));
			double[] _y2 = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetNode ("LandedPos").GetValue ("y2")).Split (' '), element => double.Parse (element));
			double[] _z2 = Array.ConvertAll (TabClean(_temp.GetNode ("SRL").GetNode ("LandedPos").GetValue ("z2")).Split (' '), element => double.Parse (element));
			LandedPos = new Vector3d[_x1.Length * 2];
			for (int _i = 0;_i < _x1.Length;_i++) {
				LandedPos [_i * 2] = new Vector3d (_x1 [_i], _y1 [_i], _z1 [_i]);
				LandedPos [_i * 2 + 1] = new Vector3d (_x2 [_i], _y2 [_i], _z2 [_i]);
			}
			if (FlightGlobals.Bodies.Count != BodyNames.Length || BodyNames.Length != DefaultAltitude.Length || BodyNames.Length != MinimumAltitude.Length || BodyNames.Length != SimOrbitPrice.Length || BodyNames.Length!= SimLandedPrice.Length || (BodyNames.Length * 2) != LandedPos.Length) {
				Debug (string.Format("THE CONFIG FILE OF SOLAR SYSTEM SEEM TO HAVE AN ERROR !\nFlightGlobals.Bodies: {0}\nBodyNames: {1}\nDefaultAltitude: {2}\nMinimumAltitude: {3}\nSimOrbitPrice: {4}\nSimLandedPrice: {5}\nLandedPos: {6}", FlightGlobals.Bodies.Count, BodyNames.Length, DefaultAltitude.Length, MinimumAltitude.Length, SimOrbitPrice.Length, SimLandedPrice.Length, LandedPos.Length));
			}
			if (System.IO.Directory.GetFiles (Path_techtree).Length != SimTechTreeUnlock.Length) {
				Debug (string.Format ("THE TECH TREE SEEM TO HAVE AN ERROR !\nTechTree: {0}\nSimTechTreeUnlock: {1}", System.IO.Directory.GetFiles (Path_techtree).Length, SimTechTreeUnlock.Length));
			}
			Debug ("System loaded");
		}

		// Charger les paramètres
		public void Load() {
			if (System.IO.File.Exists (Path_settings + HighLogic.SaveFolder + "-config.txt")) {
				ConfigNode _temp = ConfigNode.Load (Path_settings + HighLogic.SaveFolder + "-config.txt");
				ConfigNode.LoadObjectFromConfig (this, _temp);
				if (VERSION_config != VERSION) {
					Reset ();
				}
				if (Achiev_land.Count <= 1) {
					Achiev_land = new List<string> { "Kerbin" };
				}
				if (Cost_credits <= 0) {
					Cost_credits = 1000;
				}
				if (Cost_reputations <= 0) {
					Cost_reputations = 50;
				}
				if (Cost_sciences <= 0) {
					Cost_sciences = 20;
				}
				if (SelectSystem == string.Empty) {
					SelectSystem = "Kerbol";
				}
				Debug("Load");
			} else {
				Reset ();
			}
			LoadSystem ();
		}
		public void Reset() {
			VERSION_config = VERSION;
			if (!System.IO.File.Exists (Path_settings + HighLogic.SaveFolder + "-config.txt")) {
				enable = true;
				ironman = true;
				simulate = true;
				Credits = true;
				Reputations = true;
				Sciences = false;
				Simulation_fct_duration = true;
				Simulation_fct_reputations = true;
				Simulation_fct_body = true;
				Simulation_fct_vessel = true;
				Simulation_fct_penalties = true;
				Cost_credits = 1000;
				Cost_reputations = 50;
				Cost_sciences = 20;
				Unlock_achievements = true;
				Unlock_techtree = false;
				SelectSystem = "Kerbol";
				ActiveGUI = HighLogic.Skin.name;
			}
			if (Achiev_land.Count <= 1) {
				Achiev_land = new List<string> { "Kerbin" };
			}
			price_factor_body = 0;
			price_factor_vessel = 0;
			N_launch = 0;
			N_quickload = 0;
			Simulation_duration = 0;
			Debug("Reset");
			Loadachievements = true;
			Save ();
		}
		private static void Debug(string _string) {
			if (isdebug) {
				print ("SRL" + VERSION + ": " + _string);
			}
		}
	}
}