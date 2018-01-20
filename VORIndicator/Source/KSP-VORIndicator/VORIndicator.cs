using UnityEngine;
using System.IO;
using System;
using KSP_World_Nav;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using KSP_NAV_Receiver;

namespace KSP_VORIndicator
{
    public class VORIndicator : PartModule
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

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        private int iOBS = 0;
        VHF_NAV_Receiver cNav_Select = null;
        private double dIndicated_Deviation = 0.0;

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
            //print("VOR awake");
        }
        public override void OnInactive()
        {
            base.OnInactive();
            showGui = false;
            //print("VOR inactive");
        }
        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            //print("VOR copy");
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            //print("VOR start");
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
        private string sTo_From = "####";

        public override void OnUpdate()
        {
            base.OnUpdate();
            double dElectric_Draw = 10.0 * dW_to_EC;
            double elecAvail = part.RequestResource("ElectricCharge", dElectric_Draw) / dElectric_Draw;
            bool bPowered = (elecAvail > 0.90);
            dIndicated_Deviation = -10.0;
            sStation_ID = "---";
            sTo_From = "####";

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
                    //string sMsg = "VOR indicator found receiver " + cNav_Select.moduleName;
                    //print(sMsg);
                    VOR cVOR = cNav_Select.getTunedVOR();
                    if (cVOR != null)
                    {
                        bool bZOC = cVOR.inZoneOfConfusion(cNav_Select.getPosition());
                        if (!bZOC)
                        {
                            double dDeviation = (iOBS - cVOR.getBearingFrom(cNav_Select.getPosition())) % 360.0;
                            if (dDeviation > 180.0)
                                dDeviation -= 360.0;
                            while (dDeviation < -180.0)
                                dDeviation += 360.0;
                            bool bTo = dDeviation < -90.0 || dDeviation > 90.0;
                            if (bTo)
                            {
                                sTo_From = " TO ";
                                if (dDeviation < -90.0)
                                    dDeviation = -180.0 - dDeviation;
                                else
                                    dDeviation = 180.0 - dDeviation;
                            }
                            else
                                sTo_From = "FROM";

                            dIndicated_Deviation = dDeviation / 2.5;
                            sStation_ID = cVOR.sStation_ID;
                        }
                    }
                }
            }
        }
        #endregion


        #region GUI controls
        GUIStyle styleOBS;
        GUIStyle styleTOFROM;
        GUIStyle styleStation;
        GUIStyle styleDeviation;
        GUIStyle buttonStyle;
        public Rect guiMainWindowPos = new Rect(0, 100, 150, 150);
        Rect guiDefaultPosition = new Rect(Screen.width - 190, 150, 130, 100);

        void GUIStyles()
        {
            GUI.skin = HighLogic.Skin;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            styleOBS = new GUIStyle(GUI.skin.label);
            styleOBS.alignment = TextAnchor.MiddleCenter;
            styleOBS.fontSize = 16;
            styleOBS.padding = new RectOffset(4, 4, 4, 4);
            styleOBS.normal.textColor = Color.grey;

            styleStation = new GUIStyle(GUI.skin.label);
            styleStation.alignment = TextAnchor.MiddleCenter;
            styleStation.fontSize = 16;
            styleStation.padding = new RectOffset(4, 4, 4, 4);
            styleStation.normal.textColor = Color.white;

            styleTOFROM = new GUIStyle(GUI.skin.label);
            styleTOFROM.alignment = TextAnchor.MiddleCenter;
            styleTOFROM.fontSize = 16;
            styleTOFROM.padding = new RectOffset(4, 4, 4, 4);
            styleTOFROM.normal.textColor = Color.white;

            styleDeviation = new GUIStyle(GUI.skin.label);
            styleDeviation.alignment = TextAnchor.MiddleCenter;
            styleDeviation.fontSize = 16;
            styleDeviation.padding = new RectOffset(4, 4, 4, 4);
            styleDeviation.normal.textColor = Color.magenta;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(4, 4, 4, 4);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
        }
        private bool clickThroughLocked = false;
        void OnGUI()
        {
            //print("VOR Indicator on gui");
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
                        ControlTypes.EDITOR_PAD_PICK_PLACE, "VORIndicatorLock");
                    clickThroughLocked = true;
                }
                if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked)
                {
                    InputLockManager.RemoveControlLock("VORIndicatorLock");
                    clickThroughLocked = false;
                }
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                if (guiMainWindowPos.Contains(Event.current.mousePosition) && !clickThroughLocked)
                {
                    InputLockManager.SetControlLock(
                        ControlTypes.CAMERACONTROLS | ControlTypes.MAP, "VORIndicatorLock");
                    clickThroughLocked = true;
                }
                if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked)
                {
                    InputLockManager.RemoveControlLock("VORIndicatorLock");
                    clickThroughLocked = false;
                }
            }
        }

        void decrementOBS()
        {
            iOBS--;
            if (iOBS < 0)
                iOBS += 360;
        }
        void incrementOBS()
        {
            iOBS++;
            if (iOBS > 359)
                iOBS -= 360;
        }
        void GuiMain(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            string sInd;
            if (dIndicated_Deviation < 0)
                sInd = "L " + dIndicated_Deviation.ToString("0.00");
            else
                sInd = "R " + dIndicated_Deviation.ToString("0.00");

            GUILayout.Label(sInd, styleDeviation, GUILayout.Width(80), GUILayout.Height(30));

            GUILayout.Label(sTo_From, styleTOFROM, GUILayout.Width(50), GUILayout.Height(30));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();

            if (GUILayout.Button("-", buttonStyle, GUILayout.Width(30), GUILayout.Height(30)))
            {
                decrementOBS();
            }

            string sOBS = iOBS.ToString("000");
            GUILayout.Label(sOBS, styleOBS, GUILayout.Width(50), GUILayout.Height(30));

            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(30), GUILayout.Height(30)))
            {
                incrementOBS();
            }

            GUILayout.Label(sStation_ID, styleStation, GUILayout.Width(50), GUILayout.Height(30));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        #endregion
    }
}

