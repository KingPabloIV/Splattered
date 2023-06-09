using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bird_Data : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 4;
    public float accelerationTime = 1;
    public float deccelerationTime = 1;
    [Range(0.01f, 1)] public float slowSpeedMultiplier = 0.721f;
    [HideInInspector] public float targetSpeed;
    [HideInInspector] public float accelAmount;
    [HideInInspector] public float deccelAmount;
    public float slowDistance = 9;
    [HideInInspector] public bool isUsingSlowSpeed;
    public float heightAboveTarget = 3;
    [HideInInspector] public Vector2 targetLocation;
    public Vector2 targetMaxCoordinates = new Vector2(13, 6);
    public Vector2 targetMinCoordinates = new Vector2(-13, -6);
    [Space(10)]

    [Header ("Attack")]
    public float attackRange = 12;
    [Range(0.01f, 10)]public float attackSpeed = 2;
    public GameObject bulletPrefab;

    [Space (10)]

    [Header ("Health")]
    public float maxHealth = 10;
    void OnValidate()
    {
        accelAmount = maxSpeed / accelerationTime;
        deccelAmount = maxSpeed / deccelerationTime;
        slowDistance = attackRange - heightAboveTarget;
    }
}
