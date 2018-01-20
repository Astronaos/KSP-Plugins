using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using KSP_World_Nav;

namespace KSP_COMM_Receiver
{
    public class VHF_COMM_Receiver : PartModule
    {
        private COMMreceiver cRecv = new COMMreceiver();
        private NAVmaster cNav_Master = new NAVmaster();

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Power"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool bOn = false;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Radio Is Powered")]
        public bool bPowered = false;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Freq.", guiFormat = "F3")]
        public double dFrequency;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Standby Freq.", guiFormat = "F3")]
        public double dStandby_Frequency;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Tuned Station ID")]
        public string sTuned_Station = "---";

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up")]
        public void nextFrequency()
        {
            cRecv.incrementStandbyChannel();
            cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down")]
        public void prevFrequency()
        {
            cRecv.decrementStandbyChannel();
            cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up (1 MHz)")]
        public void nextFrequencyLarge()
        {
            cRecv.incrementStandbyChannelLarge();
            dFrequency = cRecv.getFrequency();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down (1 MHz)")]
        public void prevFrequencyLarge()
        {
            cRecv.decrementStandbyChannelLarge();
            dFrequency = cRecv.getFrequency();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "<-->")]
        public void swapFrequency()
        {
            cRecv.swapChannels();
            cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dFrequency = cRecv.getFrequency();
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }

        NAVbase cTuned_Station = null;

        // Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
        // the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double dW_to_EC = 1.0 / 45000.0;
        private int iReceiver_ID = -1;

        private void initialize()
        {
            if (cRecv == null)
                cRecv = new COMMreceiver();
            if (cNav_Master == null)
                cNav_Master = new NAVmaster();
        }
        private void register()
        {
            initialize();
            if (iReceiver_ID == -1)
                iReceiver_ID = cNav_Master.registerReceiver(cRecv);
        }
        public override void OnActive()
        {
        }
        public override void OnAwake()
        {
            initialize();
        }
        public override void OnInactive()
        {
        }

        public override void OnStart(StartState state)
        {
            register();
            cRecv.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude);
            if (iReceiver_ID != -1)
                cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dFrequency = cRecv.getFrequency();
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            cRecv.OnSave(node);
        }
        public override void OnLoad(ConfigNode node)
        {
            initialize();

            base.OnLoad(node);
            cRecv.OnLoad(node);
            if (iReceiver_ID != -1)
                cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dFrequency = cRecv.getFrequency();
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }
        private double dReceiver_Timer = 0.0;
        public override void OnUpdate()
        {
            initialize();

            string ResName = "ElectricCharge";

            double dPowerRequired = 10.0 * dW_to_EC; // 10 W is typical receive power for NAV radio

            double dElectric_Draw = dPowerRequired * TimeWarp.deltaTime;
            double elecAvail = 0.0;
            if (bOn)
                elecAvail = part.RequestResource(ResName, dElectric_Draw) / dElectric_Draw;
            bPowered = (elecAvail > 0.90);

            if (cRecv.vPosition.updatePositionData(vessel.mainBody,vessel.latitude,vessel.longitude,vessel.altitude))
                cNav_Master.updateReceiver(iReceiver_ID, cRecv);

            //string sMsg = "Recv freq " + cRecv.getFrequency().ToString("000.00");
            //print(sMsg);
            cNav_Master.onReceiverUpdate();
            if (bPowered)
            {
                cTuned_Station = cNav_Master.getStation(iReceiver_ID);
            }
            else
            {
                cTuned_Station = null;
            }
            if (cTuned_Station != null)
            {
                if (cTuned_Station.getFlux(cRecv.vPosition) > 2.0e-9)
                {
                    sTuned_Station = cTuned_Station.sStation_ID;
                    dReceiver_Timer += Time.deltaTime;
                    if (dReceiver_Timer > 15.0)
                    {
                        COMM cComm = cTuned_Station as COMM;
                        if (cComm != null)
                        {
                            ScreenMessages.PostScreenMessage(cComm.sMessage, 10.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        dReceiver_Timer = 0.0;
                    }
                }
                else
                {
                    dReceiver_Timer = 0.0;
                    sTuned_Station = "---";
                }
            }
            else
            {
                dReceiver_Timer = 0.0;
                sTuned_Station = "---";
            }
        }
    }
}
