using UnityEngine;

namespace InterstellarAccelerator
{
	//this one is to be added via the module manager to EVERYTHING so that we can scale the parts correctly.
	public class VABScaleDetector : PartModule
	{
		public override void OnStart(PartModule.StartState state)
		{
			EditorLogic logic = EditorLogic.fetch;

			if (logic != null)
			{
				Debug.Log("found editor logic!");

				Debug.Log(logic.ship[0].scaleFactor);

				foreach (Part p in logic.ship.parts)
				{
					var moduleList = p.FindModulesImplementing<StructuralIntegrityField>();
					if (moduleList.Count != 0)
					{
						StructuralIntegrityField mod = moduleList[0];
						if (mod.sizeState)
						{
							//don't rescale a strut.
							if (this.part.FindModuleImplementing<CompoundParts.CModuleStrut>() == null)
								this.part.rescaleFactor = .02f;
							//this.part.partTransform.localScale = new Vector3(.02f, .02f, .02f);
							this.part.srfAttachNode.position *= .02f;
							foreach (AttachNode node in part.attachNodes)
							{
								// node.size = 1;
								node.originalPosition /= 50.0f;
								//node.position /= 50.0f;
							}


							var lightsOnModel = part.FindModelComponents<Light>();
							if (lightsOnModel != null)
								foreach (Light l in lightsOnModel)
									l.enabled = false;
						}

					}

				}



				//logic.ship;

			}
			base.OnStart(state);
		}

	}
}
