using UnityEngine;
#if false
namespace NeatherdyneMassDriver
{
	//this one is to be added via the module manager to EVERYTHING so that we can scale the parts correctly.
	public class VABScaleDetector : PartModule
	{
		public override void OnStart(PartModule.StartState state)
		{
			EditorLogic logic = EditorLogic.fetch;

			if (logic != null)
			{
				Log.Info("found editor logic!");

				Log.Info("logic.ship[0].scaleFactor: " + logic.ship[0].scaleFactor.ToString());

				for (int i = 0; i < logic.ship.parts.Count; i++)
				{

					Part p = logic.ship.parts[i];

					var moduleList = logic.ship.parts[i].FindModulesImplementing<StructuralIntegrityField>();
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
							for (int i2 = 0; i2 < part.attachNodes.Count; i2++)
							//foreach (AttachNode node in part.attachNodes)
							{
								part.attachNodes[i2].originalPosition /= 50.0f;
								//node.originalPosition /= 50.0f;
								// Following two were commented out in the old code
								// node.size = 1;
								//node.position /= 50.0f;
							}


							var lightsOnModel = part.FindModelComponents<Light>();
							if (lightsOnModel != null)
							{
								for (int i3 = 0; i3 < lightsOnModel.Count; i3++)
									lightsOnModel[i3].enabled = false;
							}
						}

					}

				}



				//logic.ship;

			}
			base.OnStart(state);
		}

	}
}
#endif