//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//
// modified by Malah for Simulate, Revert & Launch
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SRL
{
	public class HyperEdit_fct
	{
		public static void Set(Orbit orbit, Orbit newOrbit)
		{
			var vessel = FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
			var body = FlightGlobals.fetch == null ? null : FlightGlobals.Bodies.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
			if (vessel != null)
				WarpShip(vessel, newOrbit);
			else if (body != null)
				WarpPlanet(body, newOrbit);
			else
				HardsetOrbit(orbit, newOrbit);
		}

		private static void WarpShip(Vessel vessel, Orbit newOrbit)
		{
			if (newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude > newOrbit.referenceBody.sphereOfInfluence)
			{
				//ErrorPopup.Error("Destination position was above the sphere of influence");
				return;
			}

			vessel.Landed = false;
			vessel.Splashed = false;
			vessel.landedAt = string.Empty;
			var parts = vessel.parts;
			if (parts != null)
			{
				var clamps = parts.Where(p => p.Modules != null && p.Modules.OfType<LaunchClamp>().Any()).ToList();
				foreach (var clamp in clamps)
					clamp.Die();
			}

			try
			{
				OrbitPhysicsManager.HoldVesselUnpack(60);
			}
			catch (NullReferenceException)
			{
			}

			foreach (var v in (FlightGlobals.fetch == null ? (IEnumerable<Vessel>)new[] { vessel } : FlightGlobals.Vessels).Where(v => v.packed == false))
				v.GoOnRails();

			HardsetOrbit(vessel.orbit, newOrbit);

			vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
			vessel.orbitDriver.vel = vessel.orbit.vel;
		}

		private static void WarpPlanet(CelestialBody body, Orbit newOrbit)
		{
			var oldBody = body.referenceBody;
			HardsetOrbit(body.orbit, newOrbit);
			if (oldBody != newOrbit.referenceBody)
			{
				oldBody.orbitingBodies.Remove(body);
				newOrbit.referenceBody.orbitingBodies.Add(body);
			}
			body.CBUpdate();
		}

		private static void HardsetOrbit(Orbit orbit, Orbit newOrbit)
		{
			orbit.inclination = newOrbit.inclination;
			orbit.eccentricity = newOrbit.eccentricity;
			orbit.semiMajorAxis = newOrbit.semiMajorAxis;
			orbit.LAN = newOrbit.LAN;
			orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
			orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
			orbit.epoch = newOrbit.epoch;
			orbit.referenceBody = newOrbit.referenceBody;
			orbit.Init();
			orbit.UpdateFromUT(Planetarium.GetUniversalTime());
		}
		public static Orbit CreateOrbit(double inc, double e, double sma, double lan, double w, double mEp, double epoch, CelestialBody body)
		{
			if (double.IsNaN(inc))
				inc = 0;
			if (double.IsNaN(e))
				e = 0;
			if (double.IsNaN(sma))
				sma = body.Radius + body.maxAtmosphereAltitude + 10000;
			if (double.IsNaN(lan))
				lan = 0;
			if (double.IsNaN(w))
				w = 0;
			if (double.IsNaN(mEp))
				mEp = 0;
			if (double.IsNaN(epoch))
				mEp = Planetarium.GetUniversalTime();

			if (Math.Sign(e - 1) == Math.Sign(sma))
				sma = -sma;

			if (Math.Sign(sma) >= 0)
			{
				while (mEp < 0)
					mEp += Math.PI * 2;
				while (mEp > Math.PI * 2)
					mEp -= Math.PI * 2;
			}

			return new Orbit(inc, e, sma, lan, w, mEp, epoch, body);
		}
	}
}