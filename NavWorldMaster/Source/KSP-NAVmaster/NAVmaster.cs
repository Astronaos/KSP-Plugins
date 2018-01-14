using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

namespace KSP_World_Nav
{
    public class VectorBody
    {
        public CelestialBody bBody = null;
        public Vector3d vPosition_Vector;
        public Vector3d vPosition_Vector_Common;
        public string sLatitude_Human = "N00°00'00\"";
        public string sLongitude_Human = "E000°00'00\"";

        public bool updatePositionData(CelestialBody i_bBody, double i_dLatitude, double i_dLongitude, double i_dElevation)
        {
            bool bRet = false;
            if (Math.Abs(vPosition_Vector_Common.z - i_dElevation) > 0.5 ||
                Math.Abs(vPosition_Vector_Common.x - i_dLatitude) > 2e-5 || // 0.1"
                Math.Abs(vPosition_Vector_Common.y - i_dLongitude) > 2e-5 ||
                bBody == null ||
                bBody != i_bBody)
            {
                bRet = true; // flag to indciate update was performed.
                bBody = i_bBody;
                double dLat = Math.Abs(i_dLatitude);
                double dMin = (dLat - Math.Floor(dLat)) * 60.0;
                double dSec = (dMin - Math.Floor(dMin)) * 60.0;
                if (i_dLatitude < 0)
                    sLatitude_Human = "S" + dLat.ToString("00") + "°" + dMin.ToString("00") + "'" + dSec.ToString("00.0") + "\"";
                else
                    sLatitude_Human = "N" + dLat.ToString("00") + "°" + dMin.ToString("00") + "'" + dSec.ToString("00.0") + "\"";

                double dLon = Math.Abs(i_dLongitude);
                dMin = (dLon - Math.Floor(dLon)) * 60.0;
                dSec = (dMin - Math.Floor(dMin)) * 60.0;
                if (i_dLongitude < 0)
                    sLongitude_Human = "W" + dLon.ToString("000") + "°" + dMin.ToString("00") + "'" + dSec.ToString("00.0") + "\"";
                else
                    sLongitude_Human = "E" + dLon.ToString("000") + "°" + dMin.ToString("00") + "'" + dSec.ToString("00.0") + "\"";
                vPosition_Vector = computePositionVector(i_bBody, i_dLatitude, i_dLongitude, i_dElevation);
                vPosition_Vector_Common.x = i_dLatitude;
                vPosition_Vector_Common.y = i_dLongitude;
                vPosition_Vector_Common.z = i_dElevation;
            }
            return bRet;
        }
        private Vector3d computePositionVector(CelestialBody i_bBody, double i_dLatitude, double i_dLongitude, double i_dElevation)
        {

            Vector3d vVector = new Vector3d();
            double dDeg_to_Rad = Math.PI / 180.0;
            double dLat_Rad = i_dLatitude * dDeg_to_Rad;
            double dLon_Rad = i_dLongitude * dDeg_to_Rad;
            double dCos_Lat = Math.Cos(dLat_Rad);
            double dSin_Lat = Math.Sin(dLat_Rad);
            double dCos_Lon = Math.Cos(dLon_Rad);
            double dSin_Lon = Math.Sin(dLon_Rad);
            double dRadius = i_dElevation + i_bBody.Radius;
            vVector.x = dRadius * dCos_Lat * dCos_Lon;
            vVector.y = dRadius * dCos_Lat * dSin_Lon;
            vVector.z = dRadius * dSin_Lat;
            return vVector;
        }

        public double Dot(VectorBody i_vRHO)
        {
            return (vPosition_Vector.x * i_vRHO.vPosition_Vector.x + vPosition_Vector.y * i_vRHO.vPosition_Vector.y + vPosition_Vector.z * i_vRHO.vPosition_Vector.z);
        }
        public double Dot(Vector3d i_vRHO)
        {
            return (vPosition_Vector.x * i_vRHO.x + vPosition_Vector.y * i_vRHO.y + vPosition_Vector.z * i_vRHO.z);
        }
        public Vector3d Cross(VectorBody i_vRHO)
        {
            Vector3d vRet = new Vector3d();
            vRet.x = (vPosition_Vector.y * i_vRHO.vPosition_Vector.z - vPosition_Vector.z * i_vRHO.vPosition_Vector.y);
            vRet.y = (-vPosition_Vector.x * i_vRHO.vPosition_Vector.z + vPosition_Vector.z * i_vRHO.vPosition_Vector.x);
            vRet.z = (vPosition_Vector.x * i_vRHO.vPosition_Vector.y - vPosition_Vector.y * i_vRHO.vPosition_Vector.x);
            return vRet;
        }
        public Vector3d Cross(Vector3d i_vRHO)
        {
            Vector3d vRet = new Vector3d();
            vRet.x = (vPosition_Vector.y * i_vRHO.z - vPosition_Vector.z * i_vRHO.y);
            vRet.y = (-vPosition_Vector.x * i_vRHO.z + vPosition_Vector.z * i_vRHO.x);
            vRet.z = (vPosition_Vector.x * i_vRHO.y - vPosition_Vector.y * i_vRHO.x);
            return vRet;
        }

    }
    public abstract class NAVbase
    {
        public string sStation_ID;
        public int[] iStation_ID_Characters;
        public VectorBody vPosition = new VectorBody();
        public double dTransmit_Power = 0.0;
        public int iChannel = 0;

