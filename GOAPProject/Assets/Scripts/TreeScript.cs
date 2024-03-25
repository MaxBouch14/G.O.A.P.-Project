using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeScript : MonoBehaviour
{

    private List<GameObject> nuts = new List<GameObject>();
    public float radius = 3f;
    public float groundPosOffset = -0.225f; //the distance to the "ground level" where the tree comes out, which is where nuts will spawn.
    private GameObject occupantSquirrel = null;


    public bool spawnNut(){
        return nuts.Count < 5;
    }

    public void nutSpawned(GameObject nut){ //A nut was just spawned, so we add it to this tree's nuts List.
        nut.GetComponent<NutScript>().setMyTree(gameObject);
        nuts.Add(nut);
    }

    public void removeNut(GameObject nut){
        if(nut == null){
            nuts.RemoveAll(null);
        }
        if(nuts.Contains(nut)){
            nuts.Remove(nut);
        }
    }

    public bool containsSquirrel(){
        return (occupantSquirrel != null);
    }

    public void setOccupantSquirrel(GameObject inputSquirrel){
        occupantSquirrel = inputSquirrel;
    }
}
