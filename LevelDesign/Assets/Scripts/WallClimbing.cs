using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallClimbing : MonoBehaviour
{

    [Header("Wall Climbing")]
    public float wallClimbSpeed = 5f;
    public float maxWallClimbTime = 1.5f;
    private float wallClimbTimer;
    public bool isWallClimbing;
    public LayerMask whatIsWall; // Layer du mur
    [Header("Wall Climb Height Limit")]
    public float maxWallHeight = 5f;  // Hauteur max du mur que le joueur peut escalader

    PlayerMovement pm;

    // Start is called before the first frame update
    void Start()
    {
      pm =GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if(IsWallInFront() && !pm._grounded && Input.GetKey( pm._jumpKey) && CanClimbWall())
{
            StartWallClimb();
        }
        if (isWallClimbing && (!IsWallInFront() || pm._grounded))
        {
            StopWallClimb();
        }

    }
    private void FixedUpdate()
    {
        if (isWallClimbing)
        {
            if (wallClimbTimer > 0)
            {
                pm._rb.velocity = new Vector3(pm._rb.velocity.x, wallClimbSpeed, pm._rb.velocity.z);  // Appliquer la montée
                wallClimbTimer -= Time.deltaTime;
            }
            else
            {
                StopWallClimb(); // Arrêter la montée après un certain temps
            }
        }

    }
    bool IsWallInFront()
    {
        return Physics.Raycast(transform.position, pm._orientation.forward, out _, 1f, whatIsWall);
    }
    bool CanClimbWall()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up, out hit, maxWallHeight, whatIsWall))
        {
            return false;  // Il y a un mur au-dessus → Trop haut, on ne peut pas grimper
        }
        return true;  // Pas d'obstacle au-dessus → On peut grimper
    }

    void StartWallClimb()
    {
        isWallClimbing = true;
        wallClimbTimer = maxWallClimbTime;
        pm._rb.useGravity = false;  // Désactive la gravité pendant la montée
    }

    void StopWallClimb()
    {
        isWallClimbing = false;
        pm._rb.useGravity = true;  // Réactive la gravité après la montée
    }
}
