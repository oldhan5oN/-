using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("音效一览")]
    public AudioClip gangSound;
    public static AudioManager instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake(){
        if(instance != null&&instance != this){
            Destroy(gameObject);
            return;
        }
        instance=this;
    }
    // Update is called once per frame
    public void PlayGangSound(){
        AudioSource.PlayClipAtPoint(gangSound,transform.position);
    }
}
