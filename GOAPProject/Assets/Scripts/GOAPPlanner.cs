using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class GOAPPlanner : MonoBehaviour
{
    public static GOAPPlanner instance = null;
    void Awake(){
        if(instance == null){
            instance = this;
        }else if (instance != this){
            Destroy(gameObject);
        }
    }



    public GameObject squirrelPrefab;
    private List<GameObject> squirrels = new List<GameObject>();
    private List<Action> allActions = new List<Action>();
    public List<GameObject> allTrees;

    //This function will create Squirrels and give them initial plans.
    public void startPlans(List<GameObject> trees)
    {
        //5 Squirrels, one tree each.
        for(int i = 0; i < 5; i++){
            Vector3 spawnPoint = trees[i].transform.position;
            spawnPoint.y = 1.075f;
            spawnPoint.z = spawnPoint.z + 0.02f;
            var instantiatedSquirrel = Instantiate(squirrelPrefab, spawnPoint + new Vector3(0,0, 0.2f), Quaternion.Euler(0,0,0)) as GameObject; 
            squirrels.Add(instantiatedSquirrel);
            instantiatedSquirrel.GetComponent<SquirrelScript>().setHomeTree(trees[i]);
            instantiatedSquirrel.GetComponent<SquirrelScript>().setCurrentTree(trees[i]);
        }

        allActions.Add(new ClimbTreeAction(1.5f));
        allActions.Add(new StoreFoodAction(2f));
        allActions.Add(new DescendTreeAction(1.5f));
        allActions.Add(new TakeNutAction(0.3f));
        allActions.Add(new InvestigateGarbageAction(0.3f));

        allTrees = trees;
    }

    

    //This function will get get a goal and a queue of necessary actions to get there, given a squirrel (which contains its worldState)
    public Queue<Action> givePlan(GameObject squirrel){

        if(squirrel.GetComponent<SquirrelScript>().hasSeenPlayer()){ //Special case of plan. If the squirrel has seen a player, the only action is "ClimbTree". SquirrelScript will handle finding the nearest available tree.
            Queue<Action> specialPlan = new Queue<Action>();
            specialPlan.Enqueue(new ClimbTreeAction(1.5f));
            squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Fleeing from player!";
            return specialPlan;
        }


        Queue<Action> newPlan = new Queue<Action>();
        Dictionary<string,int> goalState = new Dictionary<string, int>();
        List<Action> actions = new List<Action>();

        if(squirrel.GetComponent<SquirrelScript>().getFoodCount() == 3){ //Case where we are full on food. In this case, the goal will always be to go store the food.
            squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Plan to store food!";
            //We want our goal state to be "foodCount == 0"
            goalState.Add("foodCount", 0);
            foreach(Action a in allActions){
                actions.Add(a.getClone());
            }

        }else if(squirrel.GetComponent<SquirrelScript>().getFoodCount() != 0){ //Case where we have some food. In this case, we randomly choose to go store the food, or go collect more food.
            int decider = (int) Random.Range(0f, 1.99f);
            if(decider == 0){                           //Squirrel will go store the food.
                //We want our goal state to be "foodCount == 0"
                squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Plan to store food!";
                goalState.Add("foodCount", 0);
                foreach(Action a in allActions){
                    actions.Add(a.getClone());
                }

            }else{                                      //Squirrel will go collect more food.
                //We want our goal state to be "foodCount == 3"
                squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Plan to collect food!";
                goalState.Add("foodCount", 3);
                //Climb actions may cause strange behavior in this plan, since squirrel will climb up and down a tree in bewteen nuts. This happens due to a lack of action costs, but since this is the only case of this,
                //I just instead don't include the climb action as being possible in the plan.
                foreach(Action a in allActions){ 
                    if(! (a is ClimbTreeAction)){
                        actions.Add(a.getClone());
                    }
                }
            }

        }else{ //Case where we have 0 food. Randomly choose to either go idle, or go collect food.
            int decider = (int) Random.Range(0f, 1.99f);

            if(decider == 0){                           //Squirrel will be idle. Special case of no goal state, so we just check that the squirrel is not in a tree, we descend if it is, and then execute the idle action.
            decider = (int) Random.Range(0f, 1.99f);
            if(decider == 0){ //Squirrel will sit there a few seconds.
                squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Plan to go idle!";
                if(squirrel.GetComponent<SquirrelScript>().getTreeState() == 1){
                    newPlan.Enqueue(new DescendTreeAction(0.5f));
                }
                newPlan.Enqueue(new IdleSitAction(Random.Range(3f, 5f) ));
            }else{ //Squirrel will wander to a random destination.
                squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Plan to wander!";
                if(squirrel.GetComponent<SquirrelScript>().getTreeState() == 1){
                    newPlan.Enqueue(new DescendTreeAction(0.5f));
                }
                newPlan.Enqueue(new IdleAction(squirrel, 0.1f));
            }
            return newPlan;

            }else{                                      //Squirrel wants to go collect food.
                //We want our goal state to be "foodCount == 3"
                squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Plan to collect food!";
                goalState.Add("foodCount", 3);
                foreach(Action a in allActions){ 
                    if(! (a is ClimbTreeAction)){
                        actions.Add(a.getClone());
                    }
                }
            }
        }

        //Now, we set up our attributes to build our graph of actions.
        List<Node> endPoints = new List<Node>();
        Node startNode = new Node(null, null, getStateFromSquirrel(squirrel));
        bool pathFound = buildGraph(startNode, endPoints, actions, goalState); //We build the graph, which will update the amount of valid "endpoints".

        //Now that we have endpoints, we check if the path was found. If not, return null. If so, shuffle the endpoints to randomly choose one path.
        Node end = null;
        if(!pathFound){
            squirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "No plan!";
            return null;
        }else{
            System.Random rng = new System.Random();
            var shuffledEndPts = endPoints.OrderBy(a => rng.Next()).ToList();
            end = shuffledEndPts[0];
            }

        //Now we just create our plan backwards from the "end" node, and we return the plan!.
        List<Action> orderedPlan = new List<Action>();
        Node node = end;
        while(node != null){
            if(node.action != null){
                orderedPlan.Insert(0, node.action);
            }
            node = node.parent;
        }
        foreach(Action a in orderedPlan){
            newPlan.Enqueue(a);
        }
        return newPlan;

    }

    //Self explanatory
    private static Dictionary<string, int> getStateFromSquirrel(GameObject squirrel){
        Dictionary<string, int> result = new Dictionary<string, int>();
        result.Add("foodCount", squirrel.GetComponent<SquirrelScript>().getFoodCount());
        result.Add("inTree", squirrel.GetComponent<SquirrelScript>().getTreeState());
        return result;
    }

    //Recursively builds a graph given a set of useable actions (which can be custom, not neccessarily all of them).
    private bool buildGraph(Node parNode, List<Node> endPoints, List<Action> actions, Dictionary<string, int> goalState){
        bool foundPath = false;

        foreach(Action action in actions){
            if(action.checkPlanPrecondition(parNode.curState)){ //Action is compatible.
                Dictionary<string,int> newState = action.applyPostcondition(parNode.curState); //Apply the action to the state.
                Node newNode = new Node(parNode, action, newState);

                if(isGoal(goalState, newState)){ //We've reached an endpoint.
                    endPoints.Add(newNode);
                    foundPath = true;
                }else{
                    //We remove the action we just used (to prevent loops) unless it is takeNut (since we may want to do that several times)
                    List<Action> newActions = actionsWithout(actions, action);
                    bool otherwiseFound = buildGraph(newNode, endPoints, newActions, goalState);
                    if(otherwiseFound){
                        foundPath = true;
                    }
                }

            }
        }


        return foundPath;
    }

    //Figures out if a state is the goal state.
    public bool isGoal(Dictionary<string, int> goalState, Dictionary<string, int> inputState){
        bool hasInTree = goalState.ContainsKey("inTree");
        bool hasFoodCount = goalState.ContainsKey("foodCount");

        if(hasInTree && hasFoodCount){
            return ((goalState["inTree"] == inputState["inTree"]) && (goalState["foodCount"] == inputState["foodCount"]));
        }else if (hasInTree){
            return (goalState["inTree"] == inputState["inTree"]);
        }else if (hasFoodCount){
            return (goalState["foodCount"] == inputState["foodCount"]);
        } else{
            Debug.Log("Hey! This shouldn't be possible!");
            return false; //Should never happen.
        }

    }

    //Removes "withoutMe" if it is not a takeNutAction.
    private static List<Action> actionsWithout(List<Action> actions, Action withoutMe){
        List<Action> newList = new List<Action>();
        foreach(Action action in actions){
            
            if(! action.Equals(withoutMe) || action is TakeNutAction ){ //Want to keep TakeNut actions, since we may want to repeat them.
                newList.Add(action.getClone()); //GetClone may not be necessary but it doesn't hurt to be safe.
            }
        }
        return newList;
    }

    public class Node{ //For use when building a graph of actions.
        public Node parent;
        public Action action;
        public Dictionary<string, int> curState;

        public Node(Node par, Action aAction, Dictionary<string,int> aState){
            parent = par;
            action = aAction;
            curState = aState;
        }
    }

}
