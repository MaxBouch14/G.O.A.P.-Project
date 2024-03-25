using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Action 
{
    //A given action needs its own preconditions.
    protected Dictionary<string, List<int>> preconditions = new Dictionary<string, List<int>>();
    public bool isRunning;
    public GameObject target; //The location we're trying to pathfind to, if applicable.
    public float duration = 0;


    public Action(float aDuration){
        isRunning = false;
        duration = aDuration;
        target = null;
    }

    public virtual Vector3 getTargetLocation(){
        if(target == null){
            return new Vector3(-999, -999, -999); //Represents an error.
        }else{
            return target.transform.position;
        }
    }

    public abstract Action getClone();
    public abstract bool checkPlanPrecondition(Dictionary<string,int> inputState);
    public abstract Dictionary<string,int> applyPostcondition(Dictionary<string, int> inputState);

    public void setTargetObject(GameObject aTarget){
        target = aTarget;
    }

    //Checks that a precondition is met before we perform an action. If false, we throw away the plan and try again with a new one.
    public abstract bool checkCondition(GameObject aSquirrel);

    //Performs the action in question. Returns an ActionState, either Processing or Finished. These will be called repeatedly in FixedUpdate.
    //However, the FixedUpdate Loop they are called in will first check to see if the Player is nearby. If so, the action is thrown away along with the plan.
    public abstract bool performAction(GameObject aSquirrel);

}

public class IdleSitAction : Action {

    public IdleSitAction(float aDuration) : base(aDuration){
        preconditions.Add("inTree", new List<int>());
        preconditions["inTree"].Add(0);
    }

    public override bool checkPlanPrecondition(Dictionary<string,int> inputState){
        return true;
    }

    public override Dictionary<string,int> applyPostcondition(Dictionary<string, int> inputState){ //Idle does nothing to the state.
        Dictionary<string,int> result = new Dictionary<string, int>();
        foreach(string key in inputState.Keys){
            result.Add(key, inputState[key]);
        }
       
        return result;
    }

    public override bool checkCondition(GameObject aSquirrel)
    {
        return true; //A squirrel can go idle at any time from anywhere if that is its goal.
    }

    //Idling doesnt have any functionality so just return.
    public override bool performAction(GameObject aSquirrel)
    {
        return true;
    }

    public override Action getClone(){
        return new IdleSitAction(duration);
    }

}

public class IdleAction : Action{

    //Since we have no target gameobject here, we add this to make a slight modification.
    private Vector3 targetLocation;

    public IdleAction(GameObject aTarget, float aDuration) : base(aDuration){
        preconditions.Add("inTree", new List<int>());
        preconditions["inTree"].Add(0);

        //aTarget is the squirrel.
            Vector3 result = aTarget.transform.position;
            for (int i = 0; i < 30; i++){
                Vector3 randomPoint = aTarget.transform.position + Random.insideUnitSphere * 5f;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas)){
                result = hit.position;
                }
            }
            targetLocation = result;
            target = aTarget;
    }

    public override Vector3 getTargetLocation(){
        return targetLocation;
    }

    public override bool checkPlanPrecondition(Dictionary<string,int> inputState){
        return true;
    }

    public override Dictionary<string,int> applyPostcondition(Dictionary<string, int> inputState){ //Idle does nothing to the state.
        Dictionary<string,int> result = new Dictionary<string, int>();
        foreach(string key in inputState.Keys){
            result.Add(key, inputState[key]);
        }
       
        return result;
    }

    //A squirrel can't wander around if it was initially in a tree.
    public override bool checkCondition(GameObject aSquirrel)
    {
        if(aSquirrel.GetComponent<SquirrelScript>().getTreeState() == 1){
            return false;
        }
        return true;
    }

    // Idle doesn't have any functionality, so just return.
    public override bool performAction(GameObject aSquirrel)
    {
        return true;
    }

    public override Action getClone(){
        return new IdleAction(target, duration);
    }
}

public class ClimbTreeAction : Action{ 

    public ClimbTreeAction(float aDuration) : base(aDuration){
        preconditions.Add("inTree", new List<int>());
        preconditions["inTree"].Add(0);
    }

    public override bool checkPlanPrecondition(Dictionary<string,int> inputState){
        if(inputState.ContainsKey("inTree")){
            for(int i = 0; i < preconditions["inTree"].Count; i++){
                if(inputState["inTree"] == preconditions["inTree"][i]){
                    return true;
                }
            }
        }
        return false;
    }

    public override Dictionary<string, int> applyPostcondition(Dictionary<string, int> inputState){ //Changes "inTree" from 0 to 1.
        Dictionary<string,int> result = new Dictionary<string, int>();
        foreach(string key in inputState.Keys){
            result.Add(key, inputState[key]);
        }
       
        if(result.ContainsKey("inTree")){
            result["inTree"] = 1; 
        }
        return result;
    }

