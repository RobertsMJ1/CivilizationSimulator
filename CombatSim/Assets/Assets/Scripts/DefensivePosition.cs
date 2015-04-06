using UnityEngine;
using System.Collections;

public class DefensivePosition : MonoBehaviour {
    public bool available = true;
    public bool ranged = true;

    public Vector3 getPosition() { return transform.position; }
    public GameObject getObject() { return gameObject; }

    void Update()
    {
        if(available)
        {
            GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
    }
}
