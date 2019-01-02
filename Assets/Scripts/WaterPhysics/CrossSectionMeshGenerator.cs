using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterPhysics
{
    public class CrossSectionMeshGenerator
    {
        public List<TriangleData> cuttedMesh = new List<TriangleData>();

        public float objectTotalArea;

        public List<SlammingForceData> slammingForceData;

        public List<int> originalTriangleIndex;

        private Transform objectTransform;
        private Vector3[] objectVertices;
        private int[] objectTriangles;

        private float[] pointDistancesToSurface;

        private Vector3[] objectVerticesGlobalPos;

        private Rigidbody objectRigidBody;

        public CrossSectionMeshGenerator(Transform transform, MeshFilter meshFilter, Rigidbody objectRigidBody)
        {
            this.objectTransform = transform;
            this.objectVertices = meshFilter.mesh.vertices;
            this.objectTriangles = meshFilter.mesh.triangles;

            this.objectVerticesGlobalPos = new Vector3[this.objectVertices.Length];

            this.pointDistancesToSurface = new float[this.objectVertices.Length];

            this.objectRigidBody = objectRigidBody;

            this.originalTriangleIndex = new List<int>();

            this.slammingForceData = InitiateObjectSlammingForceData();

            this.objectTotalArea = CalculateObjectTotalArea();
        }

        public void GenerateMeshUnder()
        {
            cuttedMesh.Clear();
            originalTriangleIndex.Clear();

            for(int i = 0; i < slammingForceData.Count; i++)
            {
                slammingForceData[i].previousSubmergedArea = slammingForceData[i].submergedArea;
            }

            // Find distance of all points to the cross section surface
            for(int i = 0; i < objectVertices.Length; i++)
            {
                objectVerticesGlobalPos[i] = objectTransform.TransformPoint(objectVertices[i]);

                pointDistancesToSurface[i] = objectVerticesGlobalPos[i].y;
            }

            // Generate underwater mesh
            AddTriangles(true);
        }

        private float CalculateObjectTotalArea()
        {
            float area = 0f;

            int i = 0;
            while(i < objectTriangles.Length)
            {
                Vector3 p1 = objectVertices[objectTriangles[i]];
                i++;

                Vector3 p2 = objectVertices[objectTriangles[i]];
                i++;

                Vector3 p3 = objectVertices[objectTriangles[i]];
                i++;

                area += WaterPhysicsMath.TriangleArea(p1, p2, p3);
            }


            return area;
        }

        private List<SlammingForceData> InitiateObjectSlammingForceData()
        {
            List<SlammingForceData> slammingForceData = new List<SlammingForceData>();

            for(int i = 0; i < objectTriangles.Length; i += 3)
            {
                TriangleData triangle = new TriangleData(objectVertices[objectTriangles[i + 0]],
                                                         objectVertices[objectTriangles[i + 1]],
                                                         objectVertices[objectTriangles[i + 2]],
                                                         objectRigidBody);

                SlammingForceData slamming = new SlammingForceData();

                slamming.originalArea = triangle.area;
                slamming.submergedArea = 0f;
                slamming.previousSubmergedArea = 0f;
                slamming.velocity = Vector3.zero;
                slamming.previousVelocity = Vector3.zero;

                slammingForceData.Add(slamming);
            }

            return slammingForceData;
        }

        private void AddTriangles(bool under)
        {
            List<VertexData> vertexData = new List<VertexData>();

            vertexData.Add(new VertexData());
            vertexData.Add(new VertexData());
            vertexData.Add(new VertexData());

            int i = 0;
            int triangleCounter = 0;
            while(i < objectTriangles.Length)
            {
                for(int j = 0; j < 3; j++)
                {
                    vertexData[j].index = j;
                    vertexData[j].globalVertexPos = objectVerticesGlobalPos[objectTriangles[i]];

                    vertexData[j].distance = pointDistancesToSurface[objectTriangles[i]];
                    if(!under)
                    {
                        vertexData[j].distance = -vertexData[j].distance;
                    }

                    i++;
                }

                slammingForceData[triangleCounter].center = (vertexData[0].globalVertexPos + vertexData[1].globalVertexPos + vertexData[2].globalVertexPos) / 3f;

                if (vertexData[0].distance > 0f && vertexData[1].distance > 0f && vertexData[2].distance > 0f)
                {
                    slammingForceData[triangleCounter].submergedArea = 0f;

                    continue;
                }


                if (vertexData[0].distance < 0f && vertexData[1].distance < 0f && vertexData[2].distance < 0f)
                {
                    cuttedMesh.Add(new TriangleData(vertexData[0].globalVertexPos, vertexData[1].globalVertexPos, vertexData[2].globalVertexPos, objectRigidBody));
                    originalTriangleIndex.Add(triangleCounter);

                    slammingForceData[triangleCounter].submergedArea = slammingForceData[triangleCounter].originalArea;
                }
                else
                {
                    vertexData.Sort((x, y) => x.distance.CompareTo(y.distance));
                    vertexData.Reverse();

                    if (vertexData[0].distance > 0f && vertexData[1].distance < 0f && vertexData[2].distance < 0f)
                    {
                        AddTriangleOneAbove(vertexData);

                        slammingForceData[triangleCounter].submergedArea = cuttedMesh[cuttedMesh.Count - 1].area + cuttedMesh[cuttedMesh.Count - 2].area;

                        originalTriangleIndex.Add(triangleCounter);
                        originalTriangleIndex.Add(triangleCounter);
                    }
                    else if (vertexData[0].distance > 0f && vertexData[1].distance > 0f && vertexData[2].distance < 0f)
                    {
                        AddTriangleTwoAbove(vertexData);

                        slammingForceData[triangleCounter].submergedArea = cuttedMesh[cuttedMesh.Count - 1].area;

                        originalTriangleIndex.Add(triangleCounter);
                    }
                }

                triangleCounter += 1;
            }
        }

        private void AddTriangleOneAbove(List<VertexData> vertexData)
        {

            int M_index = vertexData[0].index - 1;
            if(M_index < 0)
            {
                M_index = 2;
            }

            // Triangle heights in relation to the surface
            float h_H = vertexData[0].distance;
            float h_M = 0f;
            float h_L = 0f;

            Vector3 H = vertexData[0].globalVertexPos;
            Vector3 M = Vector3.zero;
            Vector3 L = Vector3.zero;

            if(vertexData[1].index == M_index)
            {
                h_M = vertexData[1].distance;
                h_L = vertexData[2].distance;

                M = vertexData[1].globalVertexPos;
                L = vertexData[2].globalVertexPos;
            }
            else
            {
                h_M = vertexData[2].distance;
                h_L = vertexData[1].distance;

                M = vertexData[2].globalVertexPos;
                L = vertexData[1].globalVertexPos;
            }

            float t_M = -h_M / (h_H - h_M);
            float t_L = -h_L / (h_H - h_L);

            Vector3 I_M = t_M * (H - M) + M;
            Vector3 I_L = t_L * (H - L) + L;

            cuttedMesh.Add(new TriangleData(M, I_M, I_L, objectRigidBody));
            cuttedMesh.Add(new TriangleData(M, I_L, L, objectRigidBody));
        }

        private void AddTriangleTwoAbove(List<VertexData> vertexData)
        {

            int H_index = vertexData[2].index + 1;
            if (H_index > 2)
            {
                H_index = 0;
            }

            // Triangle heights in relation to the surface
            float h_L = vertexData[2].distance;
            float h_M = 0f;
            float h_H = 0f;

            Vector3 L = vertexData[2].globalVertexPos;
            Vector3 M = Vector3.zero;
            Vector3 H = Vector3.zero;

            if (vertexData[1].index == H_index)
            {
                h_H = vertexData[1].distance;
                h_M = vertexData[0].distance;

                H = vertexData[1].globalVertexPos;
                M = vertexData[0].globalVertexPos;
            }
            else
            {
                h_H = vertexData[0].distance;
                h_M = vertexData[1].distance;

                H = vertexData[0].globalVertexPos;
                M = vertexData[1].globalVertexPos;
            }

            float t_M = -h_L / (h_M - h_L);
            float t_H = -h_L / (h_H - h_L);

            Vector3 I_M = t_M * (M - L) + L;
            Vector3 I_H = t_H * (H - L) + L;

            cuttedMesh.Add(new TriangleData(L, I_H, I_M, objectRigidBody));
        }

        private class VertexData
        {
            public float distance;
            public int index;
            public Vector3 globalVertexPos;
        }

        public void DisplayMesh(Mesh mesh)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for(int i = 0; i < cuttedMesh.Count; i++)
            {
                Vector3 p1 = objectTransform.InverseTransformPoint(cuttedMesh[i].p1);
                Vector3 p2 = objectTransform.InverseTransformPoint(cuttedMesh[i].p2);
                Vector3 p3 = objectTransform.InverseTransformPoint(cuttedMesh[i].p3);

                vertices.Add(p1);
                triangles.Add(vertices.Count - 1);

                vertices.Add(p2);
                triangles.Add(vertices.Count - 1);

                vertices.Add(p3);
                triangles.Add(vertices.Count - 1);
            }

            mesh.Clear();

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateBounds();
        }

    }
}
