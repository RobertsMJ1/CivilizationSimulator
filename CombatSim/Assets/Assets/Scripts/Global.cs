using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Global : MonoBehaviour {
    public static Global global;
    //Precompute solutions to shots at different ranges and heights.
    //Key is defined as <Power, Range, Height> - all ints.
    //The vector 2 will hold <max, min>
    public Dictionary<Vector3, Vector2> rangedWeapons = new Dictionary<Vector3,Vector2>();
	
    // Use this for initialization
	void Start () {
        if(global == null)
        {
            DontDestroyOnLoad(this);
            global = this;
        }
        else if (this != global)
        {
            Destroy(this);
        }
	}
}
