using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SquirrelScript : MonoBehaviour
{

    public LayerMask nutLayer;
    public LayerMask canLayer;
    public LayerMask playerLayer;

    private int foodCount;
    private bool inTree;
    private bool inCan;
    private bool seenPlayer;
    private Queue<GameObject> nutsInMemory = new Queue<GameObject>();
    private Queue<GameObject> cansInMemory = new Queue<GameObject>();
    private GameObject homeTree;
    private GameObject currentTree;
    private GameObject player;

    private Queue<Action> currentPlan;
    private Action currentAction = null;

    private NavMeshAgent agent;
    private Vector3 moveToDest = new Vector3(0,0,0);

    public GameObject updateTextAction;
    public GameObject updateTextState;
    public GameObject updateTextPlan;

    public void Awake(){
        //Here, we set the squirrel's percieved state of the world.
        foodCount = 0;
        inTree = false;
        inCan = false;
        currentPlan = new Queue<Action>();
        agent = gameObject.GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0.1f;
        agent.speed = 1f;
        updateTextAction = gameObject.transform.GetChild(1).gameObject;
        updateTextState = gameObject.transform.GetChild(0).gameObject;
        updateTextPlan = gameObject.transform.GetChild(2).gameObject;
        player = GameObject.Find("Player");

    }

    //Next, a bunch of small self-explanatory functions.

    public void getStuck(){
        inCan = true;
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }

    public void getUnStuck(){
        inCan = false;
        gameObject.GetComponent<MeshRenderer>().enabled = true;
    }

    public void setCurrentTree(GameObject tree){
        this.currentTree = tree;
    }

    public GameObject getCurrentTree(GameObject tree){
        return currentTree;
    }

    public void setHomeTree(GameObject newHomeTree){
        this.homeTree = newHomeTree;
    }

    public bool hasPlan(){
        return currentPlan.Count == 0;
    }

    public void setPlan(Queue<Action> aPlan){
        currentPlan = aPlan;
    }

    public void setTreeState(bool treeState){
        inTree = treeState;
    }

    public int getTreeState(){
        if(inTree){
            return 1;
        }else{
            return 0;
        }
    }

    public void incrementFoodCount(){
        foodCount++;
    }

    public void clearFoodCount(){
        foodCount = 0;
    }

    public int getFoodCount(){
        return foodCount;
    }

    public bool hasSeenPlayer(){
        return seenPlayer;
    }

    private GameObject nearestTree(){
        GameObject closestValid = null;

        foreach(GameObject tree in GOAPPlanner.instance.allTrees){
            if(!tree.GetComponent<TreeScript>().containsSquirrel()){
                closestValid = tree;
                break;
            }
        }
        float currentClosestDistance = (gameObject.transform.position - closestValid.transform.position).magnitude;

        foreach(GameObject tree in GOAPPlanner.instance.allTrees){
            if( ((gameObject.transform.position - tree.transform.position).magnitude < currentClosestDistance) && (!tree.GetComponent<TreeScript>().containsSquirrel()) ){ //If the tree is closer hand has no squirrel.
                closestValid = tree;
                currentClosestDistance = (gameObject.transform.position - tree.transform.position).magnitude;
            }
        }

        return closestValid;
    }

    private bool performed = false;

    public void PerformAction(){
        if(currentPlan.Count == 0 || currentAction == null){
            currentAction = null;
            performed = false;
            currentPlan.Clear();
            return; //Case where a performAction should have been interrupted by a player.
        }


        currentAction.isRunning = false;
        currentAction.performAction(gameObject);
        currentPlan.Dequeue(); //We just finished performing an action, so we can dequeue it here.
        currentAction = null;
        performed = false;
    }


    private Collider[] nutResults = new Collider[5];
    private Collider[] canResults = new Collider[2];
    void FixedUpdate(){
        Physics.OverlapSphereNonAlloc(gameObject.transform.position, 5f, nutResults, nutLayer); //Get all nuts in a radius.
        //Add them to the nutsInMemory queue.
        foreach(Collider nut in nutResults){
            if(nut == null || nutsInMemory.Contains(nut.gameObject)){ //If the nut is already in our memory, continue.
                continue;
            }

            if(nutsInMemory.Count == 5){ //In this case the squirrel's "memory" is full, so we must enqueue the nut that was seen first.
                nutsInMemory.Dequeue();
                nutsInMemory.Enqueue(nut.gameObject);
            }else{
                nutsInMemory.Enqueue(nut.gameObject); //Otherwise we just add to memory. Same logic applies below to cans.
            }
        }

        Physics.OverlapSphereNonAlloc(gameObject.transform.position, 10f, canResults, canLayer);
        //Add them to the cansInMemory queue.
        foreach(Collider can in canResults){
            if(can == null || cansInMemory.Contains(can.gameObject)){ //If the nut is already in our memory, continue.
                continue;
            }

            if(cansInMemory.Count == 2){
                cansInMemory.Dequeue();
                cansInMemory.Enqueue(can.gameObject);
            }else{
                cansInMemory.Enqueue(can.gameObject);
            }
        }

        //Here, the squirrelScript will have a new attribute, "seenPlayer", initially false. We'll do a check here to see if a squirrel sees the Player. If so, do something according to "seenPlayer"'s value.
        //If seenPlayer is false, set "seenPlayer" to true and throw out the plan completely, which makes the GOAP planner create a "flee" plan.
        //If seenPlayer is already true, do nothing.
        if(Physics.CheckSphere(transform.position, 3f, playerLayer) && ! player.GetComponent<GhostScript>().isGhost){
            if(!seenPlayer){
                seenPlayer = true;
                currentAction = null;
                currentPlan.Clear();
                performed = false;
            }
        }else{
            seenPlayer = false;
        }


        nutResults = new Collider[5];
        canResults = new Collider[2];
    }

    //In the LateUpdate function, Squirrels follow their plans.        
    void LateUpdate(){

        if(!inCan){

            if(currentPlan.Count == 0){ //In this case, we have no plan, so we ask for one from the GOAP Planner.
                    Queue<Action> newPlan = GOAPPlanner.instance.givePlan(gameObject);
                if(newPlan != null){
                    currentPlan = newPlan;
                }else{
                    return; //For some reason, getting the plan failed, so return.
                }


            }else{//In this case, we have remaining actions in our action queue.

                if(currentAction != null && currentAction.isRunning){ //In this case, an action is currently in the process of being executed.

                    //If we're travelling to a tree and it contains a squirrel, or to a garbage can and it contains a squirrel, end the path and get a new plan.
                    if((currentAction is ClimbTreeAction && currentAction.target.GetComponent<TreeScript>().containsSquirrel()) || currentAction is InvestigateGarbageAction && currentAction.target.GetComponent<GarbageCanScript>().containsSquirrel){
                        agent.isStopped = true;
                        currentAction = null;
                        currentPlan.Clear();
                        return;
                    }else{
                        agent.isStopped = false;
                    }

                    if((agent.remainingDistance <= agent.stoppingDistance)){ //Agent has arrived.
                        if(!performed){ //We want to perfom the action only once but we may want it to take a little while to perform, so we invoke it and then change 'performed' so we don't call it several times.
                            Invoke("PerformAction", currentAction.duration);
                            performed = true;
                        }
                    }
                    return;
                }else{
                    currentAction = currentPlan.Peek();

                    if(currentAction is TakeNutAction && nutsInMemory.Count != 0){
                            // Debug.Log("Takenut action");
                            updateTextAction.GetComponent<TextMesh>().text = "TakeNut Action";
                            currentAction.setTargetObject(nutsInMemory.Dequeue()); 
                        }else if (currentAction is ClimbTreeAction){ 
                            // Debug.Log("Climb action");
                            updateTextAction.GetComponent<TextMesh>().text = "ClimbTree Action";
                            if(seenPlayer){
                                //In this case, we set the target tree to the nearest unoccupied tree.
                                currentAction.setTargetObject(nearestTree());
                            }else{
                                //The only other tree we'd ever climb is the home tree.
                                currentAction.setTargetObject(homeTree);
                            }
                        }else if (currentAction is DescendTreeAction){  
                            // Debug.Log("Descend action");
                            updateTextAction.GetComponent<TextMesh>().text = "DescendTree Action";
                            currentAction.setTargetObject(currentTree); //CurrentTree may not be set properly ***
                        }else if (currentAction is StoreFoodAction){
                            // Debug.Log("Storefood action");
                            updateTextAction.GetComponent<TextMesh>().text = "StoreFood Action";
                            currentAction.setTargetObject(homeTree);
                        }else if (currentAction is InvestigateGarbageAction && cansInMemory.Count != 0){
                            // Debug.Log("GarbageAction");
                            updateTextAction.GetComponent<TextMesh>().text = "GetGarbage Action";
                            currentAction.setTargetObject(cansInMemory.Dequeue());
                        }else if (currentAction is IdleAction || currentAction is IdleSitAction){
                            //Continue
                            updateTextAction.GetComponent<TextMesh>().text = "Idle Action";
                        }else{
                            // Debug.Log("getting rid of plan due to not being able to set a target.");
                            updateTextAction.GetComponent<TextMesh>().text = "NoAction";
                            currentPlan.Clear(); //Plan failed, throw away and restart.
                            return;
                        }

                        if(seenPlayer){
                            agent.speed = 2.2f;
                        }else{
                            agent.speed = 1f;
                        }

                    if(currentAction.checkCondition(gameObject)){

                        if(currentAction.getTargetLocation() == null){ //If the target is still null after all that, throw away and restart.
                            // Debug.Log("Getting rid of plan due to null target");
                            currentPlan.Clear();
                            return;
                        }

                        currentAction.isRunning = true;
                        agent.SetDestination(currentAction.getTargetLocation());
                        agent.isStopped = false;
                    
                    }else{
                        //Condition failed, so throw away the plan.
                        currentPlan.Clear();
                    }
                }
            }
        }
}

void Update(){
    updateTextAction.transform.LookAt(GameObject.Find("Main Camera").transform);
    updateTextState.transform.LookAt(GameObject.Find("Main Camera").transform);
    updateTextPlan.transform.LookAt(GameObject.Find("Main Camera").transform);

    updateTextState.GetComponent<TextMesh>().text = "Food: " + foodCount + "  inTree: " + inTree;
}

}
