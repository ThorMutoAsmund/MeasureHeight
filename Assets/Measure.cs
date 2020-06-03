using Gonio;
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
    public GameObject blueSphere;
    public Material greenMaterial;
    public Material redMaterial;

    private RomTestMath romTestMath;
    private RomTestMathExtended romTestMathExtended;
    private bool measuringSpherical;
    private GameObject miniBallContainer;
    private string startText;

    // Start is called before the first frame update
    void Start()
    {
        this.redSphere.transform.localScale = Vector3.zero;
        this.greenSphere.transform.localScale = Vector3.zero;
        this.startText = this.text.text;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.measuringSpherical)
        {
            if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                var handPosition = this.rightHandAnchor.position;
                this.romTestMath.AddData(handPosition);
                this.romTestMathExtended.AddData(handPosition);
            }
            else
            {
                this.measuringSpherical = false;

                // Extended
                this.romTestMathExtended.FinishMeasurement();

                // Old method
                if (this.romTestMath.HasEstimate)
                {
                    this.text.text += $"Calculated height (3d matrix) = {this.romTestMath.ShoulderPosition.y.ToString("0.00")} m\n";
                    this.blueSphere.transform.position = this.romTestMath.ShoulderPosition;
                    this.blueSphere.transform.localScale = 0.05f * Vector3.one;
                }
                else
                {
                    this.blueSphere.transform.localScale = Vector3.zero;
                }
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
            this.text.text = startText;
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            this.text.text += "Reading sample points...\n";
            this.measuringSpherical = true;
            this.romTestMath = new RomTestMath();
            this.romTestMathExtended = new RomTestMathExtended();
            this.romTestMathExtended.MeasurementAdded += point =>
            {
                var miniBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                miniBall.transform.localScale = 0.01f * Vector3.one;
                miniBall.transform.position = point;
                miniBall.transform.parent = this.miniBallContainer.transform;
                miniBall.name = $"Measurement";
            };
            this.romTestMathExtended.Message += message =>
            {
                this.text.text += message + "\n";
            };
            this.romTestMathExtended.CenterCalculated += point =>
            {
                if (point.HasValue)
                {
                    this.greenSphere.transform.position = point.Value;
                    this.greenSphere.transform.localScale = 0.05f * Vector3.one;
                }
                else
                {
                    this.greenSphere.transform.localScale = Vector3.zero;
                }
            };
            this.romTestMathExtended.CenterCreated += (point, isOutlier) =>
            {
                var miniCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                miniCube.transform.localScale = 0.01f * Vector3.one;
                miniCube.transform.position = point;
                miniCube.transform.parent = this.miniBallContainer.transform;
                miniCube.GetComponent<Renderer>().material = isOutlier ? redMaterial : greenMaterial;
            };

            if (this.miniBallContainer)
            {
                GameObject.Destroy(this.miniBallContainer);
            }
            this.miniBallContainer = new GameObject();
        }
    }
}
