using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Flying : MonoBehaviour
{
    public float PAtm;
    public Vector3 WindVelocity = Vector3.zero;
    public float density;
    public float g;
    public Rigidbody rb;
    public Vector3 initialForce;
    // Start is called before the first frame update
    void Start()
    {
        rb.AddForce(initialForce, ForceMode.Impulse);
    }

    // Update is called once per frame
    void Update()
    {
        rb.AddForce(Mul(gameObject.transform.forward, 0.48 * 0.37 * PressureFinder(WindVelocity, transform.position.y)));
    }
    double PressureFinder(Vector3 velocity, float height)
    {
        return PAtm + 0.5 * density * Mathf.Pow(velocity.magnitude, 2) + density * g * height;
    }

    Vector3 Mul(Vector3 v, double m)
    {
        return new Vector3(v.x * ((float)m), v.y * ((float)m), v.z * ((float)m));
    }
}
