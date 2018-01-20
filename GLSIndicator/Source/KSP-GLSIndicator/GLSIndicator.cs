using UnityEngine;
using System.IO;
using System;
using KSP_World_Nav;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using KSP_NAV_Receiver;

namespace KSP_GLSIndicator
{
    public class GLSIndicator : PartModule
    {

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool showGui = true;
        public Rect guiMainWindowPos = new Rect(0,100,150,150);
        Rect guiDefaultPosition = new Rect(Screen.width * 0.5f - 350.0f, Screen.height * 0.5f - 50.0f, 88, 100);
//        Rect guiDebugWindowPos = new Rect(0, 50, 500, 300);
//        Rect defaultEditorPos = new Rect(Screen.width / 3, 40, 10, 10);
//        Rect defaultFlightPos = new Rect(0, 50, 10, 10);
// Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
// the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double dW_to_EC = 1.0 / 45000.0;

        VHF_NAV_Receiver cNav_Select = null;
        private double dIndicated_Deviation = 0.0;
        private string sStation_ID = "---";

        #region PartModule overrides.
        public override void OnActive()
        {
            base.OnActive();
            showGui = true;
        }
        public override void OnInitialize()
        {
            base.OnInitialize();
        }
        public override void OnAwake()
        {
            base.OnAwake();
            //print("GLS awake");
        }
        public override void OnInactive()
        {
            base.OnInactive();
            showGui = false;
            //print("GLS inactive");
        }
        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            //print("GLS copy");
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            //print("GLS start");
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            double dElectric_Draw = 10.0 * dW_to_EC;
            double elecAvail = part.RequestResource("ElectricCharge", dElectric_Draw) / dElectric_Draw;
            bool bPowered = (elecAvail > 0.90);
            dIndicated_Deviation = -10.0;
            sStation_ID = "---";

            if (bPowered)
            {
                cNav_Select = null;
                foreach (Part p in vessel.Parts)
                {
                    //string sMsg1 = "VI: " + p.name;
                    //print(sMsg1);
                    foreach (PartModule m in p.Modules)
                    {
                        //string sMsg2 = "VIm: " + m.name;
                        //print(sMsg2);
                        VHF_NAV_Receiver cNav = m as VHF_NAV_Receiver;
                        if (cNav != null)
                        {
                            cNav_Select = cNav;
                        }
                    }
                }
                if (cNav_Select != null)
                {
                    //string sMsg = "GLS indicator found receiver " + cNav_Select.moduleName;
                    //print(sMsg);
                    GLS cGLS = cNav_Select.getTunedGLS();
                    if (cGLS != null)
                    {
                        dIndicated_Deviation = cGLS.getOffset(cNav_Select.getPosition());
                        sStation_ID = cGLS.sStation_ID;
                    }
                    else
                        sStation_ID = "---";

                }
                else
                    sStation_ID = "---";
            }
        }
        #endregion


        #region GUI controls
        GUIStyle locTextStyle;
        GUIStyle stationIDStyle;
        void GUIStyles()
        {
            GUI.skin = HighLogic.Skin;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            locTextStyle = new GUIStyle(GUI.skin.label);
            locTextStyle.alignment = TextAnchor.MiddleCenter;
            locTextStyle.fontSize = 20;
            locTextStyle.padding = new RectOffset(4, 4, 4, 4);
            locTextStyle.normal.textColor = Color.magenta;

            stationIDStyle = new GUIStyle(GUI.skin.label);
            stationIDStyle.alignment = TextAnchor.MiddleCenter;
            stationIDStyle.fontSize = 16;
            stationIDStyle.padding = new RectOffset(4, 4, 4, 4);
            stationIDStyle.normal.textColor = Color.white;

        }
        private bool clickThroughLocked = false;
        void OnGUI()
        {
            //print("GLS Indicator on gui");
            if (!showGui || !vessel.isActiveVessel || HighLogic.LoadedSceneIsEditor)
            {
                return;
            }

            GUIStyles();

            // Set title
            string title = part.partInfo.title;

            guiMainWindowPos = GUILayout.Window(GetInstanceID(), guiDefaultPosition, GuiMain, title);

            // Disable Click through
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (guiMainWindowPos.Contains(Event.current.mousePosition) && !clickThroughLocked)
                {
                    InputLockManager.SetControlLock(
                        ControlTypes.EDITOR_PAD_PICK_PLACE, "GLSIndicatorLock");
                    clickThroughLocked = true;
                }
                if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked)
                {
                    InputLockManager.RemoveControlLock("GLSIndicatorLock");
                    clickThroughLocked = false;
                }
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                if (guiMainWindowPos.Contains(Event.current.mousePosition) && !clickThroughLocked)
                {
                    InputLockManager.SetControlLock(
                        ControlTypes.CAMERACONTROLS | ControlTypes.MAP, "GLSIndicatorLock");
                    clickThroughLocked = true;
                }
                if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked)
                {
                    InputLockManager.RemoveControlLock("GLSIndicatorLock");
                    clickThroughLocked = false;
                }
            }
        }

        void GuiMain(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            string sInd;
            if (sStation_ID == "---")
                sInd = "------";
            else if (dIndicated_Deviation < 0)
                sInd = "U " + dIndicated_Deviation.ToString("0.00");
            else
                sInd = "D " + dIndicated_Deviation.ToString("0.00");

            GUILayout.Label(sInd, locTextStyle,GUILayout.Width(80), GUILayout.Height(30));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(sStation_ID, stationIDStyle,GUILayout.Width(80), GUILayout.Height(30));
            GUILayout.EndHorizontal();



            GUILayout.EndVertical();
        }
        #endregion
    }
}

