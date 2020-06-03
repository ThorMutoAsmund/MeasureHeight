using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gonio
{
    public class RomTestMathExtended
    {
        private List<Vector3> positions = new List<Vector3>();
        public Vector3 ShoulderPosition { get; private set; }
        public float ArmLength { get; private set; }
        public bool HasEstimate { get; private set; }

        public event Action<Vector3> MeasurementAdded;
        public event Action<string> Message;
        public event Action<Vector3?> CenterCalculated;
        public event Action<Vector3, bool> CenterCreated;

        public int minimumNumberOfSamplePoints;
        public float maxDistanceToHead;
        public int minimumNumberOfCentersRequired;
        public int iterations;
        public float maxAcceptedOutlierRatio;

        public RomTestMathExtended(int minimumNumberOfSamplePoints = 10, float maxDistanceToHead = 0.6f,
            int minimumNumberOfCentersRequired = 4, int iterations = 1000, float maxAcceptedOutlierRatio = 0.2f)
        {
            this.minimumNumberOfSamplePoints = minimumNumberOfSamplePoints;
            this.maxDistanceToHead = maxDistanceToHead;
            this.minimumNumberOfCentersRequired = minimumNumberOfCentersRequired;
            this.iterations = iterations;
            this.maxAcceptedOutlierRatio = maxAcceptedOutlierRatio;
        }

        public void AddData(Vector3 handPosition)
        {
            if (this.positions.Count == 0 || Vector3.Distance(handPosition, this.positions[this.positions.Count - 1]) > 0.1f)
            {
                this.positions.Add(handPosition);
                this.MeasurementAdded?.Invoke(handPosition);
            }
        }

        public void FinishMeasurement()
        {
            if (this.positions.Count < minimumNumberOfSamplePoints)
            {
                this.Message?.Invoke($"Too few points. Please try again");
                return;
            }

            var center = FindCenterUsingMatrices();

            if (center.HasValue)
            {
                this.Message?.Invoke($"Calculated height (4d matrix) = {center.Value.y.ToString("0.00")} m");
                this.CenterCalculated?.Invoke(center.Value);
            }
            else
            {
                this.CenterCalculated?.Invoke(default);
            }
        }

        private Vector3? FindCenterUsingMatrices()
        {
            var centers = new List<Vector3>();
            var centerInfo = new Dictionary<Vector3, string>();
            Vector3[] v = new Vector3[4];

            var names = new List<string>();
            int imb = 0;
            foreach (var p in positions)
            {
                names.Add($"MiniBall {++imb}");
            }

            while (iterations-- > 0)
            {
                // Get 4 random points
                var s = "";
                var used = new HashSet<int>();
                for (int i = 0; i < 4; ++i)
                {
                    int r;
                    do
                    {
                        r = UnityEngine.Random.Range(0, this.positions.Count);
                    }
                    while (used.Contains(r));
                    used.Add(r);
                    v[i] = this.positions[r];
                    s += $"{names[r]} ";
                }

                float T, D, E, F, G;
                var c = SphereCenterFrom4Points(v[0], v[1], v[2], v[3], out T, out D, out E, out F, out G);
                centers.Add(c);
                centerInfo[c] = $"{c.x}, {c.y}, {c.z} from {s}. T = {T}. D = {D}. E = {E}. F = {F}. G = {G}";
            }

            // Approximate center
            var approximateCenter = Camera.main.transform.position;

            var okCenters = new List<Vector3>();
            foreach (var c in centers)
            {
                var isOutlier = Vector3.Distance(c, approximateCenter) > maxDistanceToHead;
                if (!isOutlier)
                {
                    okCenters.Add(c);
                }
                this.CenterCreated(c, isOutlier);
            }

            var outliers = centers.Count - okCenters.Count;
            var outlierRatio = (float)outliers / centers.Count;
            this.Message?.Invoke($"{centers.Count} points sampled. {outliers} outliers. Outlier ratio {outlierRatio}");

            if (okCenters.Count < minimumNumberOfCentersRequired)
            {
                this.Message?.Invoke($"Measurement not precise enough. Please try again");
                return default;
            }

            if (outlierRatio > maxAcceptedOutlierRatio)
            {
                this.Message?.Invoke($"Measurement not precise enough. Please try again");
                return default;
            }

            var center = Vector3.zero;
            foreach (var c in okCenters)
            {
                center.x += c.x;
                center.y += c.y;
                center.z += c.z;
            }
            center.x /= okCenters.Count;
            center.y /= okCenters.Count;
            center.z /= okCenters.Count;

            return center;
        }


        private Vector3 SphereCenterFrom4Points(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
    out float T, out float D, out float E, out float F, out float G)
        {
            var t1 = -(v1.x * v1.x + v1.y * v1.y + v1.z * v1.z);
            var t2 = -(v2.x * v2.x + v2.y * v2.y + v2.z * v2.z);
            var t3 = -(v3.x * v3.x + v3.y * v3.y + v3.z * v3.z);
            var t4 = -(v4.x * v4.x + v4.y * v4.y + v4.z * v4.z);
            T = Det4(v1.x, v1.y, v1.z, 1f, v2.x, v2.y, v2.z, 1f, v3.x, v3.y, v3.z, 1f, v4.x, v4.y, v4.z, 1f);
            D = Det4(t1, v1.y, v1.z, 1f, t2, v2.y, v2.z, 1f, t3, v3.y, v3.z, 1f, t4, v4.y, v4.z, 1f) / T;
            E = Det4(v1.x, t1, v1.z, 1f, v2.x, t2, v2.z, 1f, v3.x, t3, v3.z, 1f, v4.x, t4, v4.z, 1f) / T;
            F = Det4(v1.x, v1.y, t1, 1f, v2.x, v2.y, t2, 1f, v3.x, v3.y, t3, 1f, v4.x, v4.y, t4, 1f) / T;
            G = Det4(v1.x, v1.y, v1.z, t1, v2.x, v2.y, v2.z, t2, v3.x, v3.y, v3.z, t3, v4.x, v4.y, v4.z, t4) / T;

            return new Vector3(-D / 2f, -E / 2f, -F / 2f);
        }

        private float Det4(float a, float b, float c, float d
            , float e, float f, float g, float h
            , float i, float j, float k, float l
            , float m, float n, float o, float p)

        {
            // a(fkp − flo − gjp + gln + hjo − hkn) − b(ekp − elo − gip + glm + hio − hkm) + c(ejp − eln − fip + flm + hin − hjm) − d(ejo − ekn − fio + fkm + gin − gjm)
            return
                a * (f * k * p - f * l * o - g * j * p + g * l * n + h * j * o - h * k * n) - b * (e * k * p - e * l * o - g * i * p + g * l * m + h * i * o - h * k * m)
            + c * (e * j * p - e * l * n - f * i * p + f * l * m + h * i * n - h * j * m) - d * (e * j * o - e * k * n - f * i * o + f * k * m + g * i * n - g * j * m);

        }

        //private Vector3 FindCenterUsingApproximation()
        //{
        //    var d = 5f;
        //    var c = Vector3.zero;

        //    for (int i = 0; i < 20; ++i)
        //    {
        //        var v1 = new Vector3(c.x - d, c.y - d, c.z - d);
        //        var v2 = new Vector3(c.x + d, c.y + d, c.z + d);
        //        var sq = CreateSquare(v1, v2);

        //        float bestsd = 1000000f;
        //        foreach (var s in sq)
        //        {
        //            var dist = new float[this.positions.Count];
        //            var j = 0;
        //            foreach (var p in this.positions)
        //            {
        //                dist[j++] = Vector3.Distance(s, p);
        //            }

        //            var avg = dist.Average();
        //            var sd = dist.Sum(ds => (ds - avg) * (ds - avg));
        //            if (sd * avg < bestsd)
        //            {
        //                bestsd = sd;
        //                c = s;
        //            }
        //        }
        //        d /= 2F;
        //    }
        //    return c;
        //}

        //private Vector3[] CreateSquare(Vector3 v1, Vector3 v2)
        //{
        //    var result = new Vector3[27];

        //    int i = 0;
        //    for (int x = 0; x < 3; ++x)
        //    {
        //        var fx = (x == 0 ? v1.x : (x == 2 ? v2.x : (v1.x + v2.x) / 2f));
        //        for (int y = 0; y < 3; ++y)
        //        {
        //            var fy = (y == 0 ? v1.y : (y == 2 ? v2.y : (v1.y + v2.y) / 2f));
        //            for (int z = 0; z < 3; ++z)
        //            {
        //                var fz = (z == 0 ? v1.z : (z == 2 ? v2.z : (v1.z + v2.z) / 2f));
        //                result[i++] = new Vector3(fx, fy, fz);
        //            }
        //        }
        //    }

        //    return result;
        //}

    }
}