    public override bool checkCondition(GameObject aSquirrel)
    {
        return ( (aSquirrel.GetComponent<SquirrelScript>().getTreeState()) == 0 && (! target.GetComponent<TreeScript>().containsSquirrel()) );
    }

    public override bool performAction(GameObject aSquirrel)
    {
        //Moves a squirrel to the top of a tree. Move the squirrel object to the target position + y = 1.7f
        aSquirrel.GetComponent<SquirrelScript>().setTreeState(true); //Squirrel is in the tree now.
        aSquirrel.GetComponent<SquirrelScript>().setCurrentTree(target);
        target.GetComponent<TreeScript>().setOccupantSquirrel(aSquirrel);
        return true; 
    }

    public override Action getClone(){
        return new ClimbTreeAction(duration);
    }

}

public class StoreFoodAction : Action{

    public StoreFoodAction(float aDuration) : base(aDuration){ //Must be in tree and have 1, 2 or 3 pieces of food.
        preconditions.Add("foodCount", new List<int>());
        preconditions["foodCount"].Add(1);
        preconditions["foodCount"].Add(2);
        preconditions["foodCount"].Add(3);
        preconditions.Add("inTree", new List<int>());
        preconditions["inTree"].Add(1);
    }
    
    public override bool checkPlanPrecondition(Dictionary<string,int> inputState){   
        if(inputState.ContainsKey("inTree") && inputState.ContainsKey("foodCount")){
            for(int i = 0; i < preconditions["inTree"].Count; i++){
                if(inputState["inTree"] == preconditions["inTree"][i]){
                    
                    for(int j = 0; j < preconditions["foodCount"].Count; j++){
                        if(inputState["foodCount"] == preconditions["foodCount"][j]){
                            return true;
                        }
                    }

                }
            }
        }
        return false;
    }

    public override Dictionary<string, int> applyPostcondition(Dictionary<string, int> inputState){ //Changes foodCount to 0
        Dictionary<string,int> result = new Dictionary<string, int>();
        foreach(string key in inputState.Keys){
            result.Add(key, inputState[key]);
        }
       
        if(result.ContainsKey("foodCount")){
            result["foodCount"] = 0; 
        }
        return result;
    }

    public override bool checkCondition(GameObject aSquirrel)
    {
        return ((aSquirrel.GetComponent<SquirrelScript>().getFoodCount() != 0) && (aSquirrel.GetComponent<SquirrelScript>().getTreeState() == 1));
    }

    public override bool performAction(GameObject aSquirrel)
    {  
        aSquirrel.GetComponent<SquirrelScript>().clearFoodCount();
        return true;
    }

    public override Action getClone(){
        return new StoreFoodAction(duration);
    }

}

public class DescendTreeAction : Action{

    public DescendTreeAction(float aDuration) : base(aDuration){
        preconditions.Add("inTree", new List<int>());
        preconditions["inTree"].Add(1);
    }

    public override bool checkPlanPrecondition(Dictionary<string,int> inputState){
        if(inputState.ContainsKey("inTree")){
            for(int i = 0; i < preconditions["inTree"].Count; i++){
                if(inputState["inTree"] == preconditions["inTree"][i]){
                    return true;
                }
            }
        }
        return false;
    }

    public override Dictionary<string, int> applyPostcondition(Dictionary<string, int> inputState){ //Changes "inTree" from 1 to 0.
        Dictionary<string,int> result = new Dictionary<string, int>();
        foreach(string key in inputState.Keys){
            result.Add(key, inputState[key]);
        }
       
        if(result.ContainsKey("inTree")){
            result["inTree"] = 0; 
        }
        return result;
    }

    public override bool checkCondition(GameObject aSquirrel)
    {
        return aSquirrel.GetComponent<SquirrelScript>().getTreeState() == 1;
    }

    public override bool performAction(GameObject aSquirrel)
    {
        Vector3 spawnPoint = target.transform.position;
        spawnPoint.y = 1.075f;
        spawnPoint.z = spawnPoint.z + 0.02f; 
        aSquirrel.transform.position = spawnPoint;
        aSquirrel.GetComponent<SquirrelScript>().setTreeState(false);
        aSquirrel.GetComponent<SquirrelScript>().setCurrentTree(null);
        target.GetComponent<TreeScript>().setOccupantSquirrel(null);
        return true;
    }

    public override Action getClone(){
        return new DescendTreeAction(duration);
    }
}

public class TakeNutAction: Action{

