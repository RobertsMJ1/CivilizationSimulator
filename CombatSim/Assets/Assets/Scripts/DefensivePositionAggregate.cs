/*
 * Will contain referenecs to all defensive positions for a city. 
 * Will pass them to the city.
 * Will not handle assignment of units or maintaining availability for positions.
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DefensivePositionAggregate : MonoBehaviour {
    //List<DefensivePosition> positions;
    List<DefensivePosition> openPositions = new List<DefensivePosition>();
    List<DefensivePosition> usedPositions = new List<DefensivePosition>();

    void Start()
    {
        Init();
    }

    void Init()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            DefensiveLocalAggregate local = transform.GetChild(i).gameObject.GetComponent<DefensiveLocalAggregate>();
            if (local == null) continue;
            GameObject temp = local.positions;

            for (int j = 0; j < temp.transform.childCount; j++)
            {
                //positions.Add(temp.transform.GetChild(j).gameObject.GetComponent<DefensivePosition>());
                openPositions.Add((temp.transform.GetChild(j).gameObject.GetComponent<DefensivePosition>()));
            }
        }
    }

    void Update()
    {
        if(openPositions.Count == 0 && usedPositions.Count == 0)
        {
            Init();
        }
    }

    //Checks if there are defensive positions available
    public bool HasOpenPositions()
    {
        if(openPositions.Count == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //Gets an open position and removes it from the open list
    public DefensivePosition GetOpenPosition()
    {
        if (openPositions.Count == 0)
        {
            return null;
        }
        else
        {
            //If there is an open position, remove it from open positions and move it to used positions.
            DefensivePosition p = openPositions[0];
            openPositions.Remove(p);
            usedPositions.Add(p);
            p.available = false;
            return p;
        }
    }

    public void OpenUsedPosition(DefensivePosition pos)
    {
        if (usedPositions.Contains(pos))
        {
            usedPositions.Remove(pos);
            openPositions.Add(pos);
            pos.available = true;
        }
    }
}