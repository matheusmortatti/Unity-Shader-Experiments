using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatEngineController : MonoBehaviour
{
    public float engineForceVertical;
    public float engineForceHorizontal;

    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (this.transform.position.y > 0)
            return;

        int vertical = 0;
        int horizontal = 0;

        vertical += Input.GetKey(KeyCode.W) ? 1 : 0;
        vertical += Input.GetKey(KeyCode.S) ? -1 : 0;

        horizontal += Input.GetKey(KeyCode.D) ? -1 : 0;
        horizontal += Input.GetKey(KeyCode.A) ? 1 : 0;

        Vector3 verticalForce = vertical * engineForceVertical * this.transform.forward;
        Vector3 horizontalForce = horizontal * engineForceHorizontal * this.transform.right;

        rb.AddForceAtPosition(verticalForce, this.transform.position);
        rb.AddForceAtPosition(horizontalForce, this.transform.position);

        Debug.DrawRay(this.transform.position, verticalForce, Color.blue);
        Debug.DrawRay(this.transform.position, horizontalForce, Color.yellow);
    }
}
