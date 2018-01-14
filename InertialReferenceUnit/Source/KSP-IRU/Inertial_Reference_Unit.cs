using UnityEngine;
using System.IO;

namespace KSP_IRU
{
    public class InertialReferenceUnit : PartModule
    {
        //private Rect _WindowRect = new Rect();
        private Vector3d vInitialization_Position;
        //private double dInitialization_Longitude;
        private Vector3d vCurrent_Position;
        private Vector3d vOrientationD; // pitch, roll, yaw
        private Vector3d vOrientation; // pitch, roll, yaw
        private double dTime;
        private double ElectricPowerRequired = 0.001;
        //private double StandardGravitationalParameterKerbin = 3.5315984e12;


        private Vector3d[] mRotation; // true spatial rotation matrix
        private void Zero()
        {
            mRotation[0].Zero();
            mRotation[1].Zero();
            mRotation[2].Zero();
            mRotation[0][0] = 1.0;
            mRotation[1][1] = 1.0;
            mRotation[2][2] = 1.0;
            vInitialization_Position.Zero();
            vCurrent_Position.Zero();
            vOrientation.Zero();
            dTime = 0.0;
            //dInitialization_Longitude = 0.0;
        }
        private void OnDraw()
        {

        }
        private void OnWindow(int WindowID)
        {

        }
        public override void OnActive()
        {
            print("IRU Active");
        }
        public override void OnAwake()
        {
            print("IRU Awake");
        }
        public override void OnInactive()
        {
            print("IRU Inactive");
        }
        
        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                
            }
            if ((state & StartState.PreLaunch) == StartState.PreLaunch)
            {
                print("IRU Initializing");
                mRotation = new Vector3d[3];
                Zero();
                using (StreamWriter sw = File.CreateText("iru.csv"))
                {
                    string sHeader= "part, time, dt, vx, vy, vz, ox, oy, oz, oxD, oyD, ozD, vs(true), vy(true), vz(true), ax, ay, az, dox, doy, doz, doxD, doyD, dozD";
                    sw.WriteLine(sHeader);
                }
            }
        }
        public override void OnUpdate()
        {
            string ResName = "ElectricCharge";
            double dElectric_Draw = ElectricPowerRequired * TimeWarp.deltaTime;
            double elecAvail = part.RequestResource(ResName, dElectric_Draw) / dElectric_Draw;
            if (elecAvail > 0.90)
            {
                
                dTime += TimeWarp.deltaTime;
                vCurrent_Position += vessel.acceleration * TimeWarp.deltaTime;
                vOrientationD += vessel.angularVelocityD * TimeWarp.deltaTime;
                vOrientation += vessel.angularVelocity * TimeWarp.deltaTime;

                using (StreamWriter sw = File.AppendText("iru.csv"))
                {
                    string sPos = "IRU, " + dTime + ", " + TimeWarp.deltaTime + ", " + vCurrent_Position[0] + ", " + vCurrent_Position[1] + ", " + vCurrent_Position[2] +
                    ", " + vOrientation[0] + ", " + vOrientation[1] + ", " + vOrientation[2] +
                    ", " + vOrientationD[0] + ", " + vOrientationD[1] + ", " + vOrientationD[2] +
                ", " + vessel.velocityD[0] + ", " + vessel.velocityD[1] + ", " + vessel.velocityD[2] +
                ", " + vessel.acceleration[0] + ", " + vessel.acceleration[1] + ", " + vessel.acceleration[2] +
                    ", " + vessel.angularVelocity[0] + ", " + vessel.angularVelocity[1] + ", " + vessel.angularVelocity[2] +
                    ", " + vessel.angularVelocityD[0] + ", " + vessel.angularVelocityD[1] + ", " + vessel.angularVelocityD[2];
                    sw.WriteLine(sPos);
                }
            }
            else
            {
                Zero();
            }
        }
    }
}
