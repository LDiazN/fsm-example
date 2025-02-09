using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canMove = true;
    public float moveSpeed;
    public Transform cameraT;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public bool IsWalking()
    {
        return anim.GetBool("IsWalking");
    }

    public void DisableMovement()
    {
        canMove = false;
        anim.SetBool("IsWalking", false);
    }

    void Update()
    {
        if(!canMove)
            return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if(h != 0 || v != 0)
        {
            Vector3 moveDir = new Vector3(h, 0, v);
            moveDir = Quaternion.AngleAxis(cameraT.eulerAngles.y, Vector3.up) * moveDir;
            transform.forward = moveDir;
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            anim.SetBool("IsWalking", true);
        }
        else
        {
            anim.SetBool("IsWalking", false);
        }
    }
}
