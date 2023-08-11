using System;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEditor.Search;

public class PlayerControl : NetworkBehaviour
{
    //temp here
    private bool wereTeleportedFromFinish = false;

    [SerializeField]
    private Vector2 rangeTeleportation = new Vector2(2, 10);

    public enum KuraState
    {
        //Kissing a wall, ground
        Stand,
        //No speed, air
        Fall,
        //No\Normal speed, ground
        Run,
        //Too much speed, ground
        FlapRun,
        //Normal speed, air
        Fly,
        //Too much speed, air
        Glide
    }

    // *** Constants

    // Objects

    [SerializeField]
    private Camera k_Camera;

    [SerializeField]
    private BoxCollider2D s_BoxCollider2D;

    [SerializeField]
    private Rigidbody2D s_RigidBody2d;

    [SerializeField]
    private Transform s_Transform;

    [SerializeField]
    private GameData gameManagerGameData;

    //[SerializeField]
    //private GameObject s_RedKura;

    //[SerializeField]
    //private GameObject s_BlueKura;

    // Physics

    [SerializeField]
    private float s_OnGroundVelocity;

    [SerializeField]
    private float s_BrakeVelocity;

    [SerializeField]
    private float s_MaxVelocity;

    [SerializeField]
    private int s_Force;

    [SerializeField]
    private float s_TimeOfAcselerationOfPlatform;

    [SerializeField]
    private float s_GravityMultiplier;

    // Logic

    private string[] s_FlipTagList = { "simplePlatform", "player" };

    [SerializeField]
    private const int s_MaxFlips = 1;

    // *** Active

    private bool sk_Request = false;

    private float s_GravityDirection = 1    ;

    private float s_CurrentAcseleration;

    int s_NFlips = 1;

    public KuraState s_State = KuraState.Fall;

    private void Start()
    {
        if (IsClient && IsOwner)
        {
            if (!k_Camera.gameObject.activeInHierarchy)
            {
                k_Camera.gameObject.SetActive(true);
            }
        }

        gameManagerGameData = GameObject.FindGameObjectWithTag("gameManager").GetComponent<GameData>();

        //if(IsClient && IsOwner)
        //{
        //    s_RedKura.SetActive(false);
        //    s_BlueKura.SetActive(true);
        //}
        //else
        //{
        //    s_RedKura.SetActive(true);
        //    s_BlueKura.SetActive(false);
        //}
    }

    // Update is called once per frame
    private void Update()
    {
        // dont like this, maybe will need to do smth before game is running
        if(gameManagerGameData.isGameRunning.Value)
        {
            if (IsServer)
            {
                UpdateServer();
            }

            if (IsClient && IsOwner)
            {
                UpdateClient();
            }
        }
        
        if (IsClient && IsOwner)
        {
            Debug.Log("HELP3");
        }
    }

    private void UpdateServer()
    {
        //Debug.Log(nm.ConnectedClientsList.Count);
        if (GetComponent<PlayerUIManager>().placeInGame.Value == -1)
        {
            if (sk_Request)
            {
                sk_Request = false;

                if (s_NFlips > 0)
                {
                    s_GravityDirection *= -1;
                    s_RigidBody2d.gravityScale = s_GravityDirection * s_GravityMultiplier;
                    s_Transform.localScale = new Vector3(s_Transform.localScale.x, s_Transform.localScale.y * -1, s_Transform.localScale.z);

                    s_NFlips--;
                }
            }
        }
        else
        {
            s_RigidBody2d.gravityScale = 0;
            if(!wereTeleportedFromFinish)
            {
                transform.position += new Vector3(UnityEngine.Random.Range(rangeTeleportation.x, rangeTeleportation.y), 0f, 0f);
                wereTeleportedFromFinish = true;
            }
            
        }
        // YARIK PLIS FIKS !!!! AND SEND DICK PICK IN DARK !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! I WILL:) 8=0
    }

    private void UpdateClient()
    {
        if(GetComponent<PlayerUIManager>().placeInGame.Value == -1)
        {
            if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                UpdateClientPositionServerRpc(true);
            }
        }

        Debug.DrawRay(transform.position, s_RigidBody2d.velocity, Color.red, 1 / 300f);
    }

    [ServerRpc]
    public void UpdateClientPositionServerRpc(bool request)
    {
        sk_Request = request;
    }

    private void FixedUpdate()
    {
        if (gameManagerGameData.isGameRunning.Value)
        {
            if (IsServer)
            {
                FixedUpdateServer();
            }
        }
        
    }

    private void FixedUpdateServer()
    {
        if(GetComponent<PlayerUIManager>().placeInGame.Value == -1)
        {
            if (checkGround())
            {
                Debug.Log("On ground");

                if (s_RigidBody2d.velocity.magnitude > s_OnGroundVelocity)
                {
                    s_RigidBody2d.velocity -= Vector2.right * s_CurrentAcseleration;
                }
                else
                {
                    if (s_RigidBody2d.velocity.magnitude < s_MaxVelocity)
                    {
                        s_RigidBody2d.velocity += Vector2.right * s_CurrentAcseleration;
                    }
                    else
                    {
                        s_RigidBody2d.velocity = Vector2.right * s_OnGroundVelocity;
                    }
                }
            }
            else
            {
                s_RigidBody2d.AddForce(Vector2.right * s_Force);
            }
        }
        else
        {
            s_RigidBody2d.velocity = Vector2.zero;
        }
        
    }

    private bool checkGround()
    {
        float extraBoxHeight = 0.1f;
        RaycastHit2D[] raycasthit = Physics2D.BoxCastAll(s_BoxCollider2D.bounds.center, new Vector2(s_BoxCollider2D.bounds.size.x, s_BoxCollider2D.bounds.size.y + extraBoxHeight), 0f, Vector2.zero, 0f);

        for (int i = 0; i < raycasthit.Length; i++)
        {
            //Debug.Log(raycasthit[i].collider.tag);
            if (raycasthit[i].collider != null && raycasthit[i].collider.CompareTag("simplePlatform"))
            {
                //Debug.Log(raycasthit[i].collider.tag);
                return true;
            }
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        //Debug.Log(collision.gameObject.tag);
        if (Array.Exists(s_FlipTagList, element => collision.gameObject.CompareTag(element)))
        {
            s_NFlips ++;
            s_NFlips = Math.Min(s_NFlips, s_MaxFlips);
            s_NFlips = (s_NFlips > s_MaxFlips) ? s_MaxFlips : s_NFlips;
        }
        s_CurrentAcseleration = Mathf.Abs(s_RigidBody2d.velocity.magnitude - s_OnGroundVelocity) / 50f * s_TimeOfAcselerationOfPlatform;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            Debug.DrawLine(new Vector3(contact.point.x, contact.point.y, transform.position.z), transform.position, Color.red, 2, false);
        }
        //Debug.Break();
    }
}
