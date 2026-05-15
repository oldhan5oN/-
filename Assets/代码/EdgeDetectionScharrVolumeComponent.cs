using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[VolumeComponentMenuForRenderPipeline("Custom/Edge Detection Scharr", typeof(UniversalRenderPipeline))]
public class EdgeDetectionScharrVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public BoolParameter enable = new BoolParameter(false);

    public ClampedFloatParameter edgeWidth = new ClampedFloatParameter(1f, 0.1f, 10f);
    public ClampedFloatParameter backgroundFade = new ClampedFloatParameter(0f, 0f, 1f);
    public ColorParameter edgeColor = new ColorParameter(Color.black);
    public ColorParameter backgroundColor = new ColorParameter(Color.white);

    public bool IsActive()
    {
        return enable.value;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}