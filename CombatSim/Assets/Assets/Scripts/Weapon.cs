/*
Implementation of weapons. 
 Idea: Give agents a generic weapon handle. Depending on the type of agent, they instantiate a melee or ranged weapon with appropriate stats.
        Agents just need to call weapon->attack to attack a target. The specifics of how the attacks work are implemented here.
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Weapon 
{
    public GameObject wOwner;
	public float wDamage;
    public float wRange;
    public float wRangeSqr;
    public float wAttackDelay;
    protected float wTimeLastAttacked;

    //abstract public Weapon(float dmg, float rng, float delay, GameObject projectile);
    abstract public void Attack(GameObject target);
    public bool IsInRange(GameObject target)
    {
        if ((target.transform.position - wOwner.transform.position).sqrMagnitude <= wRangeSqr)
            return true;
        return false;
    }

    public bool InLineOfSight(GameObject target)
    {
        RaycastHit hit;
        //Debug.DrawLine(wOwner.transform.position, target.transform.position);
        if (Physics.Linecast(wOwner.transform.position, target.transform.position, out hit))
        {
            if (hit.collider.transform.parent == null) return false;

            if (target.tag == "Unit")
            {
                //Raycast should be hitting a body, so check the parent of the hit
                if (hit.collider.transform.parent.gameObject == target) return true;
            }
            else if (target.tag == "Structure")
            {
                //Should hit a component of the structure, so check the parent of the hit
                if (hit.collider.transform.parent.gameObject == target) return true;
            }
            //Debug.Log("Hit the wrong thing - " + wOwner.name + " to " + target.name + "; hit" + hit.collider.transform.parent.gameObject.name);
            return false;
        }
        //Debug.Log("Didn't hit anything - " + wOwner.name + " to " + target.name);
        return false;
    }
}

public class MeleeWeapon : Weapon
{
    public MeleeWeapon(GameObject owner, float dmg, float rng, float delay)
    {
        wOwner = owner;
        wDamage = dmg;
        wRange = rng;
        wRangeSqr = rng * rng;
        wAttackDelay = delay + Random.Range(-0.1f * delay, 0.1f * delay);
        wTimeLastAttacked = 0;
    }

    override public void Attack(GameObject target)
    {
        Agent targetAgent = target.GetComponent<Agent>();
        //If my target does not have an Agent component, it is an invalid target.
        if (targetAgent == null)
        {
            return;
        }

        //If I am out of range, don't do any damage.
        if ((wOwner.transform.position - target.transform.position).sqrMagnitude > wRangeSqr)
        {
            return;
        }

        //If we haven't waited long enough, don't attack
        if (Time.time - wTimeLastAttacked < wAttackDelay)
        {
            return;
        }

        //Otherwise, note the time
        wTimeLastAttacked = Time.time;

        targetAgent.TakeDamage(wDamage, wOwner);
    }
}

public class RangedWeapon: Weapon
{
    GameObject wProjectile;
    float wProjectileVelocity;
    const float G = 9.81f;

    float yOffset, xOffset;
    public RangedWeapon(GameObject owner, GameObject projectile, float projVel, float dmg, float rng, float delay)
    {
        wOwner = owner;
        wProjectile = projectile;
        wProjectileVelocity = projVel;
        wDamage = dmg;
        wRange = rng;
        wRangeSqr = rng * rng;
        wAttackDelay = delay + Random.Range(-0.1f * delay, 0.1f * delay);
        wTimeLastAttacked = 0;

        yOffset = 1.5f;
        xOffset = 1.5f;

        if (Global.global == null) return;

        //Check if this type of weapon has already been calculated in the globals
        if (!Global.global.rangedWeapons.ContainsKey(new Vector3((int)wProjectileVelocity, (int)wRange, 0)))
        {
            calculateAngles();
        }
        else
        {
            return;
        }
    }

    void calculateAngles()
    {
        Debug.Log("Called calculator");
        for(int range = 0; range <= wRange; range++)
        {
            for(int height = -50; height <= 50; height++)
            {
                //Calculate the angle necessary to fire and hit the target x units away and at y units altitude, given a constant firing velocity v
                //theta = arctan(v^2 +- sqrt(v^4 - g(gx^2 + 2yv^2))/gx)
                //Taken from http://en.wikipedia.org/wiki/Trajectory_of_a_projectile
                float x = range;
                float y = height;
                
                Vector3 key = new Vector3((int)wProjectileVelocity, (int)x, (int)y);
                
                //If this has already been solved, skip it
                if(Global.global.rangedWeapons.ContainsKey(key))
                {
                    break;
                }

                float inside = Mathf.Pow(wProjectileVelocity, 4) - G * (G * Mathf.Pow(x, 2) + 2 * y * Mathf.Pow(wProjectileVelocity, 2));

                //If the problem cannot be solved, add a <-1, -1> to the map to signify that.
                if(inside < 0)
                {
                    Global.global.rangedWeapons.Add(key, new Vector2(-1, -1));
                }
                    //Otherwise, solve the equation and put it in the dictionary
                else
                {
                    float numeratorPlus = Mathf.Pow(wProjectileVelocity, 2) + Mathf.Sqrt(inside);
                    float numeratorMinus = Mathf.Pow(wProjectileVelocity, 2) - Mathf.Sqrt(inside);
                    float denom = G * x;
                    float thetaPlus = Mathf.Atan(numeratorPlus / denom);
                    float thetaMinus = Mathf.Atan(numeratorMinus / denom);

                    float min = Mathf.Min(thetaPlus, thetaMinus);
                    float max = Mathf.Max(thetaPlus, thetaMinus);
                    Vector2 solution = new Vector2(max, min);
                    
                    Global.global.rangedWeapons.Add(key, solution);
                }
            }
        }
    }

    override public void Attack(GameObject target)
    {
        //If we haven't waited long enough, don't attack
        if (Time.time - wTimeLastAttacked < wAttackDelay)
        {
            return;
        }

        Agent targetAgent = target.GetComponent<Agent>();
        //If my target does not have an Agent component, it is an invalid target.
        if (targetAgent == null)
        {
            return;
        }

        //If I am out of range, don't do any damage.
        if ((wOwner.transform.position - target.transform.position).sqrMagnitude > wRangeSqr)
        {
            return;
        }

        ////Calculate the angle necessary to fire and hit the target x units away and at y units altitude, given a constant firing velocity v
        ////theta = arctan(v^2 +- sqrt(v^4 - g(gx^2 + 2yv^2))/gx)
        ////Taken from http://en.wikipedia.org/wiki/Trajectory_of_a_projectile
        Vector2 targetXZ = new Vector2(target.transform.position.x, target.transform.position.z);
        Vector2 sourceXZ = new Vector2(wOwner.transform.position.x + xOffset, wOwner.transform.position.z);
        float x = (targetXZ - sourceXZ).magnitude;
        float y = target.transform.position.y - (wOwner.transform.position.y + yOffset);
        //float inside = Mathf.Pow(wProjectileVelocity, 4) - G*(G*Mathf.Pow(x, 2) + 2*y*Mathf.Pow(wProjectileVelocity, 2));
        Vector3 key = new Vector3((int)wProjectileVelocity, (int)x, (int)y);
        Vector2 result;
        //Vector2 result = new Vector2(Mathf.PI / 4.0f, Mathf.PI / 4.0f);
        Global.global.rangedWeapons.TryGetValue(key, out result);

        //If inside is negative, we don't have enough power to hit the target
        if (result.x == -1 && result.y == -1)
        {
            return;
        }


        wTimeLastAttacked = Time.time;

        //float numeratorPlus = Mathf.Pow(wProjectileVelocity, 2) + Mathf.Sqrt(inside);
        //float numeratorMinus = Mathf.Pow(wProjectileVelocity, 2) - Mathf.Sqrt(inside);
        //float denom = G * x;
        //float thetaPlus = Mathf.Atan(numeratorPlus / denom);
        //float thetaMinus = Mathf.Atan(numeratorMinus / denom);

        float theta;
        bool los = InLineOfSight(target);
        if (los)
        {
            //theta = Mathf.Min(thetaPlus, thetaMinus);
            theta = result.y;
        }
        else
        {
            //theta = Mathf.Max(thetaPlus, thetaMinus);
            theta = result.x;
        }
        //Debug.Log("Plus: " + thetaPlus + " | Minus: " + thetaMinus + "Distance: " + x);

        GameObject shot = GameObject.Instantiate(wProjectile, wOwner.transform.position + new Vector3(xOffset, yOffset, 0), Quaternion.identity) as GameObject;
        shot.transform.forward = new Vector3(targetXZ.x - sourceXZ.x, 0, targetXZ.y - sourceXZ.y);
        shot.transform.Rotate(new Vector3(-theta * Mathf.Rad2Deg, 0, 0), Space.Self);
        shot.GetComponent<Rigidbody>().velocity = shot.transform.forward.normalized * wProjectileVelocity;
        shot.GetComponent<Projectile>().owner = wOwner;
    }
}
