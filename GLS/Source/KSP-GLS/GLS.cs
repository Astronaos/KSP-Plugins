﻿using UnityEngine;
using System.IO;
using System;
using KSP_World_Nav;

namespace KSP_GLS
{
    public class Localizer : PartModule
    {
        private GLS cGLS = null;
        private NAVmaster cNav_Master = new NAVmaster();

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Broadcast"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
        public bool bBroadcast;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Broadcast Power", guiFormat = "F0"), UI_FloatRange(maxValue=25.0f,minValue =10.0f, stepIncrement = 5.0f)]
        double dBroadcast_Power = 25.0;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Frequency", guiFormat = "F2")]
        public double dFrequency = 108.10;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Beam Width", guiFormat = "F1"), UI_FloatRange(maxValue = 8.0f, minValue = 3.0f, stepIncrement = 0.5f)]
        public float dBeam_Width = 5.0f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Glidepath Angle", guiFormat = "F1"), UI_FloatRange(maxValue = 8.0f, minValue = 1.0f, stepIncrement = 0.5f)]
        public float dGlidepath_Angle = 3.0f;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up")]
        public void nextFrequency()
        {
            if (cGLS != null)
            {
                cGLS.incrementChannel();
                dFrequency = cGLS.getFrequency();
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down")]
        public void prevFrequency()
        {
            if (cGLS != null)
            {
                cGLS.decrementChannel();
                dFrequency = cGLS.getFrequency();
            }
        }
    
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Station ID")]
        public string sStation_ID = "IKS";

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Up")]
        public void nextID1()
        {
            if (cGLS != null)
            {
                cGLS.incrementStationID(1);
                sStation_ID = cGLS.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Down")]
        public void prevID1()
        {
            if (cGLS != null)
            {
                cGLS.decrementStationID(1);
                sStation_ID = cGLS.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Up")]
        public void nextID2()
        {
            if (cGLS != null)
            {
                cGLS.incrementStationID(2);
                sStation_ID = cGLS.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Down")]
        public void prevID2()
        {
            if (cGLS != null)
            {
                cGLS.decrementStationID(2);
                sStation_ID = cGLS.sStation_ID;
            }
        }

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Latitude")]
        public string sLatitude;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Longitude")]
        public string sLongitude;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Elevation", guiFormat = "F1")]
        public double dElevation;

        // Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
        // the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double dW_to_EC = 1.0 / 45000.0;
        private double ElectricPowerRequired;

        public override void OnActive()
        {
        }
        private int iStation_ID = -1;
        private void initialize()
        {
            if (cGLS == null)
            {
                cGLS = new GLS();
                cGLS.iStation_ID_Characters = new int[3];
                cGLS.iStation_ID_Characters[0] = 10; // K
                cGLS.iStation_ID_Characters[1] = 18; // S
                cGLS.iStation_ID_Characters[2] = 15; // P
                cGLS.buildStationID();
                sStation_ID = cGLS.sStation_ID;
            }
            if (cNav_Master == null)
                cNav_Master = new NAVmaster();
        }
        private void register()
        {
            initialize();
            if (iStation_ID == -1)
            {
                //print("GLS station register");
                iStation_ID = cNav_Master.registerStation(cGLS);
            }
        }
        public override void OnInitialize()
        {
            base.OnInitialize();
            //print("GLS initialize");
            initialize();
        }
        public override void OnAwake()
        {
            base.OnAwake();
            //print("GLS awake");
            initialize();
        }
        public override void OnInactive()
        {
            base.OnInactive();
            //print("GLS inactive");
        }
        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            //print("GLS copy");
        }

        public override void OnStart(StartState state)
        {
            register();
            cGLS.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude);
            cNav_Master.updateStation(iStation_ID, cGLS);
            base.OnStart(state);
            //print("GLS start");
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            cGLS.OnSave(node);
        }
        public override void OnLoad(ConfigNode node)
        {
            initialize();
            //print("GLS load");
            base.OnLoad(node);
            cGLS.OnLoad(node);
            if (iStation_ID != -1)
                cNav_Master.updateStation(iStation_ID, cGLS);
            sStation_ID = cGLS.sStation_ID;
            dFrequency = cGLS.getFrequency();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            initialize(); // make sure that everything is registered
            //string ResName = "ElectricCharge";

            // radio broadcast power
            ElectricPowerRequired = dBroadcast_Power * dW_to_EC;
            ElectricPowerRequired += 100.0 * dW_to_EC; // electronics power - maybe exaggerated
            double dBroadcast_Local = dBroadcast_Power;

            double dElectric_Draw = ElectricPowerRequired * TimeWarp.deltaTime;
            double elecAvail = 0.0;
            if (bBroadcast)
                elecAvail = part.RequestResource("ElectricCharge", dElectric_Draw) / dElectric_Draw;
            if (elecAvail < 0.90)
                dBroadcast_Local = 0.0;
            //string sStr1 = "Broadcast Power " + dBroadcast_Power.ToString("0.0");
            //print(sStr1);
            bool bUpdate_Flag = false;
            if (dBroadcast_Local != cGLS.dTransmit_Power)
            {
                cGLS.dTransmit_Power = dBroadcast_Local;
                bUpdate_Flag = true;
            }

            if ((dBeam_Width * 0.5) != cGLS.dBeam_Half_Width)
            {
                cGLS.dBeam_Half_Width = dBeam_Width * 0.5;
                bUpdate_Flag = true;
            }

            if (dGlidepath_Angle != cGLS.dGlidepath_Angle)
            {
                cGLS.dGlidepath_Angle = dGlidepath_Angle;
                bUpdate_Flag = true;
            }

            //string sStr2 = "Update required pre position " + bUpdate_Flag;
            //print(sStr2);

            if (cGLS.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude))
                bUpdate_Flag = true;

            //string sStr3 = "Update required post position " + bUpdate_Flag;
            //print(sStr3);

            sLatitude = cGLS.vPosition.sLatitude_Human;
            sLongitude = cGLS.vPosition.sLongitude_Human;
            dElevation = vessel.altitude;

            if (bUpdate_Flag)
                cNav_Master.updateStation(iStation_ID, cGLS);

            //string sStr4 = "Update complete ";
            //print(sStr4);
        }
    }
}
