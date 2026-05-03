using UnityEngine;

public class StickUprightDetector : MonoBehaviour
{
    public bool isUpright=true;
    public GameObject plate;
    public bool Istrigger=false;
    public PlateJugglingController plateJugglingController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(plateJugglingController.IsStickUpright()&&Istrigger==false)
        {       plateJugglingController.SpawnPlateAboveStick();
                Istrigger=true;}
        
    }
}
