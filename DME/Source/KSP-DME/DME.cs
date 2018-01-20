using UnityEngine;
using System.IO;
using System;
using KSP_World_Nav;

namespace KSP_DME
{
    public class DistanceMeasuringEquipment : PartModule
    {
        private DME cDME = null;
        private NAVmaster cNav_Master = new NAVmaster();

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Broadcast"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
        public bool bBroadcast;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Broadcast Power", guiFormat = "F0"), UI_FloatRange(maxValue = 1000.0f, minValue = 100.0f, stepIncrement = 50.0f)]
        public float dBroadcast_Power = 100.0f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Frequency", guiFormat = "F2")]
        public double dFrequency = 108.00;


        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up")]
        public void nextFrequency()
        {
            if (cDME != null)
            {
                cDME.incrementChannel();
                dFrequency = cDME.getFrequency();
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down")]
        public void prevFrequency()
        {
            if (cDME != null)
            {
                cDME.decrementChannel();
                dFrequency = cDME.getFrequency();
            }
        }
    
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Station ID")]
        public string sStation_ID = "KSP";

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Up")]
        public void nextID1()
        {
            if (cDME != null)
            {
                cDME.incrementStationID(0);
                sStation_ID = cDME.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Down")]
        public void prevID1()
        {
            if (cDME != null)
            {
                cDME.decrementStationID(0);
                sStation_ID = cDME.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Up")]
        public void nextID2()
        {
            if (cDME != null)
            {
                cDME.incrementStationID(1);
                sStation_ID = cDME.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Down")]
        public void prevID2()
        {
            if (cDME != null)
            {
                cDME.decrementStationID(1);
                sStation_ID = cDME.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(3) Select Up")]
        public void nextID3()
        {
            if (cDME != null)
            {
                cDME.incrementStationID(2);
                sStation_ID = cDME.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(3) Select Down")]
        public void prevID3()
        {
            if (cDME != null)
            {
                cDME.decrementStationID(2);
                sStation_ID = cDME.sStation_ID;
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
            if (cDME == null)
            {
                cDME = new DME();
                cDME.iStation_ID_Characters = new int[3];
                cDME.iStation_ID_Characters[0] = 10; // K
                cDME.iStation_ID_Characters[1] = 18; // S
                cDME.iStation_ID_Characters[2] = 15; // P
                cDME.buildStationID();
                sStation_ID = cDME.sStation_ID;
            }
            if (cNav_Master == null)
                cNav_Master = new NAVmaster();
        }
        private void register()
        {
            initialize();
            if (iStation_ID == -1)
            {
                //print("DME station register");
                iStation_ID = cNav_Master.registerStation(cDME);
            }
        }
        public override void OnInitialize()
        {
            base.OnInitialize();
            //print("DME initialize");
            initialize();
        }
        public override void OnAwake()
        {
            base.OnAwake();
            //print("DME awake");
            initialize();
        }
        public override void OnInactive()
        {
            base.OnInactive();
            //print("DME inactive");
        }
        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            //print("DME copy");
        }

        public override void OnStart(StartState state)
        {
            register();
            cDME.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude);
            cNav_Master.updateStation(iStation_ID, cDME);
            base.OnStart(state);
            //print("DME start");
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            cDME.OnSave(node);
        }
        public override void OnLoad(ConfigNode node)
        {
            initialize();
            //print("DME load");
            base.OnLoad(node);
            cDME.OnLoad(node);
            if (iStation_ID != -1)
                cNav_Master.updateStation(iStation_ID, cDME);
            sStation_ID = cDME.sStation_ID;
            dFrequency = cDME.getFrequency();
            dBroadcast_Power = (float)cDME.dTransmit_Power;
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
            if (dBroadcast_Local != cDME.dTransmit_Power)
            {
                cDME.dTransmit_Power = dBroadcast_Local;
                bUpdate_Flag = true;
            }

            //string sStr2 = "Update required pre position " + bUpdate_Flag;
            //print(sStr2);

            if (cDME.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude))
                bUpdate_Flag = true;

            //string sStr3 = "Update required post position " + bUpdate_Flag;
            //print(sStr3);

            sLatitude = cDME.vPosition.sLatitude_Human;
            sLongitude = cDME.vPosition.sLongitude_Human;
            dElevation = vessel.altitude;

            if (bUpdate_Flag)
                cNav_Master.updateStation(iStation_ID, cDME);

            //string sStr4 = "Update complete ";
            //print(sStr4);
        }
    }
}
