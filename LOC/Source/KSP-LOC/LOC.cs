using UnityEngine;
using System.IO;
using System;
using KSP_World_Nav;

namespace KSP_LOC
{
    public class Localizer : PartModule
    {
        private LOC cLOC = null;
        private NAVmaster cNav_Master = new NAVmaster();

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Broadcast"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
        public bool bBroadcast;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Broadcast Power", guiFormat = "F0"), UI_FloatRange(maxValue=25.0f,minValue =10.0f, stepIncrement = 5.0f)]
        double dBroadcast_Power = 25.0;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Frequency", guiFormat = "F2")]
        public double dFrequency = 108.10;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Beam Width", guiFormat = "F1"), UI_FloatRange(maxValue = 8.0f, minValue = 3.0f, stepIncrement = 0.5f)]
        public float dBeam_Width = 5.0f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Beam Bearing", guiFormat = "F1"), UI_FloatRange(maxValue = 359.0f, minValue = 0.0f, stepIncrement = 1.0f)]
        public float dBeam_Bearing = 0.0f;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up")]
        public void nextFrequency()
        {
            if (cLOC != null)
            {
                cLOC.incrementChannel();
                dFrequency = cLOC.getFrequency();
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down")]
        public void prevFrequency()
        {
            if (cLOC != null)
            {
                cLOC.decrementChannel();
                dFrequency = cLOC.getFrequency();
            }
        }
    
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Station ID")]
        public string sStation_ID = "IKS";

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Up")]
        public void nextID1()
        {
            if (cLOC != null)
            {
                cLOC.incrementStationID(1);
                sStation_ID = cLOC.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Down")]
        public void prevID1()
        {
            if (cLOC != null)
            {
                cLOC.decrementStationID(1);
                sStation_ID = cLOC.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Up")]
        public void nextID2()
        {
            if (cLOC != null)
            {
                cLOC.incrementStationID(2);
                sStation_ID = cLOC.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Down")]
        public void prevID2()
        {
            if (cLOC != null)
            {
                cLOC.decrementStationID(2);
                sStation_ID = cLOC.sStation_ID;
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
            if (cLOC == null)
            {
                cLOC = new LOC();
                cLOC.iStation_ID_Characters = new int[3];
                cLOC.iStation_ID_Characters[0] = 10; // K
                cLOC.iStation_ID_Characters[1] = 18; // S
                cLOC.iStation_ID_Characters[2] = 15; // P
                cLOC.buildStationID();
                sStation_ID = cLOC.sStation_ID;
            }
            if (cNav_Master == null)
                cNav_Master = new NAVmaster();
        }
        private void register()
        {
            initialize();
            if (iStation_ID == -1)
            {
                //print("LOC station register");
                iStation_ID = cNav_Master.registerStation(cLOC);
            }
        }
        public override void OnInitialize()
        {
            base.OnInitialize();
            //print("LOC initialize");
            initialize();
        }
        public override void OnAwake()
        {
            base.OnAwake();
            //print("LOC awake");
            initialize();
        }
        public override void OnInactive()
        {
            base.OnInactive();
            //print("LOC inactive");
        }
        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            //print("LOC copy");
        }

        public override void OnStart(StartState state)
        {
            register();
            cLOC.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude);
            cNav_Master.updateStation(iStation_ID, cLOC);
            base.OnStart(state);
            //print("LOC start");
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            cLOC.OnSave(node);
        }
        public override void OnLoad(ConfigNode node)
        {
            initialize();
            //print("LOC load");
            base.OnLoad(node);
            cLOC.OnLoad(node);
            if (iStation_ID != -1)
                cNav_Master.updateStation(iStation_ID, cLOC);
            sStation_ID = cLOC.sStation_ID;
            dFrequency = cLOC.getFrequency();
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
            if (dBroadcast_Local != cLOC.dTransmit_Power)
            {
                cLOC.dTransmit_Power = dBroadcast_Local;
                bUpdate_Flag = true;
            }

            if ((dBeam_Width * 0.5) != cLOC.dBeam_Half_Width)
            {
                cLOC.dBeam_Half_Width = dBeam_Width * 0.5;
                bUpdate_Flag = true;
            }

            if (dBeam_Bearing != cLOC.dBeam_Bearing)
            {
                cLOC.dBeam_Bearing = dBeam_Bearing;
                bUpdate_Flag = true;
            }

            //string sStr2 = "Update required pre position " + bUpdate_Flag;
            //print(sStr2);

            if (cLOC.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude))
                bUpdate_Flag = true;

            //string sStr3 = "Update required post position " + bUpdate_Flag;
            //print(sStr3);

            sLatitude = cLOC.vPosition.sLatitude_Human;
            sLongitude = cLOC.vPosition.sLongitude_Human;
            dElevation = vessel.altitude;

            if (bUpdate_Flag)
                cNav_Master.updateStation(iStation_ID, cLOC);

            //string sStr4 = "Update complete ";
            //print(sStr4);
        }
    }
}