        public abstract double getFrequency();
        public abstract void incrementChannel();
        public abstract void decrementChannel();
        public abstract void incrementChannelLarge();
        public abstract void decrementChannelLarge();
        public virtual void OnSave(ConfigNode i_Node)
        {
            i_Node.AddValue("Channel", iChannel);
            i_Node.AddValue("Transmit Power", dTransmit_Power);
            if (iStation_ID_Characters != null)
            {
                i_Node.AddValue("ID Length", iStation_ID_Characters.Length);
                for (int i = 0; i < iStation_ID_Characters.Length; i++)
                {
                    string sID = "ID" + i;

                    i_Node.AddValue(sID, iStation_ID_Characters[i]);
                }
            }
            else
                i_Node.AddValue("ID Length", 0);
        }
        public virtual void OnLoad(ConfigNode i_Node)
        {

            string sChannel = i_Node.GetValue("Channel");
            if (sChannel != null)
                iChannel = Int32.Parse(sChannel);
            string sTrx_Power = i_Node.GetValue("Transmit Power");
            if (sTrx_Power != null)
                dTransmit_Power = Double.Parse(sTrx_Power);
            string sID_Len = i_Node.GetValue("ID Length");
            int iID_Length = 0;
            if (sID_Len != null)
                iID_Length = Int32.Parse(sID_Len);
            if (iID_Length != 0)
            {
                iStation_ID_Characters = new int[iID_Length];
                for (int i = 0; i < iStation_ID_Characters.Length; i++)
                {
                    string sID = "ID" + i;
                    string sID_Data = i_Node.GetValue(sID);
                    if (sID_Data != null)
                        iStation_ID_Characters[i] = Int32.Parse(sID_Data);
                }
            }
            buildStationID();
        }
        public void buildStationID()
        {
            if (iStation_ID_Characters != null)
            {
                char[] chStr = new char[iStation_ID_Characters.Length];
                for (int i = 0; i < iStation_ID_Characters.Length; i++)
                {
                    chStr[i] = 'A';
                    chStr[i] += (char)iStation_ID_Characters[i];
                }

                sStation_ID = new string(chStr);
            }
        }
        public void incrementStationID(int i_iCharacter)
        {
            if (iStation_ID_Characters != null && i_iCharacter >= 0 && i_iCharacter < iStation_ID_Characters.Length)
            {
                iStation_ID_Characters[i_iCharacter]++;
                if (iStation_ID_Characters[i_iCharacter] > 25)
                    iStation_ID_Characters[i_iCharacter] -= 26;
                buildStationID();
            }
        }
        public void decrementStationID(int i_iCharacter)
        {
            if (iStation_ID_Characters != null && i_iCharacter >= 0 && i_iCharacter < iStation_ID_Characters.Length)
            {
                iStation_ID_Characters[i_iCharacter]--;
                if (iStation_ID_Characters[i_iCharacter] < 0)
                    iStation_ID_Characters[i_iCharacter] += 26;
                buildStationID();
            }
        }

