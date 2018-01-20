using UnityEngine;
using System.IO;
using System;
using KSP_World_Nav;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using KSP_NAV_Receiver;

namespace KSP_DMEIndicator
{
    public class DMEIndicator : PartModule
    {

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool showGui = true;
//        Rect guiDebugWindowPos = new Rect(0, 50, 500, 300);
//        Rect defaultEditorPos = new Rect(Screen.width / 3, 40, 10, 10);
//        Rect defaultFlightPos = new Rect(0, 50, 10, 10);
// Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
// the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double dW_to_EC = 1.0 / 45000.0;

        VHF_NAV_Receiver cNav_Select = null;

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
            //print("DME awake");
        }
        public override void OnInactive()
        {
            base.OnInactive();
            showGui = false;
            //print("DME inactive");
        }
        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            //print("DME copy");
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            //print("DME start");
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }
        private string sStation_ID  = "---";
        private int iMode = 0; // 0 = dist, 1 = GS, 2 = TTS
        private bool bUnits_Mi = true; // display distance in nm and groundspeed in knots
        private double dIndicated_Distance;
        private double dIndicated_Groundspeed;
        private double dIndicated_Time_To_Station;
        private double dLast_GS_Query_Time = -1;
        private double dLast_Query_Distance = 0.0;
        private bool bPowered = false;
        public override void OnUpdate()
        {
            base.OnUpdate();
            double dElectric_Draw = 5.0 * dW_to_EC;
            double elecAvail = part.RequestResource("ElectricCharge", dElectric_Draw) / dElectric_Draw;
            bPowered = (elecAvail > 0.90);
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
                    //string sMsg = "DME indicator found receiver " + cNav_Select.moduleName;
                    //print(sMsg);
                    DME cDME = cNav_Select.getTunedDME();
                    if (cDME != null)
                    {
                        sStation_ID = cDME.sStation_ID;
                        if ((dLast_GS_Query_Time + 1.0) < Time.time)
                        {
                            dIndicated_Distance = cDME.getDistance(cNav_Select.getPosition());
                            if (bUnits_Mi)
                                dIndicated_Distance /= 1852.0; // convert to nm
                            else
                                dIndicated_Distance /= 1000.0; // convert to km
                            dIndicated_Groundspeed = (dIndicated_Distance - dLast_Query_Distance) / (Time.time - dLast_GS_Query_Time) * 3600.0;
                            dIndicated_Time_To_Station = dIndicated_Distance / dIndicated_Groundspeed / 60.0;
                            dLast_Query_Distance = dIndicated_Distance;
                            dLast_GS_Query_Time = Time.time;
                        }
                    }
                    else
                    {
                        sStation_ID = "---";
                        dIndicated_Distance = -1;
                        dIndicated_Groundspeed = 0;
                        dLast_Query_Distance = 0;
                        dLast_GS_Query_Time = -1;
                    }
                }
                else
                {
                    sStation_ID = "---";
                    dIndicated_Distance = -1;
                    dIndicated_Groundspeed = 0;
                    dLast_Query_Distance = 0;
                    dLast_GS_Query_Time = -1;
                }
            }
        }
        #endregion


        #region GUI controls
        GUIStyle styleSelect;
        GUIStyle styleData;
        GUIStyle styleDataOff;
        GUIStyle buttonStyle;
        public Rect guiMainWindowPos = new Rect(0, 100, 150, 150);
        Rect guiDefaultPosition = new Rect(Screen.width - 190, 300, 200, 120);
        void GUIStyles()
        {
            GUI.skin = HighLogic.Skin;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            styleSelect = new GUIStyle(GUI.skin.label);
            styleSelect.alignment = TextAnchor.MiddleCenter;
            styleSelect.fontSize = 14;
            styleSelect.padding = new RectOffset(4, 4, 4, 4);
            styleSelect.normal.textColor = Color.grey;

            styleData = new GUIStyle(GUI.skin.label);
            styleData.alignment = TextAnchor.MiddleCenter;
            styleData.fontSize = 20;
            styleData.padding = new RectOffset(4, 4, 4, 4);
            styleData.normal.textColor = Color.white;

            styleDataOff = new GUIStyle(GUI.skin.label);
            styleDataOff.alignment = TextAnchor.MiddleCenter;
            styleDataOff.fontSize = 20;
            styleDataOff.padding = new RectOffset(4, 4, 4, 4);
            styleDataOff.normal.textColor = Color.grey;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(4, 4, 4, 4);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
        }
        private bool clickThroughLocked = false;
        void OnGUI()
        {
            //print("DME Indicator on gui");
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
                        ControlTypes.EDITOR_PAD_PICK_PLACE, "DMEIndicatorLock");
                    clickThroughLocked = true;
                }
                if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked)
                {
                    InputLockManager.RemoveControlLock("DMEIndicatorLock");
                    clickThroughLocked = false;
                }
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                if (guiMainWindowPos.Contains(Event.current.mousePosition) && !clickThroughLocked)
                {
                    InputLockManager.SetControlLock(
                        ControlTypes.CAMERACONTROLS | ControlTypes.MAP, "DMEIndicatorLock");
                    clickThroughLocked = true;
                }
                if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked)
                {
                    InputLockManager.RemoveControlLock("DMEIndicatorLock");
                    clickThroughLocked = false;
                }
            }
        }

        void changeMode()
        {
            iMode++;
            iMode %= 3;
        }
        void changeUnits()
        {
            bUnits_Mi = !bUnits_Mi;
        }
        void GuiMain(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            //GUILayout.Box("", GUILayout.Width(5), GUILayout.Height(5));
            //GUILayout.Box("", GUILayout.Width(15), GUILayout.Height(5));
            //GUILayout.Box("", GUILayout.Width(5), GUILayout.Height(5));
            //GUILayout.Box("", GUILayout.Width(15), GUILayout.Height(5));
            //GUILayout.Box("", GUILayout.Width(2), GUILayout.Height(5));
            //GUILayout.Box("", GUILayout.Width(15), GUILayout.Height(5));
            //GUILayout.Box("", GUILayout.Width(5), GUILayout.Height(5));
            //GUILayout.Box("", GUILayout.Width(5), GUILayout.Height(5));
            string sMode;
            string sReadout;
            if (bPowered)
            {
                switch (iMode)
                {
                    default:
                    case 0: //Dist
                        sReadout = dIndicated_Distance.ToString("##0.0");
                        sMode = "DIST";
                        break;
                    case 1: //Dist
                        sReadout = dIndicated_Groundspeed.ToString("##0");
                        sMode = "GS  ";
                        break;
                    case 2: //TTS
                        if (dIndicated_Groundspeed > 1.0)
                            sReadout = dIndicated_Time_To_Station.ToString("##0");
                        else
                            sReadout = "---";
                        sMode = "TTS ";
                        break;
                }
            }
            else
            {
                sMode = "----";
                sReadout = "---";
            }

            if (bPowered)
                GUILayout.Label(sStation_ID, styleData, GUILayout.Width(80), GUILayout.Height(30));
            else
                GUILayout.Label("---", styleDataOff, GUILayout.Width(80), GUILayout.Height(30));
            GUILayout.Label(sReadout, styleData, GUILayout.Width(80), GUILayout.Height(30));

            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if (bPowered)
                GUILayout.Label(sMode, styleSelect,GUILayout.Width(50), GUILayout.Height(30));
            else
                GUILayout.Label("---", styleSelect,GUILayout.Width(50), GUILayout.Height(30));
            if (GUILayout.Button("Mode", buttonStyle, GUILayout.Width(50), GUILayout.Height(30)))
            {
                changeMode();
            }


            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("nm/km", buttonStyle, GUILayout.Width(50), GUILayout.Height(30)))
            {
                changeUnits();
            }
            if (bPowered)
            {
                string sUnits;
                if (bUnits_Mi)
                    sUnits = "nm";
                else
                    sUnits = "km";
                GUILayout.Label(sUnits, styleSelect, GUILayout.Width(40), GUILayout.Height(30));
            }
            else
                GUILayout.Label("--", styleSelect, GUILayout.Width(40), GUILayout.Height(30));
            GUILayout.EndVertical();
        }
        #endregion
    }
}

