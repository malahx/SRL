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
using System.Collections.Generic;
using System.Timers;
using System.Linq;
using System.Text;
using KSP;
using UnityEngine;

namespace SRL {

	[KSPAddon(KSPAddon.Startup.EditorAny | KSPAddon.Startup.TrackingStation | KSPAddon.Startup.Flight | KSPAddon.Startup.SpaceCentre, false)]
	public class SRL : MonoBehaviour {

		// Initialiser les variables
		public const string VERSION = "1.20";
		private Texture Button_texture_sim = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/sim", false);
		private Texture Button_texture_srl = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/srl", false);
		private Texture Button_texture_insim = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/insim", false);
		private string Path_settings = KSPUtil.ApplicationRootPath + "GameData/SRL/PluginData/SRL/";
		private string Text_simulation = "SIMULATION";
		private string Text_loading = "LOADING ...";
		private string Text_nofund = "You need more credits to make a simulation";
		private string Text_nofund_sciences = "You need more science to make a simulation";
		private CelestialBody[] CelestialBodys;

		private int[] CelestialBodys_def_alt = { 
			7000, 	// Sun
			75,   	// Kerbin
			15,   	// Mun
			10,   	// Minmus
			35,   	// Moho
			100,  	// Eve
			70,   	// Duna
			15,   	// Ike
			200,  	// Jool
			65,   	// Laythe
			25,   	// Vall
			25,   	// Bop
			65,   	// Tylo
			10,   	// Gilly
			6,    	// Pol
			35,   	// Dres
			25   	// Eeloo
		};

		// -1 == atmosphere
		private double[] CelestialBodys_min_alt = {
			1.35,	// Sun
			-1, 	// Kerbin
			7.1, 	// Mun
			5.75, 	// Minmus
			6.85, 	// Moho
			-1, 	// Eve
			-1, 	// Duna
			12.8, 	// Ike
			140, 	// Jool
			-1, 	// Laythe
			8, 		// Vall
			21.8, 	// Bop
			11.3, 	// Tylo
			6.5, 	// Gilly
			5.6, 	// Pol
			5.75, 	// Dres
			3.9 	// Eeloo
		};

		// -1, -1, -1 == can't land
		private Vector3d[] CelestialBodys_land_pos = {
			new Vector3d (-1, -1, -1), // Sun 1
			new Vector3d (-1, -1, -1), // Sun 2
			new Vector3d (1, 1, 1), // Kerbin 1
			new Vector3d (1, 1, 1), // Kerbin 2
			new Vector3d (-1, -1, -1), // Mun 1
			new Vector3d (-1, -1, -1), // Mun 2
			new Vector3d (-1, -1, -1), // Minmus 1
			new Vector3d (-1, -1, -1), // Minmus 2
			new Vector3d (-1, -1, -1), // Moho 1
			new Vector3d (-1, -1, -1), // Moho 2
			new Vector3d (-1, -1, -1), // Eve 1
			new Vector3d (-1, -1, -1), // Eve 2
			new Vector3d (-1, -1, -1), // Duna 1
			new Vector3d (-1, -1, -1), // Duna 2
			new Vector3d (-1, -1, -1), // Ike 1
			new Vector3d (-1, -1, -1), // Ike 2
			new Vector3d (-1, -1, -1), // Jool 1
			new Vector3d (-1, -1, -1), // Jool 2
			new Vector3d (-1, -1, -1), // Laythe 1
			new Vector3d (-1, -1, -1), // Laythe 2
			new Vector3d (-1, -1, -1), // Vall 1
			new Vector3d (-1, -1, -1), // Vall 2
			new Vector3d (-1, -1, -1), // Bop 1
			new Vector3d (-1, -1, -1), // Bop 2
			new Vector3d (-1, -1, -1), // Tylo 1 
			new Vector3d (-1, -1, -1), // Tylo 2
			new Vector3d (-1, -1, -1), // Gilly 1
			new Vector3d (-1, -1, -1), // Gilly 2
			new Vector3d (-1, -1, -1), // Pol 1
			new Vector3d (-1, -1, -1), // Pol 2
			new Vector3d (-1, -1, -1), // Dres 1
			new Vector3d (-1, -1, -1), // Dres 2
			new Vector3d (-1, -1, -1),  // Eeloo 1
			new Vector3d (-1, -1, -1),  // Eeloo 2
		};

		private double[] CelestialBodys_fct_price = {
			1.2, // Sun in orbit
			100, // Sun landed
			1.1, // Kerbin in orbit
			1.0, // Kerbin landed
			1.25, // Mun in orbit
			1.4, // Mun landed
			1.2, // Minmus in orbit
			1.3, // Minmus landed
			1.85, // Moho in orbit
			2.15, // Moho landed
			1.5, // Eve in orbit
			4.15, // Eve landed
			1.3, // Duna in orbit
			1.6, // Duna landed
			1.4, // Ike in orbit
			1.5, // Ike landed
			2, // Jool in orbit
			100, // Jool landed
			2.5, // Laythe in orbit
			3.15, // Laythe landed
			2.6, // Vall in orbit
			2.85, // Vall landed
			2.75, // Bop in orbit
			2.8, // Bop landed
			2.7, // Tylo in orbit 
			3.4, // Tylo landed
			1.95, // Gilly in orbit
			1.95, // Gilly landed
			2.75, // Pol in orbit
			2.8, // Pol landed
			1.45, // Dres in orbit
			1.6, // Dres landed
			1.9,  // Eeloo in orbit
			2.1,  // Eeloo landed
		};

		// Variables sauvegardées par session
		[KSPField(isPersistant = true)]
		public static bool isSimulate = false;
		[KSPField(isPersistant = true)]
		public static bool orbit;
		[KSPField(isPersistant = true)]
		public static int CelestialBody;
		[KSPField(isPersistant = true)]
		private static double altitude;

		// Variables temporaires
		private ApplicationLauncherButton Button;
		private GUIStyle Text_Style_simulate;
		private GUIStyle Text_Style_info;
		private GUIStyle Text_Style_loading;
		private AltimeterSliderButtons Recovery_button = null;
		private int Index = 0;
		private bool Window_settings = false;
		private bool Window_info = false;
		private bool Window_simulate = false;
		private bool loading = false;
		private bool last_isSimulate = !isSimulate;
		private double last_time = 0;
		private double Time_FlightReady = 0;
		private bool N_plus1 = false;
		private Timer timer = new Timer(10000);
		private Timer sim_timer = new Timer (2000);

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
		private List<String> Achiev_orbit = new List<String> {};
		[Persistent]
		private List<String> Achiev_land = new List<String> {};

