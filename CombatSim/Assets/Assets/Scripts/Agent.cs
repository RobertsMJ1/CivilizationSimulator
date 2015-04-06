using UnityEngine;
using System.Collections;

public class Agent : MonoBehaviour {
    //public Faction owner;
    //Tells us if this agent has ranged or melee attacks. Melee agents will leave aProjectile null.
    public enum CombatType { Ranged, Melee };
    public CombatType aType = CombatType.Melee;
    public GameObject aProjectile;

    public enum TaskType { Defense };
    public TaskType currentTask;

    public enum TaskStatus { Complete, Working };

    //State of the agent; if passive, hold weapon in a passive stance. If aggressive, hold weapon in an aggressive stance.
    public enum CombatState { Passive, Aggressive };
    public CombatState currentCombatState;
    public CombatState prevCombatState;

    public Vector3 rCombatStance;
    public Vector3 sCombatStance;
    public Vector3 tCombatStance;

    public Vector3 rIdleStance;
    public Vector3 sIdleStance;
    public Vector3 tIdleStance;

    public GameObject aWeaponObject;
    public GameObject aBodyObject;
    public GameObject aShieldObject;

    float aTransitionBegin;
    float aTransitionLength;

    public Weapon aWeapon;
    public float aHealth;

    public GameObject aDestination;
    public GameObject aWorldDestination;
    public DefensivePosition aDefensivePosition;
    NavMeshAgent aNavigation;

    public int aFaction;

    void Start()
    {
        switch(aType)
        {
            case CombatType.Melee:
                aWeapon = new MeleeWeapon(gameObject, 1.0f, 4.0f, 1.0f);
                break;
            case CombatType.Ranged:
                aWeapon = new RangedWeapon(gameObject, aProjectile, 30.0f, 10.0f, 50.0f, 5.0f);
                break;
            default:
                aWeapon = new MeleeWeapon(gameObject, 1.0f, 4.0f, 1.0f);
                break;
        }

        aTransitionBegin = 0.0f;
        aTransitionLength = 1.0f;

        aDestination = aWorldDestination;
        aNavigation = GetComponent<NavMeshAgent>();
        if (aDestination != null) aNavigation.SetDestination(aDestination.transform.position);
    }

    public void Init(/*Faction f, */TaskType task, DefensivePosition p)
    {
        //owner = f;
        currentTask = task;
        aDefensivePosition = p;
        if (aDefensivePosition != null)
        {
            aWorldDestination = aDefensivePosition.getObject();
        }
    }

    void Update()
    {        
        //Pathfinding - if we are in attack range of our destination, stop and begin attacking
        if (IsPathFinished())
        {
            aNavigation.Stop();
            //Debug.Log(gameObject.name + ": Path Finished");
            // aNavigation.destination = transform.position;
            if (aDestination.tag == "Unit")
            {
                //Debug.Log(gameObject.name + ": Attacking");
                aWeapon.Attack(aDestination);
            }
        }
        else if (aDestination == null)
        {
            //Do something
            aDestination = aWorldDestination;
        }
        else
        {
            aNavigation.destination = aDestination.transform.position;
        }
        
        

        //State manipulation; when we are in awareness range of an enemy, transition into aggressive stance
        if (prevCombatState != currentCombatState)
        {
            aTransitionBegin = Time.time;
        }

        switch (currentCombatState)
        {
            case CombatState.Aggressive:
                LerpTransform(aWeaponObject.transform, rCombatStance, sCombatStance, tCombatStance);
                break;
            case CombatState.Passive:
                LerpTransform(aWeaponObject.transform, rIdleStance, sIdleStance, tIdleStance);
                break;
        }

        prevCombatState = currentCombatState;
    }

    public void TakeDamage(float d, GameObject attacker)
    {
        if (aDestination != attacker) aDestination = attacker;
        if(Random.Range(0, 2) > 0) aHealth -= d;

        if (aHealth <= 0.0f) Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }

    bool IsPathFinished()
    {
        if (aDestination == null) return false;

        if (aDestination.tag != "Unit" && aNavigation.remainingDistance > 1) return false;

        if (aWeapon.IsInRange(aDestination))
        {
            return true;
        }

        return false;
    }

