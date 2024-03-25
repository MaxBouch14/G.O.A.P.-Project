using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarbageCanScript : MonoBehaviour
{
    private GameObject garbage;
    public bool containsSquirrel;
    private bool empty;
    private GameObject stuckSquirrel;

    void Start(){
        empty = true;
        containsSquirrel = false;
        stuckSquirrel = null;

    }

    public void setGarbageObject(GameObject inputGarbage){ //The "junk" representing whether or not the can is full.
        garbage = inputGarbage;
    }

    public bool isEmpty(){
        return empty;
    }

    public void changeState(){ //Maybe have attached junk prefab, and just enable/disable it according to the state.
        if(empty){
            empty = false;
            garbage.SetActive(false);
        }else{
            empty = true;
            garbage.SetActive(true);
        }
    }

    public bool hasSquirrel(){
        return (stuckSquirrel != null);
    }

    public void squirrelStuck(GameObject aSquirrel){ //Put a squirrel in the can.
        containsSquirrel = true;
        stuckSquirrel = aSquirrel;
        aSquirrel.GetComponent<SquirrelScript>().getStuck();
        Invoke("squirrelOut", 2f);
    }

    public void squirrelOut(){  //Get the squirrel out.
        containsSquirrel = false;
        stuckSquirrel.GetComponent<SquirrelScript>().getUnStuck();
        stuckSquirrel = null;
    }

}
