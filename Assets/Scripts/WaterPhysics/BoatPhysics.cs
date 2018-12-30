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
            crossSectionMeshGenerator = new CrossSectionMeshGenerator(this.transform, this.meshFilter);

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
                ApplyBuoyancyForce();
            }
        }

        private void ApplyBuoyancyForce()
        {
            List<TriangleData> underwaterTriangleData = crossSectionMeshGenerator.cuttedMesh;
            for (int i = 0; i < underwaterTriangleData.Count; i++)
            {
                TriangleData triangle = underwaterTriangleData[i];

                Vector3 force = waterDensity * Physics.gravity.y * triangle.distanceToSurface * triangle.area * triangle.normal;
                force.x = 0;
                force.z = 0;

                boatRigidBody.AddForceAtPosition(force, triangle.center);

                
                Debug.DrawRay(triangle.center, force.normalized, Color.blue);
                //Debug.DrawRay(triangle.center, triangle.normal, Color.green);

            }
        }
    }

}
