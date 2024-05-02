using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Vector3 targetDirection;

    // Speed of rotation
    public float rotationSpeed = 90.0f; // Degrees per second

    // Tolerance for stopping rotation (in degrees)
    public float stopThreshold = 1.0f;

    // Flag to check if rotation is in progress
    private bool isRotating = false;


    // Start is called before the first frame update
    void Start()
    {
        targetDirection = Vector3.zero;

        // other_script = GameObject.Find("SarchObject");

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R presed");
            // targetDirection = GameObject.Find("Watermelon").transform.position;
            targetDirection = SearchObject.findObjectInGame("Rosie");
            Debug.Log(targetDirection);
            isRotating = true;
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("H presed");
            // targetDirection = GameObject.Find("Watermelon").transform.position;
            targetDirection = SearchObject.findObjectInGame("Heli Sphere");
            Debug.Log(targetDirection);
            isRotating = true;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {

            Debug.Log("E presed");
            // targetDirection = GameObject.Find("Watermelon").transform.position;
            targetDirection = SearchObject.findObjectInGame("error");
            Debug.Log(targetDirection);
            isRotating = true;
        }

    }

    void FixedUpdate()
    {
    
        Vector3 forwardDirection = transform.forward;
        Debug.DrawLine(transform.position, transform.position + forwardDirection * 3, Color.red);

        if (isRotating)
        {
            // Calculate the direction to rotate towards
            Vector3 targetDir = targetDirection - transform.position;
            targetDir.y = 0; // Optional: Keep rotation in the horizontal plane only

            // Calculate the rotation needed to look at the target direction
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);

            // Rotate towards the target rotation
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Check if the rotation has reached the target direction
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            if (angleDifference <= stopThreshold)
            {
                // Stop rotation
                isRotating = false;
            }

        }

    }


}