    public TakeNutAction(float aDuration) : base(aDuration){
        preconditions.Add("foodCount", new List<int>());
        preconditions["foodCount"].Add(0);
        preconditions["foodCount"].Add(1);
        preconditions["foodCount"].Add(2);
        preconditions.Add("inTree", new List<int>());
        preconditions["inTree"].Add(0);
    }

    public override bool checkPlanPrecondition(Dictionary<string,int> inputState){
        if(inputState.ContainsKey("inTree") && inputState.ContainsKey("foodCount")){
            for(int i = 0; i < preconditions["inTree"].Count; i++){
                if(inputState["inTree"] == preconditions["inTree"][i]){
                    
                    for(int j = 0; j < preconditions["foodCount"].Count; j++){
                        if(inputState["foodCount"] == preconditions["foodCount"][j]){
                            return true;
                        }
                    }

                }
            }
        }
        return false;
    }

    public override Dictionary<string, int> applyPostcondition(Dictionary<string, int> inputState){ //Increments "foodCount"
        Dictionary<string,int> result = new Dictionary<string, int>();
        foreach(string key in inputState.Keys){
            result.Add(key, inputState[key]);
        }
       
        if(result.ContainsKey("foodCount")){
            result["foodCount"]++; 
        }
        return result;
    }

    public override bool checkCondition(GameObject aSquirrel)
    {
        return ((aSquirrel.GetComponent<SquirrelScript>().getTreeState() == 0) && (aSquirrel.GetComponent<SquirrelScript>().getFoodCount() != 3));
    }

    public override bool performAction(GameObject aSquirrel)
    { 
        //Need to add to squirrel's foodCount while also destroying the nut object.

        //Check here that if the target no longer exists (= null) then return.
        if(target == null){
            return true;
        }

        UnityEngine.Object.Destroy(target);
        aSquirrel.GetComponent<SquirrelScript>().incrementFoodCount();
        return true;
    }

    public override Action getClone(){
        return new TakeNutAction(duration);
    }
}

public class InvestigateGarbageAction: Action{

    public InvestigateGarbageAction(float aDuration) : base(aDuration){
        preconditions.Add("foodCount", new List<int>()); //We can only do this if we have no food, since we can't mix garbage food with nuts.
        preconditions["foodCount"].Add(0);
        preconditions.Add("inTree", new List<int>());
        preconditions["inTree"].Add(0);
    }

    public override bool checkPlanPrecondition(Dictionary<string,int> inputState){
        if(inputState.ContainsKey("inTree") && inputState.ContainsKey("foodCount")){
            for(int i = 0; i < preconditions["inTree"].Count; i++){
                if(inputState["inTree"] == preconditions["inTree"][i]){
                    
                    for(int j = 0; j < preconditions["foodCount"].Count; j++){
                        if(inputState["foodCount"] == preconditions["foodCount"][j]){
                            return true;
                        }
                    }

                }
            }
        }
        return false;
    }

    public override Dictionary<string, int> applyPostcondition(Dictionary<string, int> inputState){ //Sets foodcount to 3
        Dictionary<string,int> result = new Dictionary<string, int>();
        foreach(string key in inputState.Keys){
            result.Add(key, inputState[key]);
        }
       
        if(result.ContainsKey("foodCount")){
            result["foodCount"] = 3; 
        }
        return result;
    }
    
    public override bool checkCondition(GameObject aSquirrel)
    {
        return ((aSquirrel.GetComponent<SquirrelScript>().getTreeState() == 0) && (aSquirrel.GetComponent<SquirrelScript>().getFoodCount() == 0));
    }

    public override bool performAction(GameObject aSquirrel)
    {
        GarbageCanScript targetScript =  target.GetComponent<GarbageCanScript>();

        if(targetScript.containsSquirrel){ //Case where there is already a squirrel in the can. We simply return, which means the squirrel will immediately make a new plan.
            return true;
        }else{

            if(!targetScript.isEmpty()){ //Case where squirrel needs to get stuck in the can. For who knows what reason this operated opposite to as expected, which is why I just put the negation.
                aSquirrel.GetComponent<SquirrelScript>().updateTextPlan.GetComponent<TextMesh>().text = "Stuck!";
                targetScript.squirrelStuck(aSquirrel);
                return true;

            }else{ //Case where the squirrel gets the food and can leave.
                targetScript.changeState();
                aSquirrel.GetComponent<SquirrelScript>().incrementFoodCount();
                aSquirrel.GetComponent<SquirrelScript>().incrementFoodCount();
                aSquirrel.GetComponent<SquirrelScript>().incrementFoodCount();
                return true;
            }

        }
    }

    public override Action getClone(){
        return new InvestigateGarbageAction(duration);
    }
}
