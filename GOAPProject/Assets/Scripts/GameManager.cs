using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    //Coroutine which MAY change garbage state every 10s as long as that can does not contain a squirrel.
     IEnumerator changeCanState(){
         while(true){
             for(int i = 0; i < cans.Count; i++){ 
                int decider = (int) Random.Range(0f, 1.99f);

                if(decider == 0 && (! cans[i].GetComponent<GarbageCanScript>().hasSquirrel()) ){
                    cans[i].GetComponent<GarbageCanScript>().changeState();
                }
            }
            yield return new WaitForSeconds(10f);
        }
     }

    //Coroutine that will spawn nuts every 2s for any tree that does not have 5 nuts on the ground.
     IEnumerator spawnNuts(){
         while(true){
            
             for(int i = 0 ; i < trees.Count; i++){  
                bool hasntSpawnedYet = trees[i].GetComponent<TreeScript>().spawnNut();

                while(hasntSpawnedYet){

                    Vector3 spawnPoint = (trees[i].transform.position + new Vector3(Mathf.Pow(-1, (int) Random.Range(0, 1.9f)) * Random.Range(0, 0.5f), trees[i].GetComponent<TreeScript>().groundPosOffset,Mathf.Pow(-1, (int) Random.Range(0, 1.9f)) * Random.Range(0, 0.5f)));

                    if(trees[i].GetComponent<TreeScript>().spawnNut() && ( !Physics.CheckSphere(spawnPoint, 0.15f, nutMask)) ){
                     trees[i].GetComponent<TreeScript>().nutSpawned(Instantiate(nutPrefab, spawnPoint, Quaternion.identity));
                     hasntSpawnedYet = false;
                    }
                }
             }

            yield return new WaitForSeconds(2f);
         }
     }


    // Start is called before the first frame update
    public GameObject treePrefab;
    public GameObject nutPrefab;
    public GameObject garbageCanPrefab;
    public GameObject garbagePrefab;
    public GameObject GOAPPlanner;
    public LayerMask obstMask;
    public LayerMask nutMask;
    public NavMeshSurface surface;

    public float treeRadius;
    public float canRadius;

    private List<GameObject> trees = new List<GameObject>();
    private List<GameObject> cans = new List<GameObject>();

    void Start()
    {
        //First, we randomly spawn 10 trees (at y = 1.14) and 5 garbage cans (at y = 1.1) in our play area.
        //The play area has xMax = 6.25, zMax = 17, xMin = -6.25, zMin = -17.

        int numTrees = 10;
        int numCans = 5;
        int curObjects = 0;

        while(curObjects < numTrees){

            Vector3 spawnPoint = new Vector3(Random.Range(-6.25f, 6.25f), 1.241f, Random.Range(-15, 17));

            if(! Physics.CheckSphere(spawnPoint, treeRadius + 2f, obstMask)){
                var instantiatedTree = Instantiate(treePrefab, spawnPoint, Quaternion.Euler(-90,0,0)) as GameObject; 
                trees.Add(instantiatedTree);
                curObjects++;
            }
        }

        curObjects = 0;

        while(curObjects < numCans){
            Vector3 spawnPoint = new Vector3(Random.Range(-6.25f, 6.25f), 1.118f, Random.Range(-15, 17));

            if(! Physics.CheckSphere(spawnPoint, canRadius + 2f, obstMask)){
                var instantiatedCan = Instantiate(garbageCanPrefab, spawnPoint, Quaternion.Euler(-90,0,0)) as GameObject;
                var instantiatedJunk = Instantiate(garbagePrefab, spawnPoint + new Vector3(0, 0.10f, 0),Quaternion.Euler(0,0,0)) as GameObject;
                instantiatedCan.GetComponent<GarbageCanScript>().setGarbageObject(instantiatedJunk);

                //For each can object, randomly decide to change its starting state.
                int decider = (int) Random.Range(0.0f, 1.9f);
                if(decider == 0){
                    instantiatedCan.GetComponent<GarbageCanScript>().changeState();
                }
                cans.Add(instantiatedCan);
                curObjects++;
            }
        }

        //Now we build the navMesh so we don't get squirrels running through trees/cans.
        surface.BuildNavMesh();

        //Now, we call the CoRoutines which will spawn nuts around TreeObjects and change state of the GarbageCans.
        StartCoroutine(changeCanState());
        StartCoroutine(spawnNuts());
        GOAPPlanner.GetComponent<GOAPPlanner>().startPlans(trees);
    }



}