        public double getDistance(VectorBody i_vReceiver_Position)
        {
            double dDistance = -1;
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                Vector3d vAbsolute_Offset = i_vReceiver_Position.vPosition_Vector - vPosition.vPosition_Vector;
                dDistance = vAbsolute_Offset.magnitude;
            }
            return dDistance;
        }
        public double getFlux(VectorBody i_vReceiver_Position)
        {
            double dFlux = 0.0;
            // assume the user has already verified that they are around the same celestial body
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                Vector3d vAbsolute_Offset = i_vReceiver_Position.vPosition_Vector - vPosition.vPosition_Vector;
                double dDistance_m = vAbsolute_Offset.magnitude;

                dFlux = dTransmit_Power / (4.0 * Math.PI * dDistance_m * dDistance_m);

            }
            return dFlux;
        }
        public bool inLineOfSight(VectorBody i_vReceiver_Position)
        {
            bool bLOS = false;
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                Vector3d vAbsolute_Offset = i_vReceiver_Position.vPosition_Vector - vPosition.vPosition_Vector;
                //double dInv_Mag_Recv = 1.0 / i_vReceiver_Position.vPosition_Vector.magnitude;
                //double dInv_Mag_VOR = 1.0 / vPosition.vPosition_Vector.magnitude;
                //double dInv_Mag_AO = 1.0 / vAbsolute_Offset.magnitude;

                // horizon check
                // solve the interecpt such that
                // |v + a t| <= R^2
                // where v is the location of the station, a is the absolute offset from the receiver to the VOR, and t is a parameter
                // if t is real then the line does intersect the planet at some point
                // if t is between 0 and 1, then the intersection occurrs between the receiver and the station
                double dDistance_m = vAbsolute_Offset.magnitude;
                double dA = vAbsolute_Offset.magnitude * vAbsolute_Offset.magnitude;
                double dB = vPosition.Dot(vAbsolute_Offset);
                double dC = i_vReceiver_Position.vPosition_Vector.magnitude * i_vReceiver_Position.vPosition_Vector.magnitude - vPosition.bBody.Radius * vPosition.bBody.Radius;
                double dBmAC = dB * dB - dA * dC;
                bLOS = true;
                if (dBmAC > 0.0) // make sure this is a real number
                {
                    double dS = Math.Sqrt(dBmAC / (dA * dA));
                    double dR = dB / dA;
                    double dT1 = -dR - dS;
                    double dT2 = -dR + dS;
                    bLOS &= !((dT1 >= 0.0 && dT1 <= 1.0) || (dT2 >= 0.0 && dT2 <= 1.0));
                }
            }
            return bLOS;
        }
        public double getBearingFrom(VectorBody i_vReceiver_Position)
        {
            double dBearing = -999; // invalid value
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                Vector3d vAbsolute_Offset = i_vReceiver_Position.vPosition_Vector - vPosition.vPosition_Vector;
                double dInv_Mag_Recv = 1.0 / i_vReceiver_Position.vPosition_Vector.magnitude;
                double dInv_Mag_VOR = 1.0 / vPosition.vPosition_Vector.magnitude;
                double dInv_Mag_AO = 1.0 / vAbsolute_Offset.magnitude;

                double dDistance_m = vAbsolute_Offset.magnitude;

                // determine the vector that describes the normal to the Great Circle containing the station and the north pole
                Vector3d vLocal_North = new Vector3d();
                vLocal_North.x = vPosition.vPosition_Vector.y;
                vLocal_North.y = -vPosition.vPosition_Vector.x;
                vLocal_North.z = 0.0;
                vLocal_North.Normalize();


                // determine the vector that describes the normal to the Great Circle containing the station and the receiver
                Vector3d vReceiver_Station_Bearing_Circle = new Vector3d();
                vReceiver_Station_Bearing_Circle = vPosition.Cross(i_vReceiver_Position);
                vReceiver_Station_Bearing_Circle.Normalize();

                double dDot = vReceiver_Station_Bearing_Circle.x * vLocal_North.x + vReceiver_Station_Bearing_Circle.y * vLocal_North.y;
                if (vReceiver_Station_Bearing_Circle.z >= 0.0)
                    dBearing = Math.Acos(dDot) * 180.0 / Math.PI;
                else
                    dBearing = 360.0 - Math.Acos(dDot) * 180.0 / Math.PI;
            }

            return dBearing;
        }
        public double getBearingTo(VectorBody i_vReceiver_Position)
        {
            double dBearing = -999; // invalid value
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                Vector3d vAbsolute_Offset = i_vReceiver_Position.vPosition_Vector - vPosition.vPosition_Vector;
                double dInv_Mag_Recv = 1.0 / i_vReceiver_Position.vPosition_Vector.magnitude;
                double dInv_Mag_VOR = 1.0 / vPosition.vPosition_Vector.magnitude;
                double dInv_Mag_AO = 1.0 / vAbsolute_Offset.magnitude;

                double dDistance_m = vAbsolute_Offset.magnitude;

                // determine the vector that describes the normal to the Great Circle containing the receiver and the north pole
                Vector3d vLocal_North = new Vector3d();
                vLocal_North.x = i_vReceiver_Position.vPosition_Vector.y;
                vLocal_North.y = -i_vReceiver_Position.vPosition_Vector.x;
                vLocal_North.z = 0.0;
                vLocal_North.Normalize();


                // determine the vector that describes the normal to the Great Circle containing the station and the receiver
                Vector3d vReceiver_Station_Bearing_Circle = new Vector3d();
                vReceiver_Station_Bearing_Circle = vPosition.Cross(i_vReceiver_Position);
                vReceiver_Station_Bearing_Circle.Normalize();

                double dDot = vReceiver_Station_Bearing_Circle.x * vLocal_North.x + vReceiver_Station_Bearing_Circle.y * vLocal_North.y;
                if (vReceiver_Station_Bearing_Circle.z >= 0.0)
                    dBearing = Math.Acos(dDot) * 180.0 / Math.PI;
                else
                    dBearing = 360.0 - Math.Acos(dDot) * 180.0 / Math.PI;
            }

            return dBearing;
        }
    }

    public class COMM : NAVbase
    {
        public bool bClose_Spacing = false;
        public string sMessage = "";
        public override void OnSave(ConfigNode i_Node)
        {
            base.OnSave(i_Node);
            i_Node.AddValue("Close Spacing", bClose_Spacing);
            if (sMessage != null)
            {
                i_Node.AddValue("Message Length", sMessage.Length);
                for (int i = 0; i < sMessage.Length; i++)
                {
                    string sKey = "Message Data " + i;
                    i_Node.AddValue(sKey, sMessage[i]);
                }
            }
            else
                i_Node.AddValue("Message Length", 0);
        }
        public override void OnLoad(ConfigNode i_Node)
        {
            base.OnLoad(i_Node);
            string sSpacing = i_Node.GetValue("Close Spacing");
            if (sSpacing != null)
                bClose_Spacing = Boolean.Parse(sSpacing);
            sMessage = "";
            string sMsg_Length = i_Node.GetValue("Message Length");
            if (sMsg_Length != null)
            {
                int iLength = Int32.Parse(sMsg_Length);
                if (iLength != 0)
                {
                    char[] chList = new char[iLength];
                    for (int i = 0; i < iLength; i++)
                    {
                        string sKey = "Message Data " + i;
                        string sValue = i_Node.GetValue(sKey);
                        if (sValue != null)
                            chList[i] = sValue[0];
                    }
                    sMessage = new string(chList);
                }

            }
        }
        public override double getFrequency()
        {
            double dFrequency;
            dFrequency = 118.0 + iChannel * 0.025 / 3.0;
            return dFrequency;
        }
        public override void incrementChannel()
        {
            if (bClose_Spacing)
                iChannel++;
            else
                iChannel += 3;
            if (iChannel > 2279)
                iChannel -= 2280;
        }
        public override void decrementChannel()
        {
            if (bClose_Spacing)
                iChannel--;
            else
                iChannel -= 3;
            if (iChannel < 0)
                iChannel += 2280;
        }
        public override void incrementChannelLarge()
        {
            iChannel += 120;
            if (iChannel > 2279)
                iChannel -= 2280;
        }
        public override void decrementChannelLarge()
        {
            iChannel -= 120;
            if (iChannel < 0)
                iChannel += 2280;
        }
    }

    public class VOR : NAVbase
    {
        public override double getFrequency()
        {
            double dFrequency;
            if (iChannel <= 39)
                dFrequency = 108.0 + (iChannel & 1) * 0.05f + (iChannel / 2) * 0.2f;
            else
                dFrequency = 112.0 + (iChannel - 40) * 0.05f;
            return dFrequency;
        }
        public override void incrementChannel()
        {
            iChannel++;
            if (iChannel > 159)
                iChannel = 0;
        }
        public override void decrementChannel()
        {
            iChannel--;
            if (iChannel < 0)
                iChannel = 159;
        }
        public override void incrementChannelLarge()
        {
            if (iChannel <= 30)
                iChannel += 10;
            else if (iChannel >= 40 && iChannel < 140)
            {
                iChannel += 20;
            }
            else if (iChannel >= 140)
            {
                int iCh4 = (iChannel - 140) / 4;
                iCh4 *= 2;
                iCh4 += (iChannel & 1);
                iChannel = iCh4;
                //140 = 117.05 -> 108.0 (0)
                //141 = 117.05 -> 108.05 (1)
                //142 = 117.1 -> 108.0 (0)
                //143 = 117.15 -> 108.05 (1)
                //144 = 117.2 -> 108.2 (2)
                //145 = 117.25 -> 108.25 (3)
                //146 = 117.3 -> 108.2 (2)
                //147 = 117.35 -> 108.25 (3)
                //148 = 117.4 -> 108.4 (4)
                //149 = 117.45 -> 108.45 (5)
                //150 = 117.5 -> 108.4 (4)
                //151 = 117.55 -> 108.45 (5)
                //152 = 117.6 -> 108.6 (6)
                //153 = 117.65 -> 108.65 (7)
                //154 = 117.7 -> 108.6 (6)
                //155 = 117.75 -> 108.65 (7)
                //156 = 117.8 -> 108.85 (8)
                //157 = 117.85 -> 108.85  (9)
                //158 = 117.9 -> 108.8 (8)
                //159 = 117.95 -> 108.85 (9)
            }
            else //if (iChannel >= 30 && iChannel < 40)
            {
                int iCh = (iChannel - 30);
                if ((iCh & 1) == 1)
                    iCh--;
                iCh *= 2;
                if ((iChannel & 1) == 1)
                    iCh++;
                iChannel = iCh + 40;


                //30 = 111.00 -> 112.00 (40)
                //31 = 111.05 -> 112.05 (41)
                //32 = 111.20 -> 112.20 (44)
                //33 = 111.25 -> 112.25 (45)
                //34 = 111.40 -> 112.40 (48)
                //35 = 111.45 -> 112.45 (49)
            }
        }
        public override void decrementChannelLarge()
        {
            if (iChannel >= 10 && iChannel <= 41)
                iChannel -= 10;
            else if (iChannel >= 60)
            {
                iChannel -= 20;
            }
            else if (iChannel > 40)
            {
                // 40 = 112.00 -> 111.00 (30)
                // 41 = 112.05 -> 111.05 (31)
                // 42 = 112.10 -> 111.00 (30)
                // 43 = 112.15 -> 111.05 (31)
                // 42 = 112.20 -> 111.20 (32)
                // 43 = 112.25 -> 111.25 (33)
                //...

                int iCh4 = (iChannel - 40) / 4;
                iCh4 *= 2;
                iCh4 += (iChannel & 1);
                iChannel = iCh4 + 30;
            }
            else //if (iChannel < 10)
            {
                int iCh = (iChannel - 10);
                if ((iCh & 1) == 1)
                    iCh--;
                iCh *= 2;
                if ((iChannel & 1) == 1)
                    iCh++;
                iChannel = iCh + 140;


                //0 = 108.00 -> 117.00 (140)
                //1 = 108.05 -> 112.05 (141)
                //2 = 108.20 -> 112.20 (144)
                //3 = 108.25 -> 112.25 (145)
                //4 = 108.40 -> 112.40 (148)
                //5 = 108.45 -> 112.45 (149)
            }
        }

        bool bVOR_Test_Station = false;
        public double dRadial_Offset = 0.0; // allows user to enter a magnetic offset from North for bearing output

        public override void OnSave(ConfigNode i_Node)
        {
            base.OnSave(i_Node);
            i_Node.AddValue("Radial Offset", dRadial_Offset.ToString("0.0####"));
            i_Node.AddValue("Test Station", bVOR_Test_Station);
        }
        public override void OnLoad(ConfigNode i_Node)
        {
            base.OnLoad(i_Node);
            string sOffset = i_Node.GetValue("Radial Offset");
            if (sOffset != null)
                dRadial_Offset = Double.Parse(sOffset);
            string sTest = i_Node.GetValue("Test Station");
            if (sTest != null)
                bVOR_Test_Station = Boolean.Parse(sTest);
        }

        public bool inZoneOfConfusion(VectorBody i_vReceiver_Position)
        {
            bool bIn_ZOC = false;
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                Vector3d vAbsolute_Offset = i_vReceiver_Position.vPosition_Vector - vPosition.vPosition_Vector;
                double dInv_Mag_VOR = 1.0 / vPosition.vPosition_Vector.magnitude;
                double dInv_Mag_AO = 1.0 / vAbsolute_Offset.magnitude;

                double dCos_Angle = vPosition.Dot(vAbsolute_Offset) * dInv_Mag_AO * dInv_Mag_VOR;
                bIn_ZOC = (Math.Acos(dCos_Angle) < (Math.PI * 0.25)); // 45 degree angle above the station
            }
            return bIn_ZOC;
        }

        public double getRadial(VectorBody i_vReceiver_Position)
        {
            if (bVOR_Test_Station)
                return 0.0;
            else
                return getBearingFrom(i_vReceiver_Position) + dRadial_Offset;
        }
    }
   
    public class LOC: NAVbase
    {
        public override double getFrequency()
        {
            return 108.1 + (iChannel & 1) * 0.05f + (iChannel / 2) * 0.2f;
        }
        public override void incrementChannel()
        {
            iChannel++;
            if (iChannel > 39)
                iChannel = 0;
        }
        public override void decrementChannel()
        {
            iChannel--;
            if (iChannel < 0)
                iChannel = 39;
        }
        public override void incrementChannelLarge()
        {
            iChannel += 10;
            if (iChannel > 39)
                iChannel -= 40;
        }
        public override void decrementChannelLarge()
        {
            iChannel -= 10;
            if (iChannel < 0)
                iChannel += 40;
        }

        public double dBeam_Bearing = 0.0; //  // degrees relative to true north
        public double dBeam_Half_Width = 5.0; // degrees
        public override void OnSave(ConfigNode i_Node)
        {
            base.OnSave(i_Node);
            i_Node.AddValue("Beam Bearing", dBeam_Bearing.ToString("0.0####"));
            i_Node.AddValue("Beam Half Width", dBeam_Half_Width.ToString("0.0####"));
        }
        public override void OnLoad(ConfigNode i_Node)
        {
            base.OnLoad(i_Node);
            string sBearing = i_Node.GetValue("Beam Bearing");
            if (sBearing != null)
                dBeam_Bearing = Double.Parse(sBearing);
            string sWidth = i_Node.GetValue("Beam Half Width");
            if (sWidth != null)
                dBeam_Half_Width = Double.Parse(sWidth);
        }

        public double getOffset(VectorBody i_vReceiver_Position)
        {
            double dOffset = 0.0;
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                double dDeg_Rad = Math.PI / 180.0;
                double dRelative_Bearing = getBearingFrom(i_vReceiver_Position) - dBeam_Bearing;
                if (dRelative_Bearing > 180.0)
                    dRelative_Bearing -= 360.0;
                else if (dRelative_Bearing < 180.0)
                    dRelative_Bearing += 360.0;
                dOffset = Math.Sin(dRelative_Bearing * dDeg_Rad) / Math.Sin(dBeam_Half_Width * dDeg_Rad); // the division is to account for full scale being defined by the beamwidth
                if (dRelative_Bearing < -90.0 || dRelative_Bearing > 90.0)
                    dOffset *= -1.0;
            }
            return dOffset;
        }
    }
    public class GLS : NAVbase // glideslope signal
    {
        public override double getFrequency()
        {
            return 108.1 + (iChannel & 1) * 0.05f + (iChannel / 2) * 0.2f;
        }
        public override void incrementChannel()
        {
            iChannel++;
            if (iChannel > 39)
                iChannel = 0;
        }
        public override void decrementChannel()
        {
            iChannel--;
            if (iChannel < 0)
                iChannel = 39;
        }
        public override void incrementChannelLarge()
        {
            iChannel += 10;
            if (iChannel > 39)
                iChannel -= 40;
        }
        public override void decrementChannelLarge()
        {
            iChannel -= 10;
            if (iChannel < 0)
                iChannel += 40;
        }

        //public double dBeam_Bearing = 0.0; // degrees relative to true north
        public double dGlidepath_Angle = 3.0; // degrees above horizonal
        public double dBeam_Half_Width = 1.5; // degrees 
        public override void OnSave(ConfigNode i_Node)
        {
            base.OnSave(i_Node);
            i_Node.AddValue("Glidepath Angle", dGlidepath_Angle.ToString("0.0####"));
            i_Node.AddValue("Beam Half Width", dBeam_Half_Width.ToString("0.0####"));
        }
        public override void OnLoad(ConfigNode i_Node)
        {
            base.OnLoad(i_Node);
            string sAngle = i_Node.GetValue("Glidepath Angle");
            if (sAngle != null)
                dGlidepath_Angle = Double.Parse(sAngle);
            string sWidth = i_Node.GetValue("Beam Half Width");
            if (sWidth != null)
                dBeam_Half_Width = Double.Parse(sWidth);
        }

        public double getOffset(VectorBody i_vReceiver_Position)
        {
            double dOffset = 0.0;
            if (i_vReceiver_Position.bBody == vPosition.bBody)
            {
                double dRad_Deg = 180.0 / Math.PI;
                Vector3d vAbsolute_Offset = i_vReceiver_Position.vPosition_Vector - vPosition.vPosition_Vector;
                double dInv_Mag_Sta = 1.0 / vPosition.vPosition_Vector.magnitude;
                double dInv_Mag_AO = 1.0 / vAbsolute_Offset.magnitude;
                double dZenith_Angle = Math.Acos(vPosition.Dot(vAbsolute_Offset) * dInv_Mag_Sta * dInv_Mag_AO) * dRad_Deg;
                double dGLS_Angle = ((90.0 - dZenith_Angle) - dGlidepath_Angle); // - = below glidepath
                dOffset = dGLS_Angle / dBeam_Half_Width;
            }
            return dOffset;
        }

    }
    public class NDB: NAVbase
    {
        public override double getFrequency()
        {
            return 190.0 + iChannel;
        }
        public override void incrementChannel()
        {
            iChannel++;
            if (iChannel > 1559)
                iChannel = 0;
        }
        public override void decrementChannel()
        {
            iChannel--;
            if (iChannel < 0)
                iChannel = 1559;
        }
        public override void incrementChannelLarge()
        {
            iChannel += 100;
            if (iChannel > 1559)
                iChannel -= 1560;
        }
        public override void decrementChannelLarge()
        {
            iChannel -= 100;
            if (iChannel < 0)
                iChannel += 1560;
        }

    }

    public abstract class NAVreceiver
    {
        public VectorBody vPosition = new VectorBody();
        public int iChannel = 0;
        public int iStandby_Channel = 0;

        public virtual void OnSave(ConfigNode i_Node)
        {
            i_Node.AddValue("Channel", iChannel);
            i_Node.AddValue("Standby Channel", iStandby_Channel);
        }
        public virtual void OnLoad(ConfigNode i_Node)
        {
            string sChannel = i_Node.GetValue("Channel");
            if (sChannel != null)
                iChannel = Int32.Parse(sChannel);
            string sStbyChannel = i_Node.GetValue("Standby Channel");
            if (sStbyChannel != null)
                iStandby_Channel = Int32.Parse(sStbyChannel);
        }

        public void swapChannels()
        {
            int iSwap = iChannel;
            iChannel = iStandby_Channel;
            iStandby_Channel = iSwap;
        }

        public abstract double getFrequency();
        public abstract double getStandbyFrequency();
        public abstract void incrementChannel();
        public abstract void decrementChannel();
        public abstract void incrementChannelLarge();
        public abstract void decrementChannelLarge();
        public abstract void incrementStandbyChannel();
        public abstract void decrementStandbyChannel();
        public abstract void incrementStandbyChannelLarge();
        public abstract void decrementStandbyChannelLarge();
    }
    public class VHFreceiver : NAVreceiver
    { 
        public override double getFrequency()
        {
            return 108.0 + iChannel * 0.05;
        }
        public bool isActiveILS()
        {
            return (iChannel < 80 && (((iChannel / 2) & 1) == 1));
        }
        public bool isStandbyILS()
        {
            return (iStandby_Channel < 80 && (((iStandby_Channel / 2) & 1) == 1));
        }
        public override double getStandbyFrequency()
        {
            return 108.0 + iStandby_Channel * 0.05;
        }
        public override void incrementChannel()
        {
            iChannel++;
            if (iChannel > 199)
                iChannel = 0;
        }
        public override void decrementChannel()
        {
            iChannel--;
            if (iChannel < 0)
                iChannel = 199;
        }
        public override void incrementChannelLarge() // increments to e.g. 109.xx from 108.xx
        {
            iChannel+=20;
            if (iChannel > 199)
                iChannel -= 200;
        }
        public override void decrementChannelLarge() // decrements to e.g. 108.xx from 109.xx
        {
            iChannel -= 20;
            if (iChannel < 0)
                iChannel -= 200;
        }
        public override void incrementStandbyChannel()
        {
            iStandby_Channel++;
            if (iStandby_Channel > 199)
                iStandby_Channel = 0;
        }
        public override void decrementStandbyChannel()
        {
            iStandby_Channel--;
            if (iStandby_Channel < 0)
                iStandby_Channel = 199;
        }
        public override void incrementStandbyChannelLarge() // increments to e.g. 109.xx from 108.xx
        {
            iStandby_Channel += 20;
            if (iStandby_Channel > 199)
                iStandby_Channel -= 200;
        }
        public override void decrementStandbyChannelLarge() // decrements to e.g. 108.xx from 109.xx
        {
            iStandby_Channel -= 20;
            if (iStandby_Channel < 0)
                iStandby_Channel -= 200;
        }
    }
    public class MFreceiver : NAVreceiver
    {
        public override double getFrequency()
        {
            return 190.0 + iChannel;
        }
        public override void incrementChannel()
        {
            iChannel++;
            if (iChannel > 1559)
                iChannel = 0;
        }
        public override void decrementChannel()
        {
            iChannel--;
            if (iChannel < 0)
                iChannel = 1559;
        }
        public override void incrementChannelLarge()
        {
            iChannel += 100;
            if (iChannel > 1559)
                iChannel -= 1560;
        }
        public override void decrementChannelLarge()
        {
            iChannel -= 100;
            if (iChannel < 0)
                iChannel += 1560;
        }

        public override double getStandbyFrequency()
        {
            return 190.0 + iStandby_Channel;
        }
        public override void incrementStandbyChannel()
        {
            iStandby_Channel++;
            if (iStandby_Channel > 1559)
                iStandby_Channel = 0;
        }
        public override void decrementStandbyChannel()
        {
            iStandby_Channel--;
            if (iStandby_Channel < 0)
                iStandby_Channel = 1559;
        }
        public override void incrementStandbyChannelLarge()
        {
            iStandby_Channel += 100;
            if (iStandby_Channel > 1559)
                iStandby_Channel -= 1560;
        }
        public override void decrementStandbyChannelLarge()
        {
            iStandby_Channel -= 100;
            if (iStandby_Channel < 0)
                iStandby_Channel += 1560;
        }
    }
    public class COMMreceiver : NAVreceiver
    {
        public bool bClose_Spacing = false;
        public override void OnSave(ConfigNode i_Node)
        {
            base.OnSave(i_Node);
            i_Node.AddValue("Close Spacing", bClose_Spacing);
        }
        public override void OnLoad(ConfigNode i_Node)
        {
            base.OnLoad(i_Node);
            string sSpacing = i_Node.GetValue("Close Spacing");
            if (sSpacing != null)
                bClose_Spacing = Boolean.Parse(sSpacing);
        }
        public override double getFrequency()
        {
            double dFrequency;
            dFrequency = 118.0 + iChannel * 0.025 / 3.0;
            return dFrequency;
        }
        public override void incrementChannel()
        {
            if (bClose_Spacing)
                iChannel++;
            else
                iChannel += 3;
            if (iChannel > 2279)
                iChannel -= 2280;
        }
        public override void decrementChannel()
        {
            if (bClose_Spacing)
                iChannel--;
            else
                iChannel -= 3;
            if (iChannel < 0)
                iChannel += 2280;
        }
        public override void incrementChannelLarge()
        {
            iChannel += 120;
            if (iChannel > 2279)
                iChannel -= 2280;
        }
        public override void decrementChannelLarge()
        {
            iChannel -= 120;
            if (iChannel < 0)
                iChannel += 2280;
        }
        public override double getStandbyFrequency()
        {
            double dFrequency;
            dFrequency = 118.0 + iStandby_Channel * 0.025 / 3.0;
            return dFrequency;
        }
        public override void incrementStandbyChannel()
        {
            if (bClose_Spacing)
                iStandby_Channel++;
            else
                iStandby_Channel += 3;
            if (iStandby_Channel > 2279)
                iStandby_Channel -= 2280;
        }
        public override void decrementStandbyChannel()
        {
            if (bClose_Spacing)
                iStandby_Channel--;
            else
                iStandby_Channel -= 3;
            if (iStandby_Channel < 0)
                iStandby_Channel += 2280;
        }
        public override void incrementStandbyChannelLarge()
        {
            iStandby_Channel += 120;
            if (iStandby_Channel > 2279)
                iStandby_Channel -= 2280;
        }
        public override void decrementStandbyChannelLarge()
        {
            iStandby_Channel -= 120;
            if (iStandby_Channel < 0)
                iStandby_Channel += 2280;
        }
    }

    public class NAV_receiver_station_properties
    {
        public int iStation_ID;
        public double dFrequency;
        public double dFlux;

    }
    public class NAV_receiver_station_poperty_comparer : Comparer<NAV_receiver_station_properties>
    {
        public override int Compare(NAV_receiver_station_properties i_cLHO,NAV_receiver_station_properties i_cRHO)
        {
            // A null value means that this object is greater.
            if (i_cLHO.dFrequency > i_cRHO.dFrequency ||
                (i_cLHO.dFrequency == i_cRHO.dFrequency && i_cLHO.dFlux < i_cRHO.dFlux)) // invert flux so that higher flux = lower index in list
                return 1;
            else if (i_cLHO.dFrequency < i_cRHO.dFrequency || (i_cLHO.dFrequency == i_cRHO.dFrequency && i_cLHO.dFlux > i_cRHO.dFlux))
                return -1;
            else
                return 0;
        }
    }


    public class NAV_receiver_metadata
    {
        public bool bActive = false;
        public NAVreceiver cReceiver;
        public double dLast_Update;
        public List<NAV_receiver_station_properties> listStations = null;
    }

    public class NAVmaster
    {
        private static List<NAVbase> listStations = null;
        private static List<NAV_receiver_metadata> listReceiver = null;
        private double dLast_Time = -1;
        public void onReceiverUpdate()
        {
            bool bDone = false;
            if (listReceiver != null && Time.time != dLast_Time && listStations != null) // no work to do if there are no receivers or if we have already tried to do work this timestep
            {
                for (int i = 0; i < listReceiver.Count && !bDone; i++)
                {
                    if (listReceiver[i].bActive && (Time.time - listReceiver[i].dLast_Update) > 1.0)
                    {
                        if (listReceiver[i].listStations == null)
                            listReceiver[i].listStations = new List<NAV_receiver_station_properties>();
                        listReceiver[i].listStations.Clear();
                        for (int j = 0; j < listStations.Count; j++)
                        {
                            NAV_receiver_station_properties cProp = new NAV_receiver_station_properties();
                            cProp.dFrequency = listStations[j].getFrequency();
                            cProp.dFlux = listStations[j].getFlux(listReceiver[i].cReceiver.vPosition);
                            cProp.iStation_ID = j;
                            listReceiver[i].listStations.Add(cProp);
                        }
                        if (listReceiver[i].listStations.Count != 0)
                            listReceiver[i].listStations.Sort(new NAV_receiver_station_poperty_comparer());

//                        string sMsg = "Station count " + listReceiver[i].listStations.Count + " " + listStations.Count;
//                        Debug.Log(sMsg);

                        listReceiver[i].dLast_Update = Time.time;
                        bDone = true; // only process one receiver per frame
                    }
                }
                dLast_Time = Time.time;
            }
        }
        public NAVbase getStation(int i_iReceiver_ID)
        {
            NAVbase cRet = null;
            if (listReceiver != null && i_iReceiver_ID > 0 && i_iReceiver_ID < listReceiver.Count && listReceiver[i_iReceiver_ID].listStations != null)
            {
                int iStation = -1;
                for (int i = 0; i < listReceiver[i_iReceiver_ID].listStations.Count && iStation == -1; i++)
                {
                    // find the first station in the list that has the right frequency. the list is already sorted by flux
                    if (Math.Abs(listReceiver[i_iReceiver_ID].listStations[i].dFrequency - listReceiver[i_iReceiver_ID].cReceiver.getFrequency()) < 0.01)
                    {
                        iStation = listReceiver[i_iReceiver_ID].listStations[i].iStation_ID;
                    }
                }
                if (iStation != -1)
                    cRet = listStations[iStation];
            }
            return cRet;
        }
        public NAVbase getStationGLS(int i_iReceiver_ID)
        { // an ILS 
            NAVbase cRet = null;
            if (listReceiver != null && i_iReceiver_ID > 0 && i_iReceiver_ID < listReceiver.Count && listReceiver[i_iReceiver_ID].listStations != null)
            {
                int iStation = -1;
                for (int i = 0; i < listReceiver[i_iReceiver_ID].listStations.Count && iStation == -1; i++)
                {
                    // find the first station in the list that has the right frequency. the list is already sorted by flux
                    if (Math.Abs(listReceiver[i_iReceiver_ID].listStations[i].dFrequency - listReceiver[i_iReceiver_ID].cReceiver.getFrequency()) < 0.01)
                    {
                        GLS cStationGLS = listStations[listReceiver[i_iReceiver_ID].listStations[i].iStation_ID] as GLS;
                        if (cStationGLS != null)
                            iStation = listReceiver[i_iReceiver_ID].listStations[i].iStation_ID;
                    }
                }
                if (iStation != -1)
                    cRet = listStations[iStation];
            }
            return cRet;
        }

        public int registerReceiver(NAVreceiver i_Receiver)
        {
            NAV_receiver_metadata cRcvr = new NAV_receiver_metadata();
            if (listReceiver == null)
                listReceiver = new List<NAV_receiver_metadata>();
            cRcvr.bActive = true;
            cRcvr.cReceiver = i_Receiver;
            cRcvr.listStations = new List < NAV_receiver_station_properties >();
            string sMsg = "NAV Register Receiver " + listReceiver.Count + " " + i_Receiver.getFrequency();
            Debug.Log(sMsg);
            listReceiver.Add(cRcvr);
            return listReceiver.Count - 1;
        }
        public int updateReceiver(int i_iReceiver_ID, NAVreceiver i_Receiver)
        {
            int iRet = -1;
            if (listReceiver != null && i_iReceiver_ID > 0 && i_iReceiver_ID < listReceiver.Count)
            {
                iRet = i_iReceiver_ID;
                //string sMsg = "NAV Update Receiver " + i_iReceiver_ID + " " + i_Receiver.getFrequency();
                //Debug.Log(sMsg);
                listReceiver[i_iReceiver_ID].bActive = true;
                listReceiver[i_iReceiver_ID].cReceiver = i_Receiver;
            }
            return iRet;
        }
        public void deregisterReceiver(int i_iReceiver_ID)
        {
            if (listReceiver != null && i_iReceiver_ID > 0 && i_iReceiver_ID < listReceiver.Count)
            {
                listReceiver[i_iReceiver_ID].bActive = false;
                listReceiver[i_iReceiver_ID].listStations.Clear(); // empty the list
            }
        }


        public int registerStation(NAVbase i_Station)
        {
            if (listStations == null)
                listStations = new List<NAVbase>();
            string sMsg = "NAV Master Register Station " + i_Station.sStation_ID + " " + listStations.Count;
            Debug.Log(sMsg);
            listStations.Add(i_Station);
            return listStations.Count - 1;
        }
        public int updateStation(int i_iStation_ID, NAVbase i_Station)
        {
            int iRet = -1;
            if (listStations != null && i_iStation_ID > 0 && i_iStation_ID < listStations.Count)
            {
                iRet = i_iStation_ID;
                //string sMsg = "NAV Update Station" + i_iStation_ID + " " + i_Station.sStation_ID + " " + listStations.Count;
                //Debug.Log(sMsg);
                listStations[i_iStation_ID] = i_Station;
            }
            return iRet;
        }
        public void deregisterStation(int i_iStation_ID)
        {
            if (listStations != null && i_iStation_ID > 0 && i_iStation_ID < listStations.Count)
            {
                listStations[i_iStation_ID].dTransmit_Power = 0;
            }
        }
    }
}
