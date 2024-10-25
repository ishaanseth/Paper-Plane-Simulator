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
    public float radius;
    float timer = 0;
    float end = 0.2f;
    bool timerStart = false;

    // Start is called before the first frame update
    void Start()
    {
        rb.AddForce(initialForce, ForceMode.Impulse);
    }

    // Update is called once per frame
    void Update()
    {
        rb.AddForce(Mul(gameObject.transform.up, 0.48 * 1.6 * Mathf.Abs((float)(PressureFinder(WindVelocity, transform.position.y - radius) - PressureFinder(rb.velocity, transform.position.y)))));
        if (timerStart)
        {
            timer += Time.deltaTime;
        }

        if (timer > end)
        {
            Debug.Log("End");
            Destroy(gameObject.GetComponent<Flying>());
        }
    }
    double PressureFinder(Vector3 velocity, float height)
    {
        return PAtm + 0.5 * density * Mathf.Pow(velocity.magnitude, 2) + density * g * height;
    }

    Vector3 Mul(Vector3 v, double m)
    {
        return new Vector3(v.x * ((float)m), v.y * ((float)m), v.z * ((float)m));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            timerStart = true;
            Debug.Log("COllision");
            
        }
    }
}
