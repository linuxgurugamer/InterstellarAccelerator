#if false
using System.Collections.Generic;
using UnityEngine;

namespace NeatherdyneMassDriver
{
	public class StructuralIntegrityField : PartModule
	{
		[KSPField(isPersistant = false, guiActive = false)]
		public int fieldStrength = 40000000;
		[KSPField(isPersistant = false, guiActive = false)]
		public float fieldRange = 2500;

		public bool sizeState = false; //false means full size
		//bool oneshotPrint = false;

		public override void OnStart(PartModule.StartState state)
		{
			EditorLogic logic = EditorLogic.fetch;

			if (logic != null)
			{
				Log.Info("found editor logic!");

				Log.Info(logic.ship[0].scaleFactor.ToString());

				//logic.ship;
			}
			base.OnStart(state);
		}

		[KSPEvent(guiName = "Editing mode...", guiActive = false, guiActiveEditor = true)]
		public void scaleMe()
		{

			sizeState = !sizeState;
			EditorLogic logic = EditorLogic.fetch;
			if (logic != null)
			{
				if (this.part != logic.ship.parts[0])
				{
					Log.Info("Part must be root for editing mode.");
					return;
				}
				Log.Info("found editor logic!");

				Log.Info(logic.ship[0].scaleFactor.ToString());

				if (sizeState)
					part.partTransform.localScale = new Vector3(.02f, .02f, .02f);
				else
					part.partTransform.localScale = new Vector3(1f, 1f, 1f);
				for (int i = 0; i < logic.ship.parts.Count; i ++)
				{
					Part p = logic.ship.parts[i];
					if (sizeState)
					{
						if (vessel != null)
						{
							Log.Info("found vessel!");
							//vessel.vesselTransform.localScale *= .02f;

						}
						// p.transform.localScale *= .02f;
						if (p == this.part)
							continue;

						//todo: scale the attach nodes too
						for (int i1 = 0; i1 < p.attachNodes.Count; i++)
							p.attachNodes[i1].position *= 0.2f;

						var lightsOnModel = p.FindModelComponents<Light>();
						if (lightsOnModel != null)
							for (int i2 = 0; i2 < lightsOnModel.Count; i2++)
								lightsOnModel[i2].enabled =false;
							//foreach (Light l in lightsOnModel)
							//	l.enabled = false;

						p.srfAttachNode.position *= .02f;
					}
					else
					{
						// p.transform.localScale *= 100.0f;
						Log.Info(p.rescaleFactor.ToString());
						if (p == this.part)
						{

							continue;
						}
						// if (p.rescaleFactor != 1)
						//  p.partTransform.localScale = new Vector3(50.0f, 50.0f, 50.0f);
						// else
						//p.partTransform.localScale = new Vector3(1.0f,1.0f,1.0f);
						p.rescaleFactor = 1;
						p.srfAttachNode.position *= 50.0f;
						for (int i3 = 0; i3 < p.attachNodes.Count; i3++)
							p.attachNodes[i3].position *= 50.0f;
						//foreach (AttachNode node in p.attachNodes)
						//{
						//	Log.Info(node.position.ToString());
						//	node.position *= 50.0f;
						//
						//}
						logic.saveBtn.enabled = true;
					}
				}
				//logic.ship;
			}
		}

		public override void OnActive()
		{

			var partlist = this.vessel.GetActiveParts();
			for (int i = 0; i < partlist.Count; i++)
			{

				Part p = partlist[i];

				Log.Info(p.name + " " + p.crashTolerance + " " + p.breakingForce + " " + p.breakingTorque);
				//Log.Info(p.collider.active);
				Log.Info(p.attachMethod.ToString());
				p.crashTolerance = fieldStrength;
				p.breakingForce = fieldStrength;
				p.breakingTorque = fieldStrength;
				// p.attachMethod = AttachNodeMethod.FIXED_JOINT;
			}
			var vesselList = FlightGlobals.Vessels;
#if DEBUG
			foreach (Vessel v in vesselList)
			{
				Log.Info(v.RevealName());
				Log.Info(v.vesselRanges.ToString());
				Log.Info(v.vesselRanges.ToString());
				//Log.Info("v.vesselRanges: " + v.vesselRanges.)
			}
#endif
			base.OnActive();
			//oneshotPrint = true;
		}

		void setNoRailsArea()
		{
			var vesselList = FlightGlobals.Vessels;
			var myloc = vessel.GetWorldPos3D();
			// if(oneshotPrint)
			// {
			//    Log.Info(Vessel.unloadDistance);
			//    oneshotPrint = false;
			//}
			// Vessel.unloadDistance = 120000;
			// Vessel.loadDistance = 120000;
			for (int i = 0; i < vesselList.Count; i++)
			{
				var theirloc = vesselList[i].GetWorldPos3D();
				if (Vector3.Distance(myloc, theirloc) < fieldRange)
					vesselList[i].vesselRanges.GetSituationRanges(Vessel.Situations.LANDED).unpack = fieldRange; // distanceLandedUnpackThreshold
			}

			foreach (Vessel v in vesselList)
			{
				// Log.Info(v.RevealName());
				//Log.Info(v.packed);
				//Log.Info(v.distancePackThreshold);
				// Log.Info(v.distanceUnpackThreshold);

				var theirloc = v.GetWorldPos3D();
				if (Vector3.Distance(myloc, theirloc) < fieldRange)
				{
					v.vesselRanges.GetSituationRanges(Vessel.Situations.FLYING).unpack = fieldRange; // distanceUnpackThreshold
				}
			}
		}

		public override void OnFixedUpdate()
		{
			setNoRailsArea();
			base.OnFixedUpdate();
		}

		//every part attached to the vessel with this module gets a crash tolerance of fieldStrength, and breaking force/torque to match
		public override void OnUpdate()
		{
			setNoRailsArea();
			base.OnUpdate();
		}
	}
}

#endif