    //Do a linear interpolation of from to Transform(r, s, t)
    void LerpTransform(Transform from, Vector3 r, Vector3 s, Vector3 t)
    {
        if (from.localScale == s && from.localPosition == t && from.localEulerAngles == r) return;
        if (Time.time - aTransitionBegin > aTransitionLength) return;
        if (currentCombatState == CombatState.Passive)
        {
            from.localPosition = Vector3.Lerp(tCombatStance, tIdleStance, (Time.time - aTransitionBegin) / aTransitionLength);
            from.localScale = Vector3.Lerp(sCombatStance, sIdleStance, (Time.time - aTransitionBegin) / aTransitionLength);
            from.localEulerAngles = Vector3.Lerp(rCombatStance, rIdleStance, (Time.time - aTransitionBegin) / aTransitionLength);
        }
        else if (currentCombatState == CombatState.Aggressive)
        {
            from.localPosition = Vector3.Lerp(tIdleStance, tCombatStance, (Time.time - aTransitionBegin) / aTransitionLength);
            from.localScale = Vector3.Lerp(sIdleStance, sCombatStance, (Time.time - aTransitionBegin) / aTransitionLength);
            from.localEulerAngles = Vector3.Lerp(rIdleStance, rCombatStance, (Time.time - aTransitionBegin) / aTransitionLength);
        }
    }

    bool InLineOfSight(GameObject target)
    {
        RaycastHit hit;
        //Debug.DrawLine(transform.position, target.transform.position);
        if (Physics.Linecast(transform.position, target.transform.position, out hit))
        {
            if (hit.collider.transform.parent == null) return false;

            if(target.tag == "Unit")
            {
                //Raycast should be hitting a body, so check the parent of the hit
                if (hit.collider.transform.parent.gameObject == target) return true;
            }
            else if (target.tag == "Structure")
            {
                //Should hit a component of the structure, so check the parent of the hit
                if (hit.collider.transform.parent.gameObject == target) return true;
            }
            //Debug.Log("Hit the wrong thing - " + gameObject.name + " to " + target.name + "; hit" + hit.collider.transform.parent.gameObject.name);
            return false;
        }
        //Debug.Log("Didn't hit anything - " + gameObject.name + " to " + target.name);
        return false;
    }

    //When we enter a trigger of another object in the world, we want to update our destination appropriately.
    //If we are currently moving to a non-unit destination, check if we just became aware of an enemy.
    //If we did, update our destination to that enemy.
    void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Unit") return;

        //if (!InLineOfSight(other.gameObject)) return;

        Agent a = other.GetComponent<Agent>();
        if (a == null) return;

        if (a.aFaction != aFaction)
        {
            //If we don't have a target that is an enemy, we can try and set a new target.
            //However, we want to do a raycast to the target. If we can't see it, don't make it our destination
            if (aDestination == null || aDestination.tag != "Unit") 
            {
                aDestination = other.gameObject;
            }
            else if ((other.transform.position - transform.position).sqrMagnitude < (aDestination.transform.position - transform.position).sqrMagnitude)
            {
                aDestination = other.gameObject;
            }
            currentCombatState = CombatState.Aggressive;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag != "Unit") return;

        //if (!InLineOfSight(other.gameObject)) return;

        Agent a = other.GetComponent<Agent>();
        if (a == null) return;

        if (a.aFaction != aFaction)
        {
            //If we don't have a target that is an enemy, we can try and set a new target.
            //However, we want to do a raycast to the target. If we can't see it, don't make it our destination
            if (aDestination == null || aDestination.tag != "Unit")
            {
                aDestination = other.gameObject;
            }
            else if ((other.transform.position - transform.position).sqrMagnitude < (aDestination.transform.position - transform.position).sqrMagnitude)
            {
                aDestination = other.gameObject;
            }
            currentCombatState = CombatState.Aggressive;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag != "Unit") return;

        Agent a = other.GetComponent<Agent>();
        if (a == null) return;

        currentCombatState = CombatState.Passive;
    }

}
