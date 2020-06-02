using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class ControllerLabels : MonoBehaviour
{
    public GameObject text1GameObject;
    public GameObject text2GameObject;
    public GameObject text3GameObject;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.text1GameObject.transform.LookAt(Camera.main.transform.position);
        this.text2GameObject.transform.LookAt(Camera.main.transform.position);
        this.text3GameObject.transform.LookAt(Camera.main.transform.position);
    }
}
