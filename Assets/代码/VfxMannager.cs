using System.Collections.Generic;
using System.Runtime;
using UnityEngine;
using UnityEngine.VFX;

public class VfxMannager : MonoBehaviour
{
    public static VfxMannager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 场景切换时保留（可选）
        }
        else
        {
            Destroy(gameObject); // 防止重复
        }
    }
    public List<GameObject> Vfx=new List<GameObject>();
    public VisualEffect Ins(int a,Transform b){
        GameObject instance=Instantiate(Vfx[a],b);
        var vfx=instance.GetComponent<VisualEffect>();
        return vfx;
    }

}
