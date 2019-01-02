using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaterPhysics
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(Rigidbody))]
    public class BoatPhysics : MonoBehaviour
    {

        public GameObject underwaterObject;

        [Space]
        public float waterDensity = 1027f;

        private Rigidbody boatRigidBody;

        private Mesh underwaterMesh;

        private MeshFilter meshFilter;
        private CrossSectionMeshGenerator crossSectionMeshGenerator;

        // Start is called before the first frame update
        void Start()
        {
            boatRigidBody = GetComponent<Rigidbody>();

            meshFilter = GetComponent<MeshFilter>();
            crossSectionMeshGenerator = new CrossSectionMeshGenerator(this.transform, this.meshFilter, this.boatRigidBody);

            underwaterMesh = underwaterObject.GetComponent<MeshFilter>().mesh;
        }

        // Update is called once per frame
        void Update()
        {
            crossSectionMeshGenerator.GenerateMeshUnder();

            crossSectionMeshGenerator.DisplayMesh(underwaterMesh);
        }

        void FixedUpdate()
        {
            if(underwaterMesh.vertexCount > 0)
            {
                ApplyWaterForces();
            }
        }

        void ApplyWaterForces()
        {
            List<TriangleData> underwaterTriangleData = crossSectionMeshGenerator.cuttedMesh;

            float Cf = WaterPhysicsMath.ResistanceCoeficient(boatRigidBody.velocity.magnitude, CalculateUnderWaterLength());

            float boatArea = crossSectionMeshGenerator.objectTotalArea;
            float boatMass = boatRigidBody.mass;

            List<SlammingForceData> objectSlammingForceData = crossSectionMeshGenerator.slammingForceData;
            List<int> originalTriangleIndex = crossSectionMeshGenerator.originalTriangleIndex;

            CalculateTriangleVelocities(ref objectSlammingForceData);

            for (int i = 0; i < underwaterTriangleData.Count; i++)
            {
                TriangleData triangle = underwaterTriangleData[i];
                SlammingForceData slammingForceData = objectSlammingForceData[originalTriangleIndex[i]];

                Vector3 waterForce = Vector3.zero;

                waterForce += WaterPhysicsMath.BuoyancyForce(triangle, WaterPhysicsMath.RHO_OCEAN_WATER);
                waterForce += WaterPhysicsMath.PressureDrag(triangle);
                waterForce += WaterPhysicsMath.ViscousWaterResistanceForce(WaterPhysicsMath.RHO_OCEAN_WATER, triangle, Cf);
                waterForce += WaterPhysicsMath.SlammingForce(slammingForceData, triangle, boatMass, boatArea, Time.fixedDeltaTime);


                boatRigidBody.AddForceAtPosition(waterForce, triangle.center);


                //Debug.DrawRay(triangle.center, buoyancy.normalized, Color.blue);
                //Debug.DrawRay(triangle.center, triangle.normal, Color.green);

            }
        }

        private void CalculateTriangleVelocities(ref List<SlammingForceData> slammingForceData)
        {
            for(int i = 0; i < slammingForceData.Count; i++)
            {
                slammingForceData[i].previousVelocity = slammingForceData[i].velocity;

                slammingForceData[i].velocity = WaterPhysicsMath.TrianglePointVelocity(boatRigidBody.velocity, boatRigidBody.angularVelocity, boatRigidBody.worldCenterOfMass, slammingForceData[i].center);
            }
        }

        private float CalculateUnderWaterLength()
        {
            return underwaterMesh.bounds.size.z;
        }
    }

}
