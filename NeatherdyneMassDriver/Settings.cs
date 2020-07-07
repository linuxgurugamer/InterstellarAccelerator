
using System.Collections;
using System.Reflection;

#if true

namespace NeatherdyneMassDriver
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings
    //
    // HighLogic.CurrentGame.Parameters.CustomParams<IA>()

    public class IA : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Neatherdyne Mass Driver"; } }
        public override string DisplaySection { get { return "Neatherdyne Mass Driver"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    powerLevel = 2f;
                    break;

                case GameParameters.Preset.Normal:
                    powerLevel = 1f;
                    break;

                case GameParameters.Preset.Moderate:
                    powerLevel = 0.5f;
                    break;

                case GameParameters.Preset.Hard:
                    powerLevel = 0.2f;
                    break;
            }

        }

        [GameParameters.CustomFloatParameterUI("Power Level ", displayFormat = "N0", minValue = 0.1f, maxValue = 10f, stepCount = 100, asPercentage = false)]
        public float powerLevel = 1.0f;

        [GameParameters.CustomIntParameterUI("Countdown length", displayFormat = "N0", minValue = 1, maxValue = 30)]
        public int countdownLength = 10;

        [GameParameters.CustomParameterUI("Countdown on screen")]
        public bool countdownOnScreen = true;



        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}

#endif