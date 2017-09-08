using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionController : MonoBehaviour {

    public float MoveSpeed = 2f;
    public ParticleSystem bubbles;

    //buoyancy
    public float waterlevel = 4f;
    public float floatHeight = 2f;
    public float bounceDamp = 0.05f;
    public Vector3 buoyancyCentreOffset;
    private float forceFactor;
    private Vector3 actionPoint;
    private Vector3 upLift;

    //mouse controller
    enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
    RotationAxes axes = RotationAxes.MouseXAndY;
    float sensitivityX = 15F;
    float sensitivityY = 15F;
    float minimumX = -360F;
    float maximumX = 360F;
    float minimumY = -60F;
    float maximumY = 60F;
    float rotationY = 0F;

    private bool isUnderwater;
    private Color normalColor;
    private Color underwaterColor;

    private bool pause = false;

    void Start () {
        // Make the rigid body not change rotation  
        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;

        normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        underwaterColor = new Color(0.22f, 0.5f, 0.8f, 0.5f);

        bubbles.Stop();
    }
	
	void Update () {
        if (Input.GetKey(KeyCode.UpArrow))
            transform.Translate(Vector3.forward * Time.deltaTime * MoveSpeed);
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Translate(Vector3.left * Time.deltaTime * MoveSpeed);
        if (Input.GetKey(KeyCode.DownArrow))
            transform.Translate(Vector3.back * Time.deltaTime * MoveSpeed);
        if (Input.GetKey(KeyCode.RightArrow))
            transform.Translate(Vector3.right * Time.deltaTime * MoveSpeed);
        if (Input.GetKey(KeyCode.W))
            transform.Translate(Vector3.up * Time.deltaTime * MoveSpeed);
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.down * Time.deltaTime * MoveSpeed);
            bounceDamp = 1;
        }

        if (Input.GetKey(KeyCode.P)) pause = true;
        if (Input.GetKey(KeyCode.A)) pause = false;
        if (pause == false)
        {
            viewController();
        }

        actionPoint = transform.position + transform.TransformDirection(buoyancyCentreOffset);
        forceFactor = 1f - ((actionPoint.y - waterlevel) / floatHeight);
        
        if (forceFactor > 0)
        {
            upLift = -Physics.gravity * (forceFactor - GetComponent<Rigidbody>().velocity.y * bounceDamp);
            GetComponent<Rigidbody>().AddForceAtPosition(upLift, actionPoint);
        }

        if ((transform.position.y<(waterlevel-1.8f)) != isUnderwater)
        {
            isUnderwater = transform.position.y < (waterlevel-1.8f);
            if (!isUnderwater)
            {
                RenderSettings.fog = false;
                transform.Find("FirstPersonViewCamera").GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
                bubbles.Stop();
            }
            if (isUnderwater)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = underwaterColor;
                RenderSettings.fogDensity = 0.1f;
                transform.Find("FirstPersonViewCamera").GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                bubbles.Play();

            }
        }
    }

    void viewController()
    {
        //view controller
        if (axes == RotationAxes.MouseXAndY)
        {
            float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }
        else if (axes == RotationAxes.MouseX)
        {
            transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
        }
        else
        {
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
        }
    }

}
