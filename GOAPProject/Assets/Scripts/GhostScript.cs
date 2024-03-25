using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GhostScript : MonoBehaviour
{
    public bool isGhost = false;
    public Camera camera;
    public Text ghostStateText;
    public GameObject nutPrefab;

    private void changeState(){
        if(isGhost){
            isGhost = false;
        }else{
            isGhost = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Jump")){ //Enable/disable ghost-mode.
            this.changeState();
        }


         if (Input.GetMouseButtonDown(0) && isGhost)
        {
            //Cast a ray out from the camera.
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if(Physics.Raycast(ray, out hit)) //We have a hit. If it was a nut, destroy it. If it was the ground, create a nut. If it was a garbage can, change its state.
            {
                if(hit.collider.gameObject.tag == "Nut"){
                    Destroy(hit.collider.gameObject);
                }else if(hit.collider.gameObject.tag == "Can"){
                    //Can't change the can's state if a squirrel is inside it.
                    if(! hit.collider.gameObject.GetComponent<GarbageCanScript>().containsSquirrel){
                        hit.collider.gameObject.GetComponent<GarbageCanScript>().changeState();
                    }
                }else if(hit.collider.gameObject.tag == "Ground"){
                    Instantiate(nutPrefab, hit.point + new Vector3(0, 0.02f, 0), Quaternion.identity);
                }
            }
        }
        ghostStateText.text = "Ghost-Mode: " + isGhost; //For the player to know what mode they are in.
    }
}
