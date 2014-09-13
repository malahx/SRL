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
using System.Timers;
using UnityEngine;

namespace SRL {

	[KSPAddon(KSPAddon.Startup.EditorAny | KSPAddon.Startup.TrackingStation | KSPAddon.Startup.Flight | KSPAddon.Startup.SpaceCentre, false)]
	public class SRL : MonoBehaviour {
	
		// Initialiser les variables
		public const string VERSION = "1.00";
		private Texture Button_texture_sim = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/sim", false);
		private Texture Button_texture_srl = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/srl", false);
		private Texture Button_texture_insim = (Texture)GameDatabase.Instance.GetTexture ("SRL/Textures/insim", false);
		private String Path_settings = KSPUtil.ApplicationRootPath + "GameData/SRL/PluginData/SRL/";
		private string Text_simulation = "SIMULATION";
		private string Text_nofund = "You need more credits to make a simulation";
		private string Text_nofund_sciences = "You need more science to make a simulation";

		// Variables sauvegardées par session
		[KSPField(isPersistant = true)]
		public static bool isSimulate = false;
		[KSPField(isPersistant = true)]
		private static GameBackup Save_PostInitState;
		[KSPField(isPersistant = true)]
		private static GameBackup Save_PreLaunchState;
		[KSPField(isPersistant = true)]
		private static Game Save_FlightStateCache;

		// Variables temporaires
		private ApplicationLauncherButton Button;
		private GUIStyle Text_Style_simulate;
		private GUIStyle Text_Style_info;
		private VesselRecoveryButton Recovery_button = null;
		private int Index = 0;
		private bool Window_settings = false;
		private bool Window_info = false;
		private bool last_isSimulate = !isSimulate;
		private double last_time = 0;
		private bool N_plus1 = false;
		private Timer timer = new Timer(10000);