		private bool CanSimulate {
			get {
				return this.enable && this.simulate;
			}
		}
		private bool isIronman {
			get {
				return this.enable && this.ironman;
			}
		}
		private bool isFunded {
			get {
				if (this.useCredits) {
					if (this.isPrelaunch) {
						return (Funding.Instance.Funds + Convert.ToInt32 (this.VesselCost) + this.CreditsCost) > 0;
					} else {
						return (Funding.Instance.Funds + this.CreditsCost) > 0;
					}
				} else {
					return true;
				}
			}
		}
		private bool isFunded_sciences {
			get {
				if (this.useSciences) {
					return (ResearchAndDevelopment.Instance.Science + this.SciencesCost) > 0;
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
				return this.Credits && HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
			}
		}
		private bool useReputations {
			get {
				return this.Reputations && HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
			}
		}
		private bool useSciences {
			get {
				return this.Sciences && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);
			}
		}
		private bool useTimeCost {
			get {
				return this.Simulation_fct_duration && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);
			}
		}
		private bool useSimulationCost {
			get {
				return this.useCredits || this.useReputations || this.useSciences;
			}
		}
		private float CreditsCost {
			get {
				if (this.useCredits) {
					int _N = this.N_launch;
					if (this.N_plus1 && Planetarium.GetUniversalTime() > (this.last_time +10)) {
						_N++;
					}
					double _cost = 0;
					if (this.Credits) {
						_cost -= this.Cost_credits * _N + (this.Cost_credits / 2 * this.N_quickload);
					}
					if (this.Simulation_fct_duration) {
						_cost -= this.Cost_credits / 20 * (this.Simulation_duration / (this.GetKerbinTime * 3600));
					}
					if (this.Simulation_fct_reputations) {
						_cost *= (1 - Reputation.UnitRep / 2);
					}
					if (this.Simulation_fct_body) {
						if (this.price_factor_body > 0 && this.Simulation_duration > 0) {
							_cost *= this.price_factor_body / this.Simulation_duration;
						} else {
							if (orbit) {
								_cost *= CelestialBodys_fct_price [CelestialBody * 2];
							} else {
								_cost *= CelestialBodys_fct_price [CelestialBody * 2+1];
							}
						}
					}
					if (this.Simulation_fct_vessel) {
						if (this.price_factor_vessel > 0 && this.Simulation_duration > 0) {
							_cost *= this.price_factor_vessel / this.Simulation_duration;
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
		private float ReputationsCost {
			get {
				if (this.useReputations) {
					int _N = this.N_launch;
					if (this.N_plus1 && Planetarium.GetUniversalTime() > (this.last_time +10)) {
						_N++;
					}
					double _cost = 0;
					if (this.Reputations) {
						_cost -= this.Cost_reputations * _N + (this.Cost_credits / 2 * this.N_quickload);
					}
					if (this.Simulation_fct_duration) {
						_cost -= this.Cost_reputations / 20 * (this.Simulation_duration / (this.GetKerbinTime * 3600));
					}
					if (this.Simulation_fct_reputations) {
						_cost *= (1 - Reputation.UnitRep / 2);
					}
					if (this.Simulation_fct_body) {
						if (this.price_factor_body > 0 && this.Simulation_duration > 0) {
							_cost *= this.price_factor_body / this.Simulation_duration;
						} else {
							if (orbit) {
								_cost *= CelestialBodys_fct_price [CelestialBody * 2];
							} else {
								_cost *= CelestialBodys_fct_price [CelestialBody * 2+1];
							}
						}
					}
					if (this.Simulation_fct_vessel) {
						if (this.price_factor_vessel > 0 && this.Simulation_duration > 0) {
							_cost *= this.price_factor_vessel / this.Simulation_duration;
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
		private float SciencesCost {
			get {
				if (this.useSciences) {
					int _N = this.N_launch;
					if (this.N_plus1 && Planetarium.GetUniversalTime() > (this.last_time +10)) {
						_N++;
					}
					double _cost = 0;
					if (this.Sciences) {
						_cost -= this.Cost_sciences * _N + (this.Cost_sciences / 2 * this.N_quickload);
					}
					if (this.Simulation_fct_duration) {
						_cost -= this.Cost_sciences / 20 * (this.Simulation_duration / (this.GetKerbinTime * 3600));
					}
					if (this.Simulation_fct_reputations) {
						_cost *= (1 - Reputation.UnitRep / 2);
					}
					if (this.Simulation_fct_body) {
						if (this.price_factor_body > 0 && this.Simulation_duration > 0) {
							_cost *= this.price_factor_body / this.Simulation_duration;
						} else {
							if (orbit) {
								_cost *= CelestialBodys_fct_price [CelestialBody * 2];
							} else {
								_cost *= CelestialBodys_fct_price [CelestialBody * 2+1];
							}
						}
					}
					if (this.Simulation_fct_vessel) {
						if (this.price_factor_vessel > 0 && this.Simulation_duration > 0) {
							_cost *= this.price_factor_vessel / this.Simulation_duration;
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
					foreach (ProtoPartSnapshot part in FlightGlobals.ActiveVessel.protoVessel.protoPartSnapshots) {
						ShipConstruction.GetPartCosts (part, part.partInfo, out _dryCost, out _fuelCost);
						_VesselCost += _dryCost + _fuelCost;
					}
					return _VesselCost;
				} 
				return 0;
			}
		}
		private bool Button_isTrue {
			get {
				return this.Button.State == RUIToggleButton.ButtonState.TRUE;
			}
		}
		private bool Button_isFalse {
			get {
				return this.Button.State == RUIToggleButton.ButtonState.FALSE;
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

		// Préparer les variables et les évènements
		private void Awake() {
			GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Add (OnLaunch);
			GameEvents.onFlightReady.Add (OnFlightReady);
			GameEvents.onCrewOnEva.Add (OnCrewOnEva);
			GameEvents.onGameStateLoad.Add (OnGameStateLoad);
			GameEvents.onLevelWasLoaded.Add (OnLevelWasLoaded);
			GameEvents.onVesselGoOffRails.Add (OnVesselGoOffRails);
			if (HighLogic.LoadedSceneIsGame) {
				this.CelestialBodys = FlightGlobals.Bodies.ToArray ();
			}
			this.Text_Style_simulate = new GUIStyle ();
			this.Text_Style_simulate.stretchWidth = true;
			this.Text_Style_simulate.stretchHeight = true;
			this.Text_Style_simulate.alignment = TextAnchor.UpperCenter;
			this.Text_Style_simulate.fontSize = (Screen.height/20);
			this.Text_Style_simulate.fontStyle = FontStyle.Bold;
			this.Text_Style_simulate.normal.textColor = Color.red;
			this.Text_Style_info = new GUIStyle ();
			this.Text_Style_info.stretchWidth = true;
			this.Text_Style_info.stretchHeight = true;
			this.Text_Style_info.wordWrap = true;
			this.Text_Style_info.alignment = TextAnchor.MiddleLeft;
			this.Text_Style_loading = new GUIStyle ();
			this.Text_Style_loading.stretchWidth = true;
			this.Text_Style_loading.stretchHeight = true;
			this.Text_Style_loading.alignment = TextAnchor.MiddleCenter;
			this.Text_Style_loading.fontSize = (Screen.height/20);
			this.Text_Style_loading.fontStyle = FontStyle.Bold;
			this.Text_Style_loading.normal.textColor = Color.red;
			this.Text_Style_loading.normal.background = (Texture2D)GameDatabase.Instance.GetTexture ("SRL/Textures/loading", false);
			this.timer.Elapsed += new ElapsedEventHandler(OnTimer);
			this.sim_timer.Elapsed += new ElapsedEventHandler(OnSim_Timer);
		}

		// Supprimer le bouton de simulation et les évènements
		private void OnDestroy() {
			GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Remove (OnLaunch);
			GameEvents.onFlightReady.Remove (OnFlightReady);
			GameEvents.onCrewOnEva.Remove (OnCrewOnEva);
			GameEvents.onGameStateLoad.Remove (OnGameStateLoad);
			GameEvents.onLevelWasLoaded.Remove (OnLevelWasLoaded);
			GameEvents.onVesselGoOffRails.Remove (OnVesselGoOffRails);
			if (this.Button != null) {
				ApplicationLauncher.Instance.RemoveModApplication (this.Button);
				this.Button = null;
			}
		}

		// Afficher le bouton de simulation
		private void OnGUIApplicationLauncherReady() {
			if (ApplicationLauncher.Ready) {
				this.Button = ApplicationLauncher.Instance.AddModApplication (this.Button_On, this.Button_Off, this.Button_OnHover, this.Button_OnHoverOut, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.MAPVIEW, this.Button_texture_srl);
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION) {
					isSimulate = false;
					CelestialBody = 1;
					altitude = this.CelestialBodys_def_alt[CelestialBody];
					orbit = false;
				}
				if (!this.CanSimulate) {
					this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER;
				} else if (HighLogic.LoadedSceneIsEditor) {
					this.Button.SetTexture(this.Button_texture_sim);
				}
			}
		}

		// Charger la simulation.
		private void OnLevelWasLoaded(GameScenes gamescenes) {
			if (this.CanSimulate) {
				if (isSimulate) {
					if (HighLogic.LoadedSceneIsFlight) {
						if (orbit || CelestialBody != 1) {
							if (QuickRevert_fct.Save_Vessel_Guid == Guid.Empty) {
								this.loading = true;
								new UI_Toggle ();
								InputLockManager.SetControlLock (ControlTypes.All, "SRLall");
							}
							HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = false;
							HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = false;
							print ("SRL" + VERSION + ": Loading simulation ...");
						} 
					}
				} else {
					this.sim_timer.Enabled = false;
					this.loading = false;
					InputLockManager.RemoveControlLock ("SRLall");
				}
				if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
					this.N_plus1 = true;
				}
			}
		}

		private void OnVesselGoOffRails (Vessel vessel) {
			if (this.CanSimulate && isSimulate && this.isPrelaunch && vessel == FlightGlobals.ActiveVessel) {
				CelestialBody _body = this.CelestialBodys [CelestialBody];
				Vessel _vessel = FlightGlobals.ActiveVessel;
				if (orbit) {
					launch ();
					// HyperEdit functions
					HyperEdit_fct.Set (_vessel.orbitDriver.orbit, HyperEdit_fct.CreateOrbit (0, 0, altitude * 1000 + _body.Radius, 0, 0, 0, 0, _body));
					//
					this.sim_timer.Enabled = true;
					print ("SRL" + VERSION + ": Simulate to orbit of " + _body.bodyName);
				} else if (CelestialBody != 1) {
					launch ();
					// HyperEdit functions
					HyperEdit_fct.Set (_vessel.orbitDriver.orbit, HyperEdit_fct.CreateOrbit (0, 0, _body.sphereOfInfluence - 1000, 0, 0, 0, 0, _body));
					//
					// Add the landed functions
					print ("SRL" + VERSION + ": Simulate landed on " + _body.bodyName);
				} else {
					print ("SRL" + VERSION + ": Simulate landed on " + _body.bodyName);
				}
			}
		}

		// Activer le revert après un quickload
		private void OnFlightReady() {
			if (this.CanSimulate) {
				this.Time_FlightReady = Planetarium.GetUniversalTime ();
				if (this.isPrelaunch) {
					if (ApplicationLauncher.Ready) {
						if (this.N_plus1) {
							this.Button.SetTexture (this.Button_texture_sim);
						} else {
							this.Button.SetTexture (this.Button_texture_insim);
						}
					}
					HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = false;
				} else if (isSimulate) {
					if (QuickRevert_fct.Save_Vessel_Guid == FlightGlobals.ActiveVessel.id) {
						this.Button.SetTexture (this.Button_texture_insim);
						this.N_quickload++;
						this.N_plus1 = false;
						this.Save ();
						HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
					} else {
						this.N_plus1 = false;
					}
				}
				if (isSimulate) {
					if (System.IO.File.Exists (KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs")) {
						if (GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, true, false).UniversalTime > QuickRevert_fct.Save_PreLaunchState.UniversalTime) {
							GamePersistence.SaveGame (FlightDriver.PreLaunchState, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
							print ("SRL" + VERSION + ": Keep the save");
						}
					}
				}
			}
		}

		// Bloquer l'accès au bouton de simulation après le lancement de la fusée
		private void OnLaunch(EventReport EventReport) {
			if (this.CanSimulate) {
				if (isSimulate) {
					if (FlightGlobals.ActiveVessel.mainBody.bodyName == "Kerbin" && FlightGlobals.ActiveVessel.situation != Vessel.Situations.ORBITING) {
						CelestialBody = 1;
						altitude = this.CelestialBodys_def_alt[CelestialBody];
						orbit = false;
					}
					launch ();
				} else {
					if (ApplicationLauncher.Ready) {
						this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
						this.N_plus1 = false;
					}
				}				
			}
		}
			
		// Modifier les variables après un lancement
		private void launch() {
			if (ApplicationLauncher.Ready) {
				this.Button.SetTexture (this.Button_texture_insim);
				if (this.Button_isTrue) {
					this.Button.SetFalse ();
					isSimulate = true;
				}
			}
			if (this.useCredits) {
				Funding.Instance.AddFunds (Convert.ToInt32 (this.VesselCost), TransactionReasons.Vessels);
			}
			this.last_time = Planetarium.GetUniversalTime ();
			if (this.N_plus1) {
				this.N_launch++;
			}
			this.N_plus1 = false;
			this.Time_FlightReady = 0;
			this.Save ();
			HighLogic.CurrentGame.Parameters.Flight.CanQuickSave = true;
			print ("SRL" + VERSION + ": Launch");
		}

		// Supprimer le bouton de simulation à l'EVA
		private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> part) {
			if (this.CanSimulate && part.from.vessel.situation == Vessel.Situations.PRELAUNCH && ApplicationLauncher.Ready) {
				this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
				isSimulate = false;
			}
		}

		// Effacer certains messages temporaires
		private void OnTimer(object sender, ElapsedEventArgs e) {
			this.timer.Enabled = false;
			this.Index = 0;
			print ("SRL" + VERSION + ": Message off");
		}

		// Enlever l'écran de chargement
		private void OnSim_Timer(object sender, ElapsedEventArgs e) {
			this.sim_timer.Enabled = false;
			this.loading = false;
			new UI_Toggle ();
			InputLockManager.RemoveControlLock ("SRLall");
		}

		// Activer le bouton
		private void Button_On() {
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				this.Window_settings = true;
				InputLockManager.SetControlLock (ControlTypes.KSC_FACILITIES, "SRLkscfacilities");
			} else if (HighLogic.LoadedSceneIsFlight) {
				if (isSimulate && !this.N_plus1 && this.isFunded && this.isFunded_sciences) {
					this.Window_info = true;
				} else {
					isSimulate = true;
				}
			} else if (HighLogic.LoadedSceneIsEditor) {
				this.Window_simulate = true;
				this.Window_info = true;
				isSimulate = true;
				if (EditorLogic.fetch.launchBtn.controlIsEnabled) {
					InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK, "SRLeditor");
				}
			}
		}

		// Désactiver le bouton
		private void Button_Off() {
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				this.Window_settings = false;
				InputLockManager.RemoveControlLock("SRLkscfacilities");
				this.Save ();
			} else {
				if (isSimulate && !this.N_plus1 && HighLogic.LoadedSceneIsFlight) {
					this.Window_info = false;
				} else {
					this.Window_simulate = false;
					this.Window_info = false;
					isSimulate = false;
					InputLockManager.RemoveControlLock ("SRLeditor");
				}
			}
		}

		// Passer la souris sur le bouton
		private void Button_OnHover() {
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor) {
				if (isSimulate && this.isFunded && this.isFunded_sciences) {
					this.Window_info = true;
					if (HighLogic.LoadedSceneIsEditor) {
						this.Window_simulate = true;
						if (EditorLogic.fetch.launchBtn.controlIsEnabled) {
							InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK, "SRLeditor");
						}
					}
				}
			}
		}

		// Enlever la souris du bouton
		private void Button_OnHoverOut() {
			if (((isSimulate && this.Button_isFalse) || this.N_plus1) && HighLogic.LoadedSceneIsFlight) {
				this.Window_info = false;
			}
		}				

		// Activer la simulation
		private void Simulation_on() {
			if (ApplicationLauncher.Ready) {
				if ((HighLogic.LoadedSceneIsEditor || this.isPrelaunch) && this.Button_isFalse) {
					this.Button.SetTrue ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (!this.isPrelaunch) {
						this.Button.SetTexture (this.Button_texture_insim);
					}
					FlightGlobals.ActiveVessel.isPersistent = false;
					FlightDriver.CanRevertToPostInit = true;
					FlightDriver.CanRevertToPrelaunch = true;
					QuickSaveLoad.fetch.AutoSaveOnQuickSave = false;
					FlightDriver.fetch.bypassPersistence = true;
				}
				if (HighLogic.LoadedSceneIsEditor) {
					this.N_plus1 = true;
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
			print ("SRL"+VERSION+": Simulation ON");
		}

		// Désactiver la simulation
		private void Simulation_off() {
			if (ApplicationLauncher.Ready) {
				if (this.Button_isTrue && !this.Window_settings) {
					this.Button.SetFalse ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (!this.isPrelaunch) {
						this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
						this.N_plus1 = false;
					}
					if (this.isIronman) {
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
			if (this.isIronman) {
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
			this.Simulation_pay ();
			print ("SRL"+VERSION+": Simulation OFF");
		}

		// Payer la simulation
		private void Simulation_pay() {
			if (this.CanSimulate && !isSimulate && this.N_launch >= 1 && !HighLogic.LoadedSceneIsFlight && MessageSystem.Ready) {
				if (this.useSimulationCost) {
					if (this.N_plus1) {
						this.N_plus1 = false;
					}
					string _string;
					_string = "<b>In simulation mode, you did:\nmake <#8BED8B>" + this.N_launch + "</> launch(s),\nuse <#8BED8B>" + this.N_quickload + "</> quickload(s),\nspend <#8BED8B>" + Convert.ToInt32 (this.Simulation_duration / (this.GetKerbinTime * 3600)) + "</> day(s).</>\n\nThe simulations have cost:";
					float _float;
					if (this.useCredits) {
						_float = this.CreditsCost;
						_string += " <#B4D455>£" + _float + "</>";
						Funding.Instance.AddFunds (_float, TransactionReasons.Vessels);
						if (this.useReputations && this.useSciences) {
							_string += ", ";
						} else if (this.useReputations || this.useSciences) {
							_string += " and";
						}
					}
					if (this.useReputations) {
						_float = this.ReputationsCost;
						_string += " <#E0D503>¡" + _float + "</>";
						Reputation.Instance.AddReputation (_float, TransactionReasons.Vessels);
						if (this.useSciences) {
							_string += " and";
						}
					}
					if (this.useSciences) {
							_float = this.SciencesCost;
						_string += " <#6DCFF6>¡" + _float + "</>";
						ResearchAndDevelopment.Instance.AddScience (_float, TransactionReasons.Vessels);
					}
					_string += ".";
					MessageSystem.Instance.AddMessage (new MessageSystem.Message ("Simulation ended", _string, MessageSystemButton.MessageButtonColor.ORANGE, MessageSystemButton.ButtonIcons.ALERT));
				}
				if (HighLogic.LoadedSceneIsEditor) {
					this.N_plus1 = true;
				}
				this.N_launch = 0;
				this.N_quickload = 0;
				this.Simulation_duration = 0;
				this.price_factor_body = 0;
				this.price_factor_vessel = 0;
				this.Save ();
				print ("SRL" + VERSION + ": Simulation paid");
			}
		}

		// Mettre à jours les variables de simulation et désactiver le bouton de récupération si la fusée est au sol de Kerbin
		public void Update() {
			if (this.enable) {
				if (HighLogic.LoadedSceneIsGame) {
					if (this.isIronman && HighLogic.CurrentGame.Parameters.Flight.CanRestart == HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter) {
						this.last_isSimulate = !isSimulate;
					}
					if (ApplicationLauncher.Ready) {
						if (this.last_isSimulate != isSimulate) {
							if (isSimulate) {
								this.Simulation_on ();
							} else {
								this.Simulation_off ();
							}
							this.last_isSimulate = isSimulate;
						}
						if (HighLogic.LoadedSceneIsFlight) {
							if (FlightGlobals.ready) {
								if (!isSimulate) {
									if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING && !this.Achiev_orbit.Contains (FlightGlobals.ActiveVessel.mainBody.bodyName)) {
										this.Achiev_orbit.Add (FlightGlobals.ActiveVessel.mainBody.bodyName);
										if (this.Unlock_achievements) {
											MessageSystem.Instance.AddMessage (new MessageSystem.Message ("Simulate, Revert & Launch", "You can now make a simulation while <#8BED8B><b>in orbit around " + FlightGlobals.ActiveVessel.mainBody.bodyName + ".</b></>", MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.ALERT));
										}
										this.Save ();
									}
									if (FlightGlobals.ActiveVessel.Landed && !this.Achiev_land.Contains (FlightGlobals.ActiveVessel.mainBody.bodyName)) {
										this.Achiev_land.Add (FlightGlobals.ActiveVessel.mainBody.bodyName);
										if (this.Unlock_achievements) {
											MessageSystem.Instance.AddMessage (new MessageSystem.Message ("Simulate, Revert & Launch", "You can now make a simulation while <#8BED8B><b>landed on " + FlightGlobals.ActiveVessel.mainBody.bodyName + ".</b></>", MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.ALERT));
										}
										this.Save ();
									}
									if (Planetarium.GetUniversalTime() >= this.Time_FlightReady+5 && this.Time_FlightReady > 0 && this.N_plus1 && FlightGlobals.ActiveVessel.srfSpeed >= 0.1) {
										this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
										this.N_plus1 = false;
										this.Time_FlightReady = 0;
									}
								} else {
									if (Planetarium.GetUniversalTime() >= this.Time_FlightReady+5 && this.Time_FlightReady > 0 && this.N_plus1 && FlightGlobals.ActiveVessel.srfSpeed >= 0.1) {
										launch ();
									}
									if (GameSettings.QUICKSAVE.GetKeyDown () && HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad) {
										HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = true;
										print ("SRL" + VERSION + ": Quickload ON");
									}
								}
								if (!this.isPrelaunch) {
									double _double;
									if (this.last_time == 0) {
										this.last_time = Planetarium.GetUniversalTime ();
									}
									_double = Planetarium.GetUniversalTime () - this.last_time;
									if (_double > 60) {
										this.Simulation_duration += Convert.ToInt32 (_double);
										if (this.Simulation_fct_vessel) {
											this.price_factor_vessel += (1 + VesselCost / 100000) * _double;
										}
										if (this.Simulation_fct_body) {
											if (orbit) {
												this.price_factor_body += CelestialBodys_fct_price [CelestialBody * 2] * _double;
											} else {
												this.price_factor_body += CelestialBodys_fct_price [CelestialBody * 2+1] * _double;
											}
										}
										this.Save ();
										this.last_time = Planetarium.GetUniversalTime ();
									}
									if (isSimulate && this.useTimeCost && (!this.isFunded || !this.isFunded_sciences)) {
										this.Window_info = true;
									}
								}
								if (FlightGlobals.ActiveVessel.LandedOrSplashed && FlightGlobals.ActiveVessel.mainBody.name == "Kerbin") {
									this.Recovery_button = (AltimeterSliderButtons)GameObject.FindObjectOfType (typeof(AltimeterSliderButtons));
									if (isSimulate && this.Recovery_button.slidingTab.enabled) {
										this.Recovery_button.slidingTab.enabled = false;
										print ("SRL" + VERSION + ": Recovery locked");
									} else if (!isSimulate && !this.Recovery_button.slidingTab.enabled) {
										this.Recovery_button.slidingTab.enabled = true;
										print ("SRL" + VERSION + ": Recovery unlocked");
									}
								}
							}	
						} else {
							this.Simulation_pay ();
						}
					}
				}
			}
		}

		// Afficher l'activation de la simulation, le panneau d'information et le panneau de configuration
		public void OnGUI() {
			if (this.CanSimulate) {
				if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
					if (this.loading) {
						GUILayout.BeginArea (new Rect (0, 0, Screen.width, Screen.height), this.Text_Style_loading);
						GUILayout.Label (this.Text_loading, this.Text_Style_loading);
						GUILayout.EndArea ();
					}
					if (isSimulate || this.Index > 0) {
						string _message = "";
						if (this.isFunded && this.isFunded_sciences) {
							_message = this.Text_simulation;
						} else {
							if (!this.isFunded) {
								_message = this.Text_nofund;
							} else {
								_message = this.Text_nofund_sciences;
							}
							this.timer.Enabled = true;
							isSimulate = false;
							this.Index = _message.Length * 2;
						}
						int _int;
						if (HighLogic.LoadedSceneIsEditor) {
							_int = 255;
						} else {
							_int = 0;
						}
						GUILayout.BeginArea (new Rect (_int, (Screen.height / 10), Screen.width - _int, 160), this.Text_Style_simulate);
						GUILayout.Label (_message.Substring (0, (this.Index / 2)), this.Text_Style_simulate);
						GUILayout.EndArea ();
						if (!isSimulate && this.isFunded && this.isFunded_sciences) {
							this.Index--;
						} 
						if ((isSimulate && this.Index < (_message.Length * 2))) {
							this.Index++;
						}
					}
					if (this.Window_info) {
						int _height, _width, _guiheight, _guiwidth;
						GUI.skin = HighLogic.Skin;
						if (this.useSimulationCost) {
							if (HighLogic.LoadedSceneIsEditor) {
								_guiheight = 230;
								_guiwidth = 250;
								_height = Screen.height - (_guiheight + 40);
								_width = Screen.width - (_guiwidth + 70);
							} else {
								if (!this.isPrelaunch && HighLogic.LoadedSceneIsFlight && this.useTimeCost && (!this.isFunded || !this.isFunded_sciences)) {
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
						GUILayout.Window (554, new Rect (_width, _height, _guiwidth, _guiheight), this.DrawInfo, "Simulate, Revert & Launch", GUILayout.Width(_guiwidth), GUILayout.Height(_guiheight));
					}
					if (this.Window_simulate) {
						if (HighLogic.LoadedSceneIsEditor) {
							if (EditorLogic.fetch.launchBtn.controlIsEnabled) {
								int _height, _width, _guiheight, _guiwidth, _guiinfo_height;
								if (this.useSimulationCost) {
									_guiinfo_height = 230;
								} else {
									_guiinfo_height = 150;
								}
								_guiheight = 140;
								_guiwidth = 250;
								_height = Screen.height - (_guiheight + 40 + _guiinfo_height + 5);
								_width = Screen.width - (_guiwidth + 70);
								print (Mouse.screenPos.x+ " / "+ Mouse.screenPos.y);
								if (Mouse.screenPos.x > _width - Screen.width / 10 && Mouse.screenPos.y > _height - Screen.height / 10 && (Mouse.screenPos.x < Screen.width - 155 || Mouse.screenPos.y < Screen.height - 40)) {
									GUI.skin = HighLogic.Skin;
									GUILayout.Window (555, new Rect (_width, _height, _guiwidth, _guiheight), this.DrawSim, "Select the body to simulate:", GUILayout.Width (_guiwidth), GUILayout.Height (_guiheight));
								} else {
									InputLockManager.RemoveControlLock ("SRLeditor");
									this.Window_simulate = false;
									this.Window_info = false;
								}
							}
						}
					}
				}
			}
			if (this.Window_settings) {
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
					int _int;
					if (this.Button_isFalse) {
						this.Button.SetTrue ();
					}
					GUI.skin = HighLogic.Skin;
					if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
						_int = 520;
					} else if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) {
						_int = 450;
					} else {
						_int = 250;
					}
					GUILayout.Window (554, new Rect ((Screen.width -515), 40, 515, _int), this.DrawSettings, "Simulate, Revert & Launch v"+VERSION, GUILayout.Width(515), GUILayout.Height(_int));
				}
			}
		}

		// Fenêtre pour choisir le lieu de la simulation
		private void DrawSim(int id) {
			string _tmp;
			CelestialBody _body = this.CelestialBodys [CelestialBody];
			Vector2 _vector2 = new Vector2();
			GUILayout.BeginVertical ();
			GUILayout.BeginScrollView (_vector2, false, false);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("<", GUILayout.Width(20))) {
				if (CelestialBody <= 0) {
					CelestialBody = this.CelestialBodys.Length - 1;
				} else {
					CelestialBody--;
				}
				_body = this.CelestialBodys [CelestialBody];
				if (this.Unlock_achievements) {
					while (!this.Achiev_orbit.Contains (_body.bodyName) && !this.Achiev_land.Contains (_body.bodyName)) {
						if (CelestialBody <= 0) {
							CelestialBody = this.CelestialBodys.Length - 1;
						} else {
							CelestialBody--;
						}
						_body = this.CelestialBodys [CelestialBody];
					}
					if (!this.Achiev_orbit.Contains (_body.bodyName) && orbit) {
						orbit = false;
					} 
					if (!this.Achiev_land.Contains (_body.bodyName) && !orbit) {
						orbit = true;
					} 
				}
				if (_body.atmosphere) {
					double _atm_alt = Math.Round (_body.maxAtmosphereAltitude / 10000)*10;
					if (this.CelestialBodys_def_alt[CelestialBody] < _atm_alt && this.CelestialBodys_min_alt[CelestialBody] == -1 && _body.atmosphere) {
						altitude = _atm_alt;
						print ("SRL" + VERSION + ": ERROR IN THE DEFAULT ALTITUDE OF " + _body.name + ".");
					} else {
						altitude = this.CelestialBodys_def_alt [CelestialBody];
					}
				} else {
					altitude = this.CelestialBodys_def_alt [CelestialBody];
				}			}
			if (GUILayout.Button (_body.bodyName)) {
				EditorLogic.fetch.launchVessel ();
			}
			if (GUILayout.Button (">", GUILayout.Width(20))) {
				if (CelestialBody >= this.CelestialBodys.Length - 1) {
					CelestialBody = 0;
				} else {
					CelestialBody++;
				}
				_body = this.CelestialBodys [CelestialBody];
				if (this.Unlock_achievements) {
					while (!this.Achiev_orbit.Contains (_body.bodyName) && !this.Achiev_land.Contains (_body.bodyName)) {
						if (CelestialBody >= this.CelestialBodys.Length - 1) {
							CelestialBody = 0;
						} else {
							CelestialBody++;
						}
						_body = this.CelestialBodys [CelestialBody];
					}
					if (!this.Achiev_orbit.Contains (_body.bodyName) && orbit) {
						orbit = false;
					} 
					if (!this.Achiev_land.Contains (_body.bodyName) && !orbit) {
						orbit = true;
					} 
				}
				if (_body.atmosphere) {
					double _atm_alt = Math.Round (_body.maxAtmosphereAltitude / 10000)*10;
					if (this.CelestialBodys_def_alt[CelestialBody] < _atm_alt && this.CelestialBodys_min_alt[CelestialBody] == -1 && _body.atmosphere) {
						altitude = _atm_alt;
					} else {
						altitude = this.CelestialBodys_def_alt [CelestialBody];
					}
				} else {
					altitude = this.CelestialBodys_def_alt [CelestialBody];
				}
			}
			GUILayout.EndHorizontal ();
			if (!this.Unlock_achievements || this.Achiev_orbit.Contains (_body.bodyName)) {
				GUILayout.BeginHorizontal (); 
				orbit = GUILayout.Toggle (orbit, "In orbit", GUILayout.Width (90));
				GUILayout.Space (5);
				double _atm_alt = Math.Round (_body.maxAtmosphereAltitude / 10000)*10;
				if (altitude < _atm_alt && this.CelestialBodys_min_alt [CelestialBody] == -1 && _body.atmosphere) {
					altitude = _atm_alt;
				} else if (this.CelestialBodys_min_alt [CelestialBody] == -1 && !_body.atmosphere) {
					altitude = this.CelestialBodys_def_alt [CelestialBody];
					print ("SRL" + VERSION + ": ERROR IN THE MINIMUM ALTITUDE OF " + _body.name);
				} else if (altitude < this.CelestialBodys_min_alt [CelestialBody]) {
					altitude = this.CelestialBodys_min_alt [CelestialBody];
				} else if (altitude > Math.Round (((_body.sphereOfInfluence - _body.Radius) / 1000) - 1)) {
					altitude = Math.Round (((_body.sphereOfInfluence - _body.Radius) / 1000) - 1);
				}
				_tmp = GUILayout.TextField (altitude.ToString ());
				try {
					altitude = Convert.ToDouble (_tmp);
				} catch {
					if (_tmp == null) {
						altitude = this.CelestialBodys_min_alt [CelestialBody];
					} else {
						altitude = this.CelestialBodys_def_alt [CelestialBody];
					}
				}
				GUILayout.Space (5);
				GUILayout.Label ("km");
				GUILayout.EndHorizontal ();
			} else {
				orbit = false;
			}
			if (!this.Unlock_achievements || this.Achiev_land.Contains (_body.bodyName)) {
				GUILayout.BeginHorizontal ();
				if (this.CelestialBodys_land_pos [CelestialBody*2] != new Vector3d (-1, -1, -1)) {
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
			int _N = this.N_launch;
			if (this.N_plus1) {
				_N++;
			}
			GUILayout.BeginVertical ();
			Vector2 _vector2 = new Vector2();
			if (this.useCredits || this.useSciences) {
				if (!this.isFunded || !this.isFunded_sciences) {
					GUILayout.BeginScrollView (_vector2, false, false);
					_string = "<color=#FF0000><b>You have lost all your ";
					if (!this.isFunded) {
						_string += "credits";
						if (!this.isFunded_sciences) {
							_string += "and ";
						}
					} 
					if (!this.isFunded_sciences) {
						_string += "science";
					}
					_string += ", you can't continue this simulation</b></color>";
					GUILayout.Label (_string,GUILayout.Height(50));
					GUILayout.EndScrollView ();
					GUILayout.Space(5);
				}
			}
			GUILayout.BeginScrollView(_vector2, false, false);
			if (isSimulate && !this.isPrelaunch && HighLogic.LoadedSceneIsFlight && this.useTimeCost && (!this.isFunded || !this.isFunded_sciences)) {
				_string2 = "<color=#FFFFFF>In simulation mode, you did:</color>";
				_string = "The simulations have cost:";
			} else {
				_string2 = "<color=#FFFFFF>In simulation mode, you will:</color>";
				_string = "<color=#FFFFFF>The simulations will cost:</color>";
			}
			_string2 += "\n<color=#FFFFFF>make </color><color=#8BED8B><b>" + _N + "</b></color><color=#FFFFFF> launch(s),</color>";
			_string2 += "\n<color=#FFFFFF>use </color><color=#8BED8B><b>" + this.N_quickload + "</b></color><color=#FFFFFF> quickload(s),</color>";
			_string2 += "\n<color=#FFFFFF>spend </color><color=#8BED8B><b>" + Convert.ToInt32(this.Simulation_duration / (this.GetKerbinTime * 3600)) + "</b></color><color=#FFFFFF> day(s).</color>";
			GUILayout.Label (_string2, this.Text_Style_info,GUILayout.Height(80));
			GUILayout.EndScrollView ();
			if (this.useSimulationCost) {
				GUILayout.BeginScrollView (_vector2, false, false);
				if (this.useCredits) {
					_string += "\n<color=#FFFFFF>credits: </color><color=#B4D455><b>" + this.CreditsCost + "</b></color><color=#FFFFFF>.</color>";
				}
				if (this.useReputations) {
					_string += "\n<color=#FFFFFF>reputation: </color><color=#E0D503><b>" + this.ReputationsCost + "</b></color><color=#FFFFFF>.</color>";
				}
				if (this.useSciences) {
					_string += "\n<color=#FFFFFF>science: </color><color=#6DCFF6><b>" + this.SciencesCost + "</b></color><color=#FFFFFF>.</color>";
				}
				GUILayout.Label (_string, this.Text_Style_info, GUILayout.Height (80));
				GUILayout.EndScrollView ();
			}
			GUILayout.Space(5);
			if (!this.isPrelaunch && HighLogic.LoadedSceneIsFlight && this.useTimeCost && (!this.isFunded || !this.isFunded_sciences)) {
				if (!FlightDriver.Pause) {
					FlightDriver.SetPause (true);
					this.Save ();
				}
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Go to the space center.", GUILayout.Height (30))) {
					this.Window_info = false;
					FlightDriver.SetPause (false);
					FlightDriver.RevertToPrelaunch (GameScenes.EDITOR);
					//HighLogic.LoadScene (GameScenes.SPACECENTER);
					//GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, false, false);
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
			this.enable = GUILayout.Toggle(this.enable,new GUIContent("Enable Simulate, Revert & Launch"));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			GUILayout.Box("Difficulty",GUILayout.Height(30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.ironman = GUILayout.Toggle(this.ironman,"Ironman (hardmode)", GUILayout.Width(235));
			GUILayout.Space(5);
			this.simulate = GUILayout.Toggle(this.simulate,"Simulate", GUILayout.Width(200));
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
					this.Credits = GUILayout.Toggle (this.Credits, "Simulations will cost credits", GUILayout.Width (250));
					_tmp = GUILayout.TextField (this.Cost_credits.ToString());
					try {
						this.Cost_credits = Convert.ToInt32(_tmp);
					} catch {
						this.Cost_credits = 1000;
					}
					GUILayout.EndHorizontal ();
					GUILayout.Space (5);
					GUILayout.BeginHorizontal ();
					this.Reputations = GUILayout.Toggle (this.Reputations, "Simulations will cost reputation", GUILayout.Width (250));
					_tmp = GUILayout.TextField (this.Cost_reputations.ToString());
					try {
						this.Cost_reputations = Convert.ToInt32(_tmp);
					} catch {
						this.Cost_reputations = 50;
					}
					GUILayout.EndHorizontal ();
					GUILayout.Space (5);
				}
				GUILayout.BeginHorizontal ();
				this.Sciences = GUILayout.Toggle (this.Sciences, "Simulations will cost science", GUILayout.Width (250));
				_tmp = GUILayout.TextField (this.Cost_sciences.ToString());
				try {
					this.Cost_sciences = Convert.ToInt32(_tmp);
				} catch {
					this.Cost_sciences = 20;
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				GUILayout.BeginHorizontal ();
				this.Simulation_fct_duration = GUILayout.Toggle (this.Simulation_fct_duration, "The time passed in simulation will cost credit, reputation or science.", GUILayout.Width (400));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				GUILayout.BeginHorizontal ();
				this.Simulation_fct_reputations = GUILayout.Toggle (this.Simulation_fct_reputations, "The amount of costs is influenced by the reputation.", GUILayout.Width (400));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				GUILayout.BeginHorizontal ();
				this.Simulation_fct_body = GUILayout.Toggle (this.Simulation_fct_body, "The amount of costs is influenced by the celestial body.", GUILayout.Width (400));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				GUILayout.BeginHorizontal ();
				this.Simulation_fct_vessel = GUILayout.Toggle (this.Simulation_fct_vessel, "The amount of costs is influenced by the price of the vessel.", GUILayout.Width (400));
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
			}
			GUILayout.BeginHorizontal();
			GUILayout.Box("Unlock the simulations",GUILayout.Height(30));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.Unlock_achievements = !GUILayout.Toggle (!this.Unlock_achievements, "All unlocked", GUILayout.Width (235));
			GUILayout.Space(5);
			this.Unlock_achievements = GUILayout.Toggle (this.Unlock_achievements, "Unlocked with achievements", GUILayout.Width (200));
			GUILayout.EndHorizontal();
			if (GUILayout.Button ("Close",GUILayout.Height(30))) {
				if (ApplicationLauncher.Ready && this.Button_isTrue) {
					this.Button.SetFalse ();
				}
			}
			GUILayout.EndVertical();
			if (!this.enable) {
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
			}
		}

		// Sauvegarde des paramètres
		public void Save() {
			ConfigNode _temp = ConfigNode.CreateConfigFromObject(this, new ConfigNode());
			_temp.Save(this.Path_settings + HighLogic.SaveFolder + "-config.txt");
			print ("SRL" + VERSION + ": Save");
		}

		// Charger les variables
		private void OnGameStateLoad (ConfigNode confignode) {
			this.Load ();
		}

		// Chargement des paramètres
		public void Load() {
			if (System.IO.File.Exists (this.Path_settings + HighLogic.SaveFolder + "-config.txt")) {
				ConfigNode _temp = ConfigNode.Load (this.Path_settings + HighLogic.SaveFolder + "-config.txt");
				ConfigNode.LoadObjectFromConfig (this, _temp);
				print ("SRL" + VERSION + ": Load");
				if (this.VERSION_config != VERSION) {
					this.Reset ();
				}
				if (this.Achiev_land.Count <= 1) {
					this.Achiev_land = new List<string> { "Kerbin" };
				}
			} else {
				this.Reset ();
			}
		}
		public void Reset() {
			this.VERSION_config = VERSION;
			if (!System.IO.File.Exists (this.Path_settings + HighLogic.SaveFolder + "-config.txt")) {
				this.enable = true;
				this.ironman = true;
				this.simulate = true;
				this.Credits = true;
				this.Reputations = true;
				this.Sciences = false;
				this.Simulation_fct_duration = true;
				this.Simulation_fct_reputations = true;
				this.Simulation_fct_body = false;
				this.Simulation_fct_vessel = true;
				this.Unlock_achievements = true;
				if (this.Achiev_land.Count <= 1) {
					this.Achiev_land = new List<string> { "Kerbin" };
				}
			}
			if (this.Cost_credits <= 0) {
				this.Cost_credits = 1000;
			}
			if (this.Cost_reputations <= 0) {
				this.Cost_reputations = 50;
			}
			if (this.Cost_sciences <= 0) {
				this.Cost_sciences = 20;
			}
			this.price_factor_body = 0;
			this.price_factor_vessel = 0;
			this.N_launch = 0;
			this.N_quickload = 0;
			this.Simulation_duration = 0;
			print ("SRL" + VERSION + ": Reset");
			this.Save ();
		}
	}
}