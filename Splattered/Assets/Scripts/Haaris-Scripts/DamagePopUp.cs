using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopUp : MonoBehaviour
{

    public static DamagePopUp Create(Vector3 position, int damageAmount) {

        Transform damagePopUpTransform = Instantiate(GameAsset.i.dPopUp, position, Quaternion.identity);
        
        DamagePopUp popUp = damagePopUpTransform.GetComponent<DamagePopUp>();
        popUp.SetUp(damageAmount);

        return popUp;
    }
    private TextMeshPro textMesh;

    public void Awake(){
        textMesh = transform.GetComponent<TextMeshPro>();
    }
    
    public void SetUp(int damageAmount){
        textMesh.SetText(damageAmount.ToString());
    }
}
