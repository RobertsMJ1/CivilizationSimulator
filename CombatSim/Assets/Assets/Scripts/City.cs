/*
    Cities will handle the following:
 * Gathering resources
 * Creating units
 *      Units created in a city will be under the control of that city
 *          unless the faction overrides the units for a faction-wide event
 * Organizing defensive units and patrols
 *      Will assign units to defensive positions. 
 *      Units will patrol to adjacent friendly cities
 *      If all positions and patrols are filled
 *          Units assigned to another city that requires units
 *          If nobody needs more units
 *              Stop making them
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class City : MonoBehaviour {
    //public Faction owner;
    //public List<GameObject> structures;
    public List<GameObject> unassignedUnits;
    public List<GameObject> assignedUnits;

    DefensivePositionAggregate defensivePositionAggregate = null;
    public GameObject aggregate;

    public GameObject rangedUnitPrefab;
    public GameObject meleeUnitPrefab;

    public GameObject spawnPoint;

    float spawnTimer = 0.0f;
    float spawnDelay = 5.0f;

	// Use this for initialization
	void Start () 
    {
        defensivePositionAggregate = aggregate.GetComponent<DefensivePositionAggregate>();
	}
	
	// Update is called once per frame
	void Update () 
    {
        if(defensivePositionAggregate == null)
        {
            defensivePositionAggregate = aggregate.GetComponent<DefensivePositionAggregate>();
        }

	    if(Time.time - spawnTimer > spawnDelay)
        {
            spawnUnit();
            spawnTimer = Time.time;
        }
	}

    void spawnUnit()
    {
        if (defensivePositionAggregate == null) return;
        //Check if there is an open position to assign a unit to
        if(defensivePositionAggregate.HasOpenPositions())
        {
            DefensivePosition pos = defensivePositionAggregate.GetOpenPosition();
            GameObject g;
            if(pos.ranged)
            {
                g = Instantiate(rangedUnitPrefab, spawnPoint.transform.position, Quaternion.identity) as GameObject;
            }
            else
            {
                g = Instantiate(meleeUnitPrefab, spawnPoint.transform.position, Quaternion.identity) as GameObject;
            }
            
            Agent a = g.GetComponent<Agent>();
            a.Init(/*owner, */Agent.TaskType.Defense, pos);
        }
    }
}