using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    public float totalhealth = 100;
    public float playerhealth;
    public Slider playerHealthSlider;
    public bool isTakingDamage = false;
    public bool isHealing = true;
    public float healingTime = 5f;
    public float remainigHealingTime;
    // Start is called before the first frame update
    void Start()
    {
        playerhealth = totalhealth;
        remainigHealingTime = healingTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            DamagePlayer(25f);
        }
        if (isTakingDamage)
        {
            TakingDamage();
        }
        if (isHealing)
        {
            StartHealing();
        }
         
    }

    public void DamagePlayer(float damage)
    {

        if (playerhealth <= 0)
        {
            playerhealth = 0;
            
            return;
        }
        else if (playerhealth > 100)
        {
            playerhealth = 100;
        }
        else
        {
            playerhealth -= damage;
            remainigHealingTime = healingTime;
            isTakingDamage = true;
            isHealing = false;

        }
        playerHealthSlider.value = playerhealth;
    }

    public void TakingDamage()
    {
        remainigHealingTime -= Time.deltaTime;
        if (remainigHealingTime <= 0)
        {
            isTakingDamage = false;
        }
    }
    public void StartHealing()
    {
        playerhealth += Time.deltaTime * 2;
        playerHealthSlider.value = playerhealth;
        if (playerhealth > 100f)
        {
            playerhealth = 100;
            isHealing = false;
        }
    }
}
