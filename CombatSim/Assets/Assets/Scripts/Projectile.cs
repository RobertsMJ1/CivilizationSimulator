using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour {
    float lifetime;
    float timeHit;
    bool hit;
    public GameObject owner;

	// Use this for initialization
	void Start () {
        lifetime = 5;
        timeHit = 0;
	}
	
	// Update is called once per frame
	void Update () {
        if(hit)
        {
            if (Time.time - timeHit > lifetime) Destroy(gameObject);
        }
	}

    void FixedUpdate()
    {
        if (!hit)
        {
            transform.forward = GetComponent<Rigidbody>().velocity;
        }
        else
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Projectile") return;
        hit = true;
        timeHit = Time.time;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Character")
        {
            if (other.transform.parent.gameObject == owner) return;

            Agent targetAgent = other.GetComponent<Agent>();

            if (targetAgent == null)
            {
                targetAgent = other.transform.parent.gameObject.GetComponent<Agent>();
                if(targetAgent == null)
                {
                    return;
                }
            }
                

            targetAgent.TakeDamage(owner.GetComponent<Agent>().aWeapon.wDamage, owner);

            Destroy(gameObject);
        }
    }
}
