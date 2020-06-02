using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Measure : MonoBehaviour
{
    public Text text;
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    public GameObject redSphere;
    public GameObject greenSphere;

    private bool measuringSpherical;
    private GameObject miniBallContainer;

    // Start is called before the first frame update
    void Start()
    {
        this.redSphere.transform.localScale = Vector3.zero;
        this.greenSphere.transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.measuringSpherical)
        {
            if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                this.positions.Add(this.rightHandAnchor.position);
                var miniBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                miniBall.transform.localScale = 0.01f * Vector3.one;
                miniBall.transform.position = this.rightHandAnchor.position;
                miniBall.transform.parent = this.miniBallContainer.transform;
            }
            else
            {
                this.measuringSpherical = false;
                FinishMeasurement();
            }
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            this.text.text += $"Height = {this.rightHandAnchor.position.y.ToString("0.00")} m\n";
            this.redSphere.transform.position = this.rightHandAnchor.position;
            this.redSphere.transform.localScale = 0.05f * Vector3.one;
        }
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            this.text.text = "Målinger\n";
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            this.text.text += "Foretager sfærisk måling\n";
            this.measuringSpherical = true;
            this.positions = new List<Vector3>();
            if (this.miniBallContainer)
            {
                GameObject.Destroy(this.miniBallContainer);
            }
            this.miniBallContainer = new GameObject();
        }
    }

    private List<Vector3> positions;

    private void FinishMeasurement()
    {
        if (this.positions.Count == 0)
        {
            this.text.text += "Ingen punkter\n";
            return;
        }
        if (this.positions.Count < 10)
        {
            this.text.text += "For få punkter\n";
            return;
        }

        var center = FindCenterUsingMatrices();
        
        this.text.text += $"{this.positions.Count} punkter\n"; 
        this.text.text += $"Beregnet højde = {center.y.ToString("0.00")} m\n";
        this.greenSphere.transform.position = center;
        this.greenSphere.transform.localScale = 0.05f * Vector3.one;    
    }

    private Vector3 FindCenterUsingMatrices()
    {
        var centers = new List<Vector3>();
        Vector3[] v = new Vector3[4];

        while (this.positions.Count >= 4)
        {
            // Get 4 random points
            for (int i = 0; i < 4; ++i)
            {
                var r = Random.Range(0, this.positions.Count);
                v[i] = this.positions[r];
                this.positions.RemoveAt(r);
            }

            centers.Add(SphereCenterFrom4Points(v[0], v[1], v[2], v[3]));
        }

        var center = Vector3.zero;
        foreach (var c in centers)
        {
            center.x += c.x;
            center.y += c.y;
            center.z += c.z;
        }
        center.x /= centers.Count();
        center.y /= centers.Count();
        center.z /= centers.Count();

        return center;
    }

    private Vector3 SphereCenterFrom4Points(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var t1 = -(v1.x * v1.x + v1.y * v1.y + v1.z * v1.z);
        var t2 = -(v2.x * v2.x + v2.y * v2.y + v2.z * v2.z);
        var t3 = -(v3.x * v3.x + v3.y * v3.y + v3.z * v3.z);
        var t4 = -(v4.x * v4.x + v4.y * v4.y + v4.z * v4.z);
        var T = Det4(v1.x, v1.y, v1.z, 1f, v2.x, v2.y, v2.z, 1f, v3.x, v3.y, v3.z, 1f, v4.x, v4.y, v4.z, 1f);
        var D = Det4(t1, v1.y, v1.z, 1f, t2, v2.y, v2.z, 1f, t3, v3.y, v3.z, 1f, t4, v4.y, v4.z, 1f) / T;
        var E = Det4(v1.x, t1, v1.z, 1f, v2.x, t2, v2.z, 1f, v3.x, t3, v3.z, 1f, v4.x, t4, v4.z, 1f) / T;
        var F = Det4(v1.x, v1.y, t1, 1f, v2.x, v2.y, t2, 1f, v3.x, v3.y, t3, 1f, v4.x, v4.y, t4, 1f) / T;
        var G = Det4(v1.x, v1.y, v1.z, t1, v2.x, v2.y, v2.z, t2, v3.x, v3.y, v3.z, t3, v4.x, v4.y, v4.z, t4) / T;
        
        return new Vector3(-D/2f, -E/2f, -F/2f);
    }

    private float Det4(float a, float b, float c, float d
        , float e, float f, float g, float h
        , float i, float j, float k, float l
        , float m, float n, float o, float p)

    {
        // a(fkp − flo − gjp + gln + hjo − hkn) − b(ekp − elo − gip + glm + hio − hkm) + c(ejp − eln − fip + flm + hin − hjm) − d(ejo − ekn − fio + fkm + gin − gjm)
        return
            a*(f*k*p - f*l*o - g*j*p + g*l*n + h*j*o - h *k*n) - b *(e*k*p - e *l*o - g *i*p + g*l*m + h*i*o - h *k*m) 
        + c*(e*j*p - e *l*n - f *i*p + f*l*m + h*i*n - h *j*m) - d *(e*j*o - e *k*n - f *i*o + f*k*m + g*i*n - g *j*m);

    }

    private Vector3 FindCenterUsingApproximation()
    {
        var d = 5f;
        var c = Vector3.zero;

        for (int i = 0; i < 20; ++i)
        {
            var v1 = new Vector3(c.x - d, c.y - d, c.z - d);
            var v2 = new Vector3(c.x + d, c.y + d, c.z + d);
            var sq = CreateSquare(v1, v2);

            float bestsd = 1000000f;
            foreach (var s in sq)
            {
                var dist = new float[this.positions.Count];
                var j = 0;
                foreach (var p in this.positions)
                {
                    dist[j++] = Vector3.Distance(s, p);
                }

                var avg = dist.Average();
                var sd = dist.Sum(ds => (ds - avg) * (ds - avg));
                if (sd * avg < bestsd)
                {
                    bestsd = sd;
                    c = s;
                }
            }
            d /= 2F;
        }
        return c;
    }

    private Vector3[] CreateSquare(Vector3 v1, Vector3 v2)
    {
        var result = new Vector3[27];

        int i = 0;
        for (int x = 0; x<3; ++x)
        {
            var fx = (x == 0 ? v1.x : (x == 2 ? v2.x : (v1.x+v2.x)/2f));
            for (int y = 0; y < 3; ++y)
            {
                var fy = (y == 0 ? v1.y : (y == 2 ? v2.y : (v1.y + v2.y) / 2f));
                for (int z= 0; z < 3; ++z)
                {
                    var fz = (z == 0 ? v1.z : (z == 2 ? v2.z : (v1.z + v2.z) / 2f));
                    result[i++] = new Vector3(fx, fy, fz);
                }
            }
        }

        return result;
    }
}
