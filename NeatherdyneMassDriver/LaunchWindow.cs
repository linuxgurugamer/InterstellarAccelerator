using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ClickThroughFix;
using SpaceTuxUtility;
using LibNoise.Modifiers;

namespace NeatherdyneMassDriver
{
    class LaunchWindow : MonoBehaviour
    {
        const int WIDTH = 350;
        const int HEIGHT = 300;

        public ModuleMassAccelerator_v2 mma;

        Rect position = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);

        float countdownLength;
        internal float powerSetting = 100;
        bool savePermanent = false;
        int winId;
        internal void SetXY(float x, float y)
        {
            position.x = x;
            position.y = y;
        }

        void Start()
        {
            countdownLength = HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength;
            powerSetting =  100f;
            winId = WindowHelper.NextWindowId("Neatherdyne");
        }
        void OnGUI()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IA>().useAltSkin)
                GUI.skin = HighLogic.Skin;
            position = ClickThruBlocker.GUILayoutWindow(winId, this.position, Display, "Neatherdyne Mass Driver Launcher", GUILayout.MinHeight(20));
        }

        void Display(int id)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fire"))
            {
                mma.rootMA.Fire(powerSetting);
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Start Countdown: " + countdownLength.ToString("F1") + " seconds"))
            {
                if (savePermanent)
                {
                    HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength = countdownLength;
                }
                mma.rootMA.StartCountdown(countdownLength, powerSetting );
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Disarm"))
            {
                mma.rootMA.DisarmAccelerator();
                Destroy(this);
            }
            GUILayout.Label("_____________________________________________");
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Settings");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Countdown (" + countdownLength.ToString("F1") + "): ");
            GUILayout.FlexibleSpace();
            countdownLength = GUILayout.HorizontalSlider(countdownLength, 1f, 20f, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power (" + powerSetting.ToString("F1") + ": ");
            GUILayout.FlexibleSpace();
            powerSetting = GUILayout.HorizontalSlider(powerSetting, 0, 10, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            savePermanent = GUILayout.Toggle(savePermanent, "Save the countdown time permanently");
            GUI.DragWindow();
        }
    }
}