		// Variables sauvegardées par parties
		[Persistent]
		public bool enable;
		[Persistent]
		public bool ironman;
		[Persistent]
		public bool simulate;
		[Persistent]
		public bool Credits;
		[Persistent]
		public bool Credits_fct_reputations;
		[Persistent]
		public bool Sciences;
		[Persistent]
		public bool Reputations;
		[Persistent]
		public bool Simulation_cost_duration;
		[Persistent]
		public int Cost_credits;
		[Persistent]
		public int Cost_reputations;
		[Persistent]
		public int Cost_sciences;
		[Persistent]
		public int N_launch;
		[Persistent]
		public int N_quickload;
		[Persistent]
		public int Simulation_duration;

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
				return this.Simulation_cost_duration && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);
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
					if (this.N_plus1) {
						_N++;
					}
					double _credits = 0;
					if (this.Credits) {
						_credits -= this.Cost_credits * _N + (this.Cost_credits / 2 * this.N_quickload);
					}
					if (this.Simulation_cost_duration) {
						_credits -= this.Cost_credits / 20 * (this.Simulation_duration / (this.GetKerbinTime * 3600));
					}
					if (this.Credits_fct_reputations) {
						_credits *= (1 - Reputation.UnitRep / 2);
					}
					return Convert.ToSingle(Math.Round(_credits));
				} else {
					return 0;
				}
			}
		}
		private float ReputationsCost {
			get {
				if (this.useReputations) {
					int _N = this.N_launch;
					if (this.N_plus1) {
						_N++;
					}
					double _reputations = 0;
					if (this.Reputations) {
						_reputations-= this.Cost_reputations * _N + (this.Cost_credits / 2 * this.N_quickload);
					}
					if (this.Simulation_cost_duration) {
						_reputations -= this.Cost_reputations / 20 * (this.Simulation_duration / (this.GetKerbinTime * 3600));
					}
					if (this.Credits_fct_reputations) {
						_reputations *= (1 - Reputation.UnitRep / 2);
					}
					return Convert.ToSingle(Math.Round(_reputations));
				} else {
					return 0;
				}
			}
		}
		private float SciencesCost {
			get {
				if (this.useSciences) {
					int _N = this.N_launch;
					if (this.N_plus1) {
						_N++;
					}
					double _sciences = 0;
					if (this.Sciences) {
						_sciences -= this.Cost_sciences * _N + (this.Cost_sciences / 2 * this.N_quickload);
					}
					if (this.Simulation_cost_duration) {
						_sciences -= this.Cost_sciences / 20 * (this.Simulation_duration / (this.GetKerbinTime * 3600));
					}
					if (this.Credits_fct_reputations) {
						_sciences *= (1 - Reputation.UnitRep / 2);
					}
					return Convert.ToSingle(Math.Round(_sciences));
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
			GameEvents.onGameStateSaved.Add (OnGameStateSaved);
			GameEvents.onFlightReady.Add (OnFlightReady);
			GameEvents.onCrewOnEva.Add (OnCrewOnEva);
			GameEvents.onGameStateLoad.Add (OnGameStateLoad);
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
			this.timer.Elapsed += new ElapsedEventHandler(OnTimer);
		}

		// Afficher le bouton de simulation
		private void OnGUIApplicationLauncherReady() {
			if (ApplicationLauncher.Ready) {
				this.Button = ApplicationLauncher.Instance.AddModApplication (this.Button_On, this.Button_Off, this.Button_OnHover, this.Button_OnHoverOut, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.MAPVIEW, this.Button_texture_srl);
				if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION) {
					isSimulate = false;
				}
				if (!this.CanSimulate) {
					this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER;
				} else if (HighLogic.LoadedSceneIsEditor) {
					this.Button.SetTexture(this.Button_texture_sim);
				}
			}
		}

		// Bloquer l'accès au bouton de simulation après le lancement de la fusée
		private void OnLaunch(EventReport EventReport) {
			if (this.CanSimulate) {
				if (isSimulate) {
					if (ApplicationLauncher.Ready) {
						this.Button.SetTexture (this.Button_texture_insim);
						if (this.Button_isTrue) {
							this.Button.SetFalse ();
							isSimulate = true;
						}
					}
					if (this.useCredits) {
						Funding.Instance.Funds += Convert.ToInt32 (this.VesselCost);
					}
					this.last_time = Planetarium.GetUniversalTime ();
					this.N_plus1 = false;
					this.N_launch++;
					this.Save ();
				} else {
					if (ApplicationLauncher.Ready) {
						this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
					}
				}				
			}
		}

		// Activer le quickload après un quicksave
		private void OnGameStateSaved(Game game) {
			if (this.CanSimulate && isSimulate && HighLogic.LoadedSceneIsFlight) {
				HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad = true;
				print ("SRL"+VERSION+": Quickload ON");
			}
		}

		// Charger les variables
		private void OnGameStateLoad (ConfigNode confignode) {
			this.Load ();
		}

		// Activer le revert après un quickload
		private void OnFlightReady() {
			if (this.CanSimulate) {
				if (this.isPrelaunch) {
					this.Button.SetTexture (this.Button_texture_sim);
					try { 
						Save_FlightStateCache = FlightDriver.FlightStateCache;
					} catch {
						print ("SRL" + VERSION + ": No FlightStateCache !");
					}
					try { 
						Save_PostInitState = FlightDriver.PostInitState;
					} catch {
						print ("SRL" + VERSION + ": No PostInitState !");
					}
					try { 
						Save_PreLaunchState = FlightDriver.PreLaunchState;
					} catch {
						print ("SRL" + VERSION + ": No PreLaunchState !");
					}

					print ("SRL" + VERSION + ": Revert saved");
				} else if (isSimulate) {
					this.Button.SetTexture (this.Button_texture_insim);
					this.N_quickload++;
					this.Save ();
					try { 
						FlightDriver.FlightStateCache = Save_FlightStateCache;
					} catch {
						print ("SRL" + VERSION + ": No FlightStateCache !");
					}
					try { 
						FlightDriver.PostInitState = Save_PostInitState;
					} catch {
						print ("SRL" + VERSION + ": No PostInitState !");
					}
					try { 
						FlightDriver.PreLaunchState = Save_PreLaunchState;
					} catch {
						print ("SRL" + VERSION + ": No PreLaunchState !");
					}
					print ("SRL" + VERSION + ": Revert loaded");
				}
			}
		}

		// Supprimer le bouton de simulation à l'EVA
		private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> part) {
			if (this.CanSimulate && part.from.vessel.situation == Vessel.Situations.PRELAUNCH && ApplicationLauncher.Ready) {
				this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
				isSimulate = false;
			}
		}

		// Supprimer le bouton de simulation et les évènements
		private void OnDestroy() {
			GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
			GameEvents.onLaunch.Remove (OnLaunch);
			GameEvents.onGameStateSaved.Remove (OnGameStateSaved);
			GameEvents.onFlightReady.Remove (OnFlightReady);
			GameEvents.onCrewOnEva.Remove (OnCrewOnEva);
			GameEvents.onGameStateLoad.Remove (OnGameStateLoad);
			if (this.Button != null) {
				ApplicationLauncher.Instance.RemoveModApplication (this.Button);
				this.Button = null;
			}
		}

		// Effacer certains messages temporaires
		private void OnTimer(object sender, ElapsedEventArgs e) {
			this.Index = 0;
			print ("SRL" + VERSION + ": Message off");
		}

		// Activer le bouton
		private void Button_On() {
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
				this.Window_settings = true;
				InputLockManager.SetControlLock(ControlTypes.KSC_FACILITIES, "SRLkscfacilities");
			} else {
				if (isSimulate && !this.isPrelaunch && HighLogic.LoadedSceneIsFlight && this.isFunded && this.isFunded_sciences) {
					this.Window_info = true;
				} else {
					isSimulate = true;
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
				if (isSimulate && !this.isPrelaunch && HighLogic.LoadedSceneIsFlight) {
					this.Window_info = false;
				} else {
					isSimulate = false;
				}
			}
		}

		// Passer la souris sur le bouton
		private void Button_OnHover() {
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor) {
				if (isSimulate && this.isFunded && this.isFunded_sciences) {
					this.Window_info = true;
				}
			}
		}

		// Enlever la souris du bouton
		private void Button_OnHoverOut() {
			if ((isSimulate && this.Button_isFalse && HighLogic.LoadedSceneIsFlight) || this.isPrelaunch || HighLogic.LoadedSceneIsEditor) {
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
		private void Simulation_off() {
			if (ApplicationLauncher.Ready) {
				if (this.Button_isTrue && !this.Window_settings) {
					this.Button.SetFalse ();
				}
				if (HighLogic.LoadedSceneIsFlight) {
					if (!this.isPrelaunch) {
						this.Button.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
					}
					if (this.isIronman) {
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
			//InputLockManager.RemoveControlLock ("SRLevainput");
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
					this.N_plus1 = false;
					string _string;
					_string = "<b>In simulation mode, you did:\nmake <#8BED8B>" + this.N_launch + "</> launch(s),\nuse <#8BED8B>" + this.N_quickload + "</> quickload(s),\nspend <#8BED8B>" + Convert.ToInt32 (this.Simulation_duration / (this.GetKerbinTime * 3600)) + "</> day(s).</>\n\nThe simulations have cost:";
					float _float;
					if (this.useCredits) {
						_float = this.CreditsCost;
						_string += " <#B4D455>£" + _float + "</>";
						Funding.Instance.Funds += _float;
						if (this.useReputations && this.useSciences) {
							_string += ", ";
						} else if (this.useReputations || this.useSciences) {
							_string += " and";
						}
					}
					if (this.useReputations) {
						_float = this.ReputationsCost;
						_string += " <#E0D503>¡" + _float + "</>";
						Reputation.Instance.AddReputation (_float, "Simulation");
						if (this.useSciences) {
							_string += " and";
						}
					}
					if (this.useSciences) {
							_float = this.SciencesCost;
						_string += " <#6DCFF6>¡" + _float + "</>";
						ResearchAndDevelopment.Instance.Science += _float;
					}
					_string += ".";
					MessageSystem.Instance.AddMessage (new MessageSystem.Message ("Simulation ended", _string, MessageSystemButton.MessageButtonColor.ORANGE, MessageSystemButton.ButtonIcons.ALERT));
					this.N_launch = 0;
					this.N_quickload = 0;
					this.Simulation_duration = 0;
					this.Save ();
					if (ResearchAndDevelopment.Instance.Science < 0) {
						ResearchAndDevelopment.Instance.Science = 0;
					}
				} else {
					this.N_launch = 0;
					this.N_quickload = 0;
					this.Simulation_duration = 0;
					this.Save ();
				}
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
						if (!this.isPrelaunch && HighLogic.LoadedSceneIsFlight && this.N_plus1) {
							this.N_plus1 = false;
						} else if ((this.isPrelaunch || HighLogic.LoadedSceneIsEditor) && !this.N_plus1) {
							this.N_plus1 = true;
						}
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
								if (!this.isPrelaunch) {
									double _double;
									if (this.last_time == 0) {
										this.last_time = Planetarium.GetUniversalTime ();
									}
									_double = Planetarium.GetUniversalTime () - this.last_time;
									if (_double > 60) {
										this.Simulation_duration += Convert.ToInt32 (_double);
										this.Save ();
										this.last_time = Planetarium.GetUniversalTime ();
									}
									if (isSimulate && this.useTimeCost && (!this.isFunded || !this.isFunded_sciences)) {
										this.Window_info = true;
									}
								}
								if (FlightGlobals.ActiveVessel.LandedOrSplashed && FlightGlobals.ActiveVessel.mainBody.name == "Kerbin") {
									this.Recovery_button = (VesselRecoveryButton)GameObject.FindObjectOfType (typeof(VesselRecoveryButton));
									if (isSimulate && this.Recovery_button.slidingTab.toggleMode != ScreenSafeUISlideTab.ToggleMode.EXTERNAL) {
										this.Recovery_button.slidingTab.toggleMode = ScreenSafeUISlideTab.ToggleMode.EXTERNAL;
										this.Recovery_button.slidingTab.Collapse ();
										this.Recovery_button.ssuiButton.Lock ();
										print ("SRL" + VERSION + ": Recovery locked");
									} else if (!isSimulate && this.Recovery_button.slidingTab.toggleMode == ScreenSafeUISlideTab.ToggleMode.EXTERNAL) {
										this.Recovery_button = (VesselRecoveryButton)GameObject.FindObjectOfType (typeof(VesselRecoveryButton));
										this.Recovery_button.slidingTab.toggleMode = ScreenSafeUISlideTab.ToggleMode.HOVER;
										this.Recovery_button.ssuiButton.Unlock ();
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
						_int = 420;
					} else if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) {
						_int = 280;
					} else {
						_int = 210;
					}
					GUILayout.Window (554, new Rect ((Screen.width -515), 40, 515, _int), this.DrawSettings, "Simulate, Revert & Launch v"+VERSION, GUILayout.Width(515), GUILayout.Height(_int));
				}
			}
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
					HighLogic.LoadScene (GameScenes.SPACECENTER);
					GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, false, false);
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
			HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = GUILayout.Toggle(HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels,"Allow Stock Vessels", GUILayout.Width(200));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			this.simulate = GUILayout.Toggle(this.simulate,"Simulate", GUILayout.Width(235));
			GUILayout.Space(5);
			HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn = GUILayout.Toggle(HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn,"Missing Crews Respawn", GUILayout.Width(200));
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
						this.Cost_reputations = 100;
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
					this.Cost_sciences = 50;
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
					GUILayout.BeginHorizontal ();
					this.Simulation_cost_duration = GUILayout.Toggle (this.Simulation_cost_duration, "The time passed in simulation will cost credit, reputation or science.", GUILayout.Width (400));
					GUILayout.EndHorizontal ();
					GUILayout.Space (5);
					GUILayout.BeginHorizontal ();
					this.Credits_fct_reputations = GUILayout.Toggle (this.Credits_fct_reputations, "The amount of costs is influenced by the reputation.", GUILayout.Width (400));
					GUILayout.EndHorizontal ();
					GUILayout.Space (5);
				}
			}
			GUILayout.EndVertical();
			if (GUILayout.Button ("Close",GUILayout.Height(30))) {
				if (ApplicationLauncher.Ready && this.Button_isTrue) {
					this.Button.SetFalse ();
				}
			}
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

		// Chargement des paramètres
		public void Load() {
			if (System.IO.File.Exists (this.Path_settings + HighLogic.SaveFolder + "-config.txt")) {
				ConfigNode _temp = ConfigNode.Load (this.Path_settings + HighLogic.SaveFolder + "-config.txt");
				ConfigNode.LoadObjectFromConfig (this, _temp);
				print ("SRL" + VERSION + ": Load");
			} else {
				this.enable = true;
				this.ironman = true;
				this.simulate = true;
				this.Credits = true;
				this.Credits_fct_reputations = true;
				this.Reputations = true;
				this.Sciences = false;
				this.Simulation_cost_duration = true;
				this.Cost_credits = 1000;
				this.Cost_reputations = 50;
				this.Cost_sciences = 20;
				this.N_launch = 0;
				this.N_quickload = 0;
				this.Simulation_duration = 0;
				this.Save ();
			}
		}
	}
}