using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterPhysics
{

    public class TriangleData
    {

        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;

        public Vector3 normal;
        public Vector3 center;
        public float distanceToSurface;
        public float area;

        public TriangleData(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;

            this.center = (p1 + p2 + p3) / 3.0f;

            this.normal = Vector3.Cross(p2 - p1, p3 - p1).normalized;

            this.distanceToSurface = Mathf.Abs(this.center.y);

            float a = Vector3.Distance(p1, p2);
            float c = Vector3.Distance(p1, p3);

            this.area = (a * c * Mathf.Sin(Vector3.Angle(p2 - p1, p3 - p1) * Mathf.Deg2Rad)) / 2f;
        }

    }

}
