using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb;
    CircleCollider2D hitbox;
    public PlayerMovementData Data;

    #region INPUT PARAMETERS
    private Vector2 moveInput;
    #endregion

    #region CheckGround
    //set in inspector
    [Header("ground check")]
    [SerializeField] private Transform groundCheck; //position of ground check
    [SerializeField] private Vector2 groundCheckSize; //radius of ground check
    public float lastOnGround; //last time player was on ground
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        Data = GetComponent<PlayerMovementData>();
    }

    // Update is called once per frame
    void Update()
    {
        lastOnGround -= (lastOnGround > -0.1) ? Time.deltaTime: 0;
        moveInput.x = Input.GetAxisRaw("Horizontal");
        if(Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, LayerMask.GetMask("Ground")))
            lastOnGround = 0.1f;

    }

    void FixedUpdate()
    {
        Run();
    }

    private void Run()
    {
        float targetSpeed = moveInput.x * Data.maxSpeed;

        #region Caluclate Acceleration
        float accelRate;
        if(lastOnGround > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
        #endregion

        #region Apply Movment
        float speedDifference = targetSpeed - rb.velocity.x;
        float movement = speedDifference * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        // Debug.Log("force: " + movement * Vector2.right);
        #endregion

        #region Friction
        if(lastOnGround > 0 && Mathf.Abs(moveInput.x) < 0.01f){
            float amount = Mathf.Min(Mathf.Abs(rb.velocity.x), Data.runDeccelAmount * Time.deltaTime);
            amount *= Mathf.Sign(rb.velocity.x);
            rb.AddForce(-amount * Vector2.right, ForceMode2D.Force);
        }
            
        #endregion
    }
}
