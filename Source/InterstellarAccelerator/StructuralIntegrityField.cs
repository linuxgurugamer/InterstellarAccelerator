using UnityEngine;

namespace InterstellarAccelerator
{
	public class StructuralIntegrityField : PartModule
	{
		[KSPField(isPersistant = false, guiActive = false)]
		public int fieldStrength = 40000000;
		[KSPField(isPersistant = false, guiActive = false)]
		public float fieldRange = 2500;

		public bool sizeState = false; //false means full size
		bool oneshotPrint = false;

		public override void OnStart(PartModule.StartState state)
		{
			EditorLogic logic = EditorLogic.fetch;

			if (logic != null)
			{
				Debug.Log("found editor logic!");

				Debug.Log(logic.ship[0].scaleFactor);

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
					Debug.Log("Part must be root for editing mode.");
					return;
				}
				Debug.Log("found editor logic!");

				Debug.Log(logic.ship[0].scaleFactor);

				if (sizeState)
					part.partTransform.localScale = new Vector3(.02f, .02f, .02f);
				else
					part.partTransform.localScale = new Vector3(1f, 1f, 1f);

				foreach (Part p in logic.ship.parts)
				{
					if (sizeState)
					{
						if (vessel != null)
						{
							Debug.Log("found vessel!");
							//vessel.vesselTransform.localScale *= .02f;

						}
						// p.transform.localScale *= .02f;
						if (p == this.part)
							continue;

						//todo: scale the attach nodes too
						foreach (AttachNode node in p.attachNodes)
						{
							//node.size = 1;
							node.position *= .02f;
						}
						var lightsOnModel = p.FindModelComponents<Light>();
						if (lightsOnModel != null)
							foreach (Light l in lightsOnModel)
								l.enabled = false;

						p.srfAttachNode.position *= .02f;
					}
					else
					{
						// p.transform.localScale *= 100.0f;
						Debug.Log(p.rescaleFactor);
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
						foreach (AttachNode node in p.attachNodes)
						{
							Debug.Log(node.position);
							node.position *= 50.0f;

						}
						logic.saveBtn.enabled = true;
					}
				}
				//logic.ship;
			}
		}

		public override void OnActive()
		{

			var partlist = this.vessel.GetActiveParts();
			foreach (Part p in partlist)
			{
				print(p.name + " " + p.crashTolerance + " " + p.breakingForce + " " + p.breakingTorque);
				//print(p.collider.active);
				print(p.attachMethod);
				p.crashTolerance = fieldStrength;
				p.breakingForce = fieldStrength;
				p.breakingTorque = fieldStrength;
				// p.attachMethod = AttachNodeMethod.FIXED_JOINT;
			}
			var vesselList = FlightGlobals.Vessels;
			foreach (Vessel v in vesselList)
			{
				print(v.RevealName());
				print(v.distancePackThreshold);
				print(v.distanceUnpackThreshold);
			}
			base.OnActive();
			oneshotPrint = true;
		}

		void setNoRailsArea()
		{
			var vesselList = FlightGlobals.Vessels;
			var myloc = vessel.GetWorldPos3D();
			// if(oneshotPrint)
			// {
			//    print(Vessel.unloadDistance);
			//    oneshotPrint = false;
			//}
			// Vessel.unloadDistance = 120000;
			// Vessel.loadDistance = 120000;
			foreach (Vessel v in vesselList)
			{
				// print(v.RevealName());
				//print(v.packed);
				//print(v.distancePackThreshold);
				// print(v.distanceUnpackThreshold);

				var theirloc = v.GetWorldPos3D();
				if (Vector3.Distance(myloc, theirloc) < fieldRange)
				{
					v.distanceUnpackThreshold = fieldRange;
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
