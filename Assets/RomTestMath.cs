using System.Collections.Generic;
using UnityEngine;

namespace Gonio
{
    public class RomTestMath
    {
        private readonly Queue<Vector3> handPositionsCache = new Queue<Vector3>(3);

        public Vector3 ShoulderPosition { get; private set; }
        public float ArmLength { get; private set; }

        public bool HasEstimate { get; private set; }

        public void AddData(Vector3 handPosition)
        {
            if (this.handPositionsCache.Count > 0)
            {
                if (Vector3.Distance(handPosition, this.handPositionsCache.Peek()) > 0.5f)
                {
                    this.handPositionsCache.Enqueue(handPosition);
                }
            }
            else
            {
                this.handPositionsCache.Enqueue(handPosition);
            }

            if (this.handPositionsCache.Count >= 3)
            {
                var points = new Vector3[] { this.handPositionsCache.Dequeue(), this.handPositionsCache.Dequeue(), this.handPositionsCache.Dequeue() };

                Vector3 shoulderPosition;
                float armLength;

                if (this.Estimate3DCircle(points, out shoulderPosition, out armLength))
                {
                    if (this.HasEstimate == false)
                    {
                        this.HasEstimate = true;

                        this.ShoulderPosition = shoulderPosition;
                        this.ArmLength = armLength;
                    }
                    else
                    {
                        this.ShoulderPosition = Vector3.Lerp(this.ShoulderPosition, shoulderPosition, 0.5f);
                        this.ArmLength = Mathf.Lerp(this.ArmLength, armLength, 0.5f);
                    }
                }
            }
        }

        public float GetArmAngle(Vector3 handPosition, Vector3 shoulderPosition)
        {
            var delta = handPosition - shoulderPosition;

            var deltaOnYZplane = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(delta.x, 0, delta.z), Vector3.up)) * delta;

            var angleA = Mathf.Atan2(deltaOnYZplane.y, deltaOnYZplane.z) * Mathf.Rad2Deg;
            var angleB = Mathf.Atan2(-1, 0) * Mathf.Rad2Deg;

            return Mathf.Abs(Mathf.DeltaAngle(angleA, angleB));
       }

        private bool Estimate3DCircle(Vector3[] points, out Vector3 center, out float radius)
        {
            if (points.Length < 3)
            {
                center = Vector3.zero;
                radius = 0f;

                return false;
            }

            Vector3 v1 = points[1] - points[0];
            Vector3 v2 = points[2] - points[0];

            float v1v1, v2v2, v1v2;

            v1v1 = Vector3.Dot(v1, v1);
            v2v2 = Vector3.Dot(v2, v2);
            v1v2 = Vector3.Dot(v1, v2);

            float baseVar = 0.5f / (v1v1 * v2v2 - v1v2 * v1v2);
            float k1 = baseVar * v2v2 * (v1v1 - v1v2);
            float k2 = baseVar * v1v1 * (v2v2 - v1v2);

            center = points[0] + v1 * k1 + v2 * k2;

            var delta = (center - points[0]);

            radius = Mathf.Sqrt(Vector3.Dot(delta, delta));

            return float.IsNaN(radius) == false;
        }
    }
}
