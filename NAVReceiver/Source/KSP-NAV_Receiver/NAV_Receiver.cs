using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using KSP_World_Nav;

namespace KSP_NAV_Receiver
{
    public class VHF_NAV_Receiver : PartModule
    {
        private VHFreceiver cRecv = new VHFreceiver();
        private NAVmaster cNav_Master = new NAVmaster();

        private int iNav_ID = -1;
        public int getNavID() { return iNav_ID; }

        public VectorBody getPosition() { return cRecv.vPosition; }

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Power"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool bOn = false;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Radio Is Powered")]
        public bool bPowered = false;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Freq.", guiFormat = "F2")]
        public double dFrequency;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Standby Freq.", guiFormat = "F2")]
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
            cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down (1 MHz)")]
        public void prevFrequencyLarge()
        {
            cRecv.decrementStandbyChannelLarge();
            cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "<-->")]
        public void swapFrequency()
        {
            cRecv.swapChannels();
            cNav_Master.updateReceiver(iReceiver_ID, cRecv);
            dFrequency = cRecv.getFrequency();
            dStandby_Frequency = cRecv.getStandbyFrequency();
        }

        VOR cTuned_VOR = null;
        LOC cTuned_LOC = null;
        GLS cTuned_GLS = null;
        DME cTuned_DME = null;
        public VOR getTunedVOR() { return cTuned_VOR; }
        public DME getTunedDME() { return cTuned_DME; }
        public LOC getTunedLOC() { return cTuned_LOC; }
        public GLS getTunedGLS() { return cTuned_GLS;  }

        // Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
        // the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double dW_to_EC = 1.0 / 45000.0;
        private int iReceiver_ID = -1;

        private void initialize()
        {
            if (cRecv == null)
                cRecv = new VHFreceiver();
            if (cNav_Master == null)
                cNav_Master = new NAVmaster();
            if (iNav_ID == -1)
            {
                int iOtherCount = -1;
                foreach (Part p in vessel.Parts)
                {
                    foreach (PartModule m in p.Modules)
                    {
                        VHF_NAV_Receiver cOtherRcvr = m as VHF_NAV_Receiver;
                        if (cOtherRcvr != null)
                        {
                            if (cOtherRcvr.iNav_ID > iOtherCount)
                                iOtherCount = cOtherRcvr.iNav_ID;
                        }
                    }
                }
                if (iOtherCount == -1)
                    iNav_ID = 1;
                else
                    iNav_ID = iOtherCount + 1;
            }
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
                if (cRecv.isActiveILS())
                {
                    cTuned_GLS = cNav_Master.getStationGLS(iReceiver_ID) as GLS;
                    cTuned_LOC = cNav_Master.getStationLOC(iReceiver_ID) as LOC;
                    cTuned_VOR = null;
                }
                else
                { 
                    cTuned_VOR = cNav_Master.getStationVOR(iReceiver_ID) as VOR;
                    cTuned_GLS = null;
                    cTuned_LOC = null;
                }
                cTuned_DME = cNav_Master.getStationDME(iReceiver_ID) as DME;
            }
            else
            {
                cTuned_VOR = null;
                cTuned_LOC = null;
                cTuned_DME = null;
                cTuned_GLS = null;
            }
            if (cTuned_VOR != null)
            {
                if (cTuned_VOR.getFlux(cRecv.vPosition) < 2.0e-9 || !cTuned_VOR.inLineOfSight(cRecv.vPosition))
                    cTuned_VOR = null;
            }
            if (cTuned_LOC != null)
            {
                if (cTuned_LOC.getFlux(cRecv.vPosition) < 2.0e-9 || !cTuned_LOC.inLineOfSight(cRecv.vPosition))
                    cTuned_LOC = null;
            }
            if (cTuned_DME != null)
            {
                if (cTuned_DME.getFlux(cRecv.vPosition) < 2.0e-9 || !cTuned_DME.inLineOfSight(cRecv.vPosition))
                    cTuned_DME = null;
            }
            if (cTuned_GLS != null)
            {
                if (cTuned_GLS.getFlux(cRecv.vPosition) > 2.0e-9 || !cTuned_GLS.inLineOfSight(cRecv.vPosition))
                    cTuned_GLS = null;
            }

            if (cTuned_VOR != null)
            {
                sTuned_Station = cTuned_VOR.sStation_ID;
            }
            else if (cTuned_LOC != null)
            {
                sTuned_Station = cTuned_LOC.sStation_ID;
            }
            else if (cTuned_DME != null)
            {
                sTuned_Station = cTuned_LOC.sStation_ID;
            }
            else if (cTuned_GLS != null)
            {
                sTuned_Station = cTuned_GLS.sStation_ID;
            }
            else
                sTuned_Station = "---";

        }
    }
}
