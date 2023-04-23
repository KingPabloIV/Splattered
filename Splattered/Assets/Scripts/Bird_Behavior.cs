using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bird_Behavior : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public GameObject target;
    Rigidbody2D rb;
    CircleCollider2D hitbox;
    private Bird_Data data;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        data = GetComponent<Bird_Data>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        data.targetLocation = (Vector2)target.transform.position + new Vector2(0, data.heightAboveTarget);
        if(Vector2.Distance(data.targetLocation, (Vector2)transform.position) > data.slowDistance)
            data.isUsingSlowSpeed = false;
        else if ((Vector2.Distance(data.targetLocation, (Vector2)transform.position) < data.slowDistance/1.5f))
            data.isUsingSlowSpeed = true;
        fly();
    }

    void fly(){
        #region Acceleration Calculation
        float accelerationRate = data.accelAmount;
        float slowDistance = data.attackRange - data.heightAboveTarget;
        if (Vector2.Distance(data.targetLocation, (Vector2)transform.position) < slowDistance * 0.05f){
            data.targetSpeed = 0;
            accelerationRate = data.deccelAmount;
        }

        else if(data.isUsingSlowSpeed == true){
            data.targetSpeed = data.maxSpeed * data.slowSpeedMultiplier;
            accelerationRate = data.deccelAmount;
        }
        else if (data.isUsingSlowSpeed == false)
            data.targetSpeed = data.maxSpeed;
        
        Vector2 direction = data.targetLocation  - (Vector2)transform.position;
        direction.Normalize();
        #endregion

        #region Apply Movement
        float speedDifference = data.targetSpeed - rb.velocity.magnitude;
        float movement = speedDifference * accelerationRate;
        rb.velocity = rb.velocity.magnitude * direction;
        rb.AddForce(movement * direction, ForceMode2D.Force);
        #endregion
        
    }
}
