using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public CharacterController controller;
    public Camera camera;
    public float moveSpeed = 5.0f;
    public float gravity = 20.0f;
    public float lookSensitivity = 20.0f;

    private float xRotation = 0;

    //First make the cursor invisible, and lock it.
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!((Input.GetAxisRaw("Horizontal") == 0) && (Input.GetAxisRaw("Vertical") == 0))) //Added this if statement because occaisionally had player moving without any inputs otherwise.
        {
            //First, we define our "move forward/backward" and "move right/sideways" vectors as well as the general moveDirection vector we'll apply to our player.
            Vector3 moveDirection = Vector3.zero;
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            //The speed should be affected by whether we are in the air or not.
            float ySpeed = controller.isGrounded ? moveSpeed * Input.GetAxisRaw("Horizontal") : moveSpeed * Input.GetAxisRaw("Horizontal") * 0.5f;
            float xSpeed = controller.isGrounded ? moveSpeed * Input.GetAxisRaw("Vertical") : moveSpeed * Input.GetAxisRaw("Vertical") * 0.5f;

            float yDirection = moveDirection.y;
            moveDirection = (forward * xSpeed * Time.deltaTime) + (right * ySpeed * Time.deltaTime);

            //If we're in the air, we want to fall so we apply gravity.
            if (!controller.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }
            //Finally we can move the controller, and then rotate our camera if need be.
            controller.Move(moveDirection);
        }
        rotateCamera();
    }

    void rotateCamera()
    {
        //We use Mathf.Clamp to rotate the camera, because otherwise we get wonky behavior.
        xRotation += -Input.GetAxis("Mouse Y") * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -70, 70);
        camera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        //Finally we can rotate the player instead of the camera to make things easier (no wonky behavior unlike moving in Y)
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSensitivity, 0);
    }
}
