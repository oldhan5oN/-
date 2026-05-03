using UnityEngine;

public class OnJonitGimoz : MonoBehaviour
{
    private ConfigurableJoint joint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDrawGizmos()
{
    if (joint == null)
        joint = GetComponent<ConfigurableJoint>();

    if (joint == null)
        return;

    // 盘子自身 anchor 的世界坐标
    Vector3 anchorWorld = transform.TransformPoint(joint.anchor);

    // connectedAnchor 的世界坐标
    Vector3 connectedAnchorWorld;

    if (joint.connectedBody != null)
    {
        connectedAnchorWorld = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
    }
    else
    {
        // connectedBody 为 null 时，connectedAnchor 近似按世界点理解
        connectedAnchorWorld = joint.connectedAnchor;
    }

    // 画盘子 anchor
    Gizmos.color = Color.red;
    Gizmos.DrawSphere(anchorWorld, 0.035f);

    // 画 connectedAnchor
    Gizmos.color = Color.green;
    Gizmos.DrawSphere(connectedAnchorWorld, 0.035f);

    // 画两点之间的连线
    Gizmos.color = Color.yellow;
    Gizmos.DrawLine(anchorWorld, connectedAnchorWorld);
}
}
