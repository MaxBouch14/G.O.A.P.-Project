using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NutScript : MonoBehaviour
{

    private GameObject myTree;

    void OnDestroy() 
    {
        if(myTree == null){ //Don't know why for sure, but I was getting a "MissingReferenceException" on line 16 ONLY when STOPPING playmode. This if-block fixes it. (My guess is OnDestroy() is still called when stopping the game?)
            return;
        }

        myTree.GetComponent<TreeScript>().removeNut(gameObject);
    }

    public void setMyTree(GameObject inputTree){
        myTree = inputTree;
    }

}
