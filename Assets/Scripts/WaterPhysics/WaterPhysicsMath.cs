using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterPhysics
{

    public static class WaterPhysicsMath
    {

        //
        // Constants
        //

        //Densities [kg/m^3]

        //Fluid
        public const float RHO_WATER = 1000f;
        public const float RHO_OCEAN_WATER = 1027f;
        public const float RHO_SUNFLOWER_OIL = 920f;
        public const float RHO_MILK = 1035f;
        //Gas
        public const float RHO_AIR = 1.225f;
        public const float RHO_HELIUM = 0.164f;
        //Solid
        public const float RHO_GOLD = 19300f;

        //Drag coefficients
        public const float C_d_flat_plate_perpendicular_to_flow = 1.28f;

        // Viscosity
        public const float VISCOSITY_WATER_20 = 0.000001f;

        public const float R_MAX = 20f;



        public static Vector3 PressureDrag(TriangleData triangle)
        {
            Vector3 pressureDrag = Vector3.zero;

            float velocity = triangle.pointVelocity.magnitude;
            float cosTheta = triangle.cosTheta;

            float velocityReference = DebugPhysics.current.velocityReference;

            velocity = velocity / velocityReference;


            if (cosTheta > 0)
            {
                float C_PD1 = DebugPhysics.current.C_PD1;
                float C_PD2 = DebugPhysics.current.C_PD2;
                float f_P = DebugPhysics.current.f_P;

                pressureDrag = -(C_PD1 * velocity + C_PD2 * velocity * velocity) * triangle.area * Mathf.Pow(cosTheta, f_P) * triangle.normal;
            }
            else
            {
                float C_SD1 = DebugPhysics.current.C_SD1;
                float C_SD2 = DebugPhysics.current.C_SD2;
                float f_S = DebugPhysics.current.f_S;

                pressureDrag = (C_SD1 * velocity + C_SD2 * velocity * velocity) * triangle.area * Mathf.Pow(cosTheta, f_S) * triangle.normal;
            }

            AssertForce(ref pressureDrag);

            return pressureDrag;
        }

        public static Vector3 BuoyancyForce(TriangleData triangleData, float rho)
        {

            Vector3 buoyancy = rho * Physics.gravity.y * triangleData.distanceToSurface * triangleData.area * triangleData.normal;
            buoyancy.x = 0;
            buoyancy.z = 0;

            AssertForce(ref buoyancy);

            //Debug.DrawRay(triangleData.center, buoyancy, Color.blue);

            return buoyancy;
        }

        public static Vector3 ViscousWaterResistanceForce(float rho, TriangleData triangleData, float Cf)
        {
            Vector3 resistance = Vector3.zero;

            Vector3 normal = triangleData.normal;
            Vector3 velocity = triangleData.pointVelocity;

            Vector3 tangencialVelocity = velocity - Vector3.Cross(velocity, normal);

            Vector3 tangencialDirection = -tangencialVelocity.normalized;

            Vector3 v_fi = velocity.magnitude * tangencialDirection;

            resistance = 0.5f * rho * Cf * triangleData.area * v_fi.magnitude * v_fi;

            AssertForce(ref resistance);

            return resistance;
        }

        public static float ResistanceCoeficient(float velocity, float length)
        {
            float Rn = velocity * length / WaterPhysicsMath.VISCOSITY_WATER_20;

            float d = Mathf.Log10(Rn) - 2;
            float Cf = 0.075f / (d * d);

            return Cf;
        }

        public static Vector3 SlammingForce(SlammingForceData slammingForceData, TriangleData triangleData, float objectMass, float objectArea, float dt)
        {
            if (triangleData.cosTheta < 0f || slammingForceData.originalArea <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 d_V_current = slammingForceData.submergedArea * slammingForceData.velocity;
            Vector3 d_V_previous = slammingForceData.previousSubmergedArea * slammingForceData.previousVelocity;

            Vector3 r = (d_V_current - d_V_previous) / (slammingForceData.originalArea * dt);

            float acc = r.magnitude;

            Vector3 F_Stopping = objectMass * triangleData.pointVelocity * (2f * triangleData.area) / objectArea;

            float p = DebugPhysics.current.p;
            float acc_max = DebugPhysics.current.acc_max;
            float slammingCheat = DebugPhysics.current.slammingCheat;

            Vector3 slammingForce = -1 * Mathf.Pow(Mathf.Clamp01(acc / acc_max), p) * triangleData.cosTheta * F_Stopping * slammingCheat;

            AssertForce(ref slammingForce);

            //Debug.DrawRay(triangleData.center, slammingForce, Color.yellow);

            return slammingForce;
        }

        public static float TriangleArea(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float a = Vector3.Distance(p1, p2);
            float c = Vector3.Distance(p1, p3);

            float area = (a * c * Mathf.Sin(Vector3.Angle(p2 - p1, p3 - p1) * Mathf.Deg2Rad)) / 2f;

            return area;
        }

        public static Vector3 TrianglePointVelocity(Vector3 linearVelocity, Vector3 angularVelocity, Vector3 centerOfMass, Vector3 triangleCenter)
        {
            return linearVelocity + Vector3.Cross(angularVelocity, triangleCenter - centerOfMass);
        }

        private static void AssertForce(ref Vector3 force)
        {
            if(float.IsNaN(force.x + force.y + force.z))
            {
                force = Vector3.zero;
            }
        }
    }
}
