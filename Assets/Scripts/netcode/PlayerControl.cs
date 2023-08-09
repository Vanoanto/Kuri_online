using System;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerControl : NetworkBehaviour
{
    /////////////////////////////////////////////////////////////////////////////
    public int playerID;

    

    [SerializeField]
    private TextMeshProUGUI playerIDText;

    [SerializeField]
    private TextMeshProUGUI winerText;

    public int placeInGame = -1;

    private bool wereTeleportedFromFinish = false;

    [SerializeField]
    private Vector2 rangeTeleportation = new Vector2(2, 10);
    /////////////////////////////////////////////////////////////////////////////

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

    // Physics

    [SerializeField]
    private const float s_OnGroundVelocity = 5f;

    [SerializeField]
    private const float s_BrakeVelocity = 0.005f;

    [SerializeField]
    private const float s_MaxVelocity = 10f;

    [SerializeField]
    private const int s_Force = 1;

    [SerializeField]
    private const float s_TimeOfAcselerationOfPlatform = 1f;

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

    //
    [SerializeField]
    private gameData gameManagerGameData;
    //
    void Start()
    {

        gameManagerGameData = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameData>();
        playerIDText = GameObject.FindGameObjectWithTag("playerID").GetComponent<TextMeshProUGUI>();
        winerText = GameObject.FindGameObjectWithTag("winerText").GetComponent<TextMeshProUGUI>();
        if (IsClient && IsOwner)
        {
            winerText.gameObject.SetActive(false);
        }

        //Debug.Log(GameObject.FindGameObjectWithTag("playerID").GetComponent<Te>);

        s_RigidBody2d.freezeRotation = true;

        if (IsClient && IsOwner)
        {
            if (!k_Camera.gameObject.activeInHierarchy)
            {
                k_Camera.gameObject.SetActive(true);
            }
        }

        if(IsServer)
        {
            gameManagerGameData.CalcNumPlayersInGame();
            playerID = gameManagerGameData.numPlayersInGame;
        }

        if (IsClient && IsOwner)
        {
            playerIDText.text = "player num" + playerID.ToString();
        }
    }

    // Update is called once per frame
    void Update()
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

    private void UpdateServer()
    {
        //Debug.Log(nm.ConnectedClientsList.Count);
        if (placeInGame == -1)
        {
            if (sk_Request)
            {
                sk_Request = false;

                if (s_NFlips > 0)
                {
                    s_GravityDirection *= -1;
                    s_RigidBody2d.gravityScale = s_GravityDirection;

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
        if(placeInGame == -1)
        {
            if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                UpdateClientPositionServerRpc(true);
            }
        }else
        {
            winerText.gameObject.SetActive(true);
            winerText.text = "YOU WON " + placeInGame.ToString() + " PLACE!!!";
        }
        
    }

    [ServerRpc]
    public void UpdateClientPositionServerRpc(bool request)
    {
        sk_Request = request;
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            FixedUpdateServer();
        }
    }

    private void FixedUpdateServer()
    {
        if(placeInGame == -1)
        {
            if (checkGround())
            {
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
        RaycastHit2D[] raycasthit = Physics2D.BoxCastAll(s_BoxCollider2D.bounds.center, new Vector2(s_BoxCollider2D.bounds.size.y, s_BoxCollider2D.bounds.size.y + extraBoxHeight), 0f, Vector2.zero, 0f);

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