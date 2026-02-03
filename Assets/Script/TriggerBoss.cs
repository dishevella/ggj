using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TriggerBoss : MonoBehaviour
{
    public RoundController RC;
    public BossProfile BP;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartBossFight()
    {
        Debug.Log("Start Boss Fight");
        RC.TriggerBossBattle(BP);
    }

}
