using UnityEngine;

public class KinectFootGroundingSystem : MonoBehaviour
{
    public enum FootState
    {
        Swing,
        Grounded,
        Airborne
    }

    [System.Serializable]
    public class Leg
    {
        [Header("Name")]
        public string legName = "Leg";

        [Header("Bones")]
        public Transform upperLeg;
        public Transform lowerLeg;
        public Transform foot;

        [Header("Optional Points")]
        public Transform solePoint;
        public Transform kneeHint;

        [Header("IK Enable")]
        public bool enable = true;

        [Range(0f, 1f)]
        public float maxIKWeight = 1f;

        [Range(0f, 1f)]
        public float footRotationWeight = 0.35f;

        [Header("Contact State")]
        public float enterGroundedDistance = 0.07f;
        public float exitGroundedDistance = 0.18f;

        [Tooltip("只使用水平脚速判断是否为稳定支撑脚")]
        public float maxGroundedFootSpeed = 0.35f;

        [Tooltip("只使用水平脚速判断是否强制进入 Swing")]
        public float forceSwingFootSpeed = 0.85f;

        [Header("Weight Smoothing")]
        public float weightUpSpeed = 8f;
        public float weightDownSpeed = 12f;

        [Header("Foot Pinning")]
        public bool enableFootPinning = false;
        public float pinReleaseDistance = 0.25f;

        [Header("Runtime Debug")]
        public FootState state = FootState.Swing;

        [Range(0f, 1f)]
        public float currentIKWeight;

        [HideInInspector] public bool hasGroundHit;
        [HideInInspector] public Vector3 groundPoint;
        [HideInInspector] public Vector3 groundNormal = Vector3.up;
        [HideInInspector] public float signedHeightToGround;
        [HideInInspector] public float absHeightToGround;
        [HideInInspector] public float soleSpeed;

        [HideInInspector] public Vector3 lastSolePosition;
        [HideInInspector] public bool hasLastSolePosition;

        [HideInInspector] public Vector3 smoothedTargetFootPosition;
        [HideInInspector] public Quaternion smoothedTargetFootRotation;
        [HideInInspector] public bool hasSmoothedTarget;

        [HideInInspector] public Vector3 pinnedSolePosition;
        [HideInInspector] public Vector3 pinnedGroundNormal;
        [HideInInspector] public bool hasPinnedSole;
    }

    [Header("Body")]
    [Tooltip("角色整体 root。应该拖 AvatarController 实际移动的 bodyRoot，不要拖 hips。")]
    public Transform bodyRoot;

    [Tooltip("角色骨盆 / hips。用于计算跳跃和下蹲速度。")]
    public Transform hips;

    [Header("Legs")]
    public Leg leftLeg;
    public Leg rightLeg;

    [Header("Ground")]
    public LayerMask groundLayer = ~0;

    [Header("Raycast")]
    public float rayStartHeight = 0.5f;
    public float rayLength = 1.3f;

    [Tooltip("脚底和地面之间保留的安全高度。")]
    public float footGroundOffset = 0.025f;

    [Header("Body Anti Penetration")]
    public bool enableBodyAntiPenetration = true;

    [Tooltip("IK 之前先抬 bodyRoot，避免 IK 在身体过低时硬扭腿。")]
    public bool preIKBodyCorrection = true;

    [Tooltip("IK 之后再检查一次，作为最终防穿地保险。")]
    public bool postIKBodyCorrection = true;

    [Tooltip("是否让 Swing 脚也参与防穿地。想严格保证脚永远不穿地，就打开。")]
    public bool strictAllFeetAboveGround = true;

    [Tooltip("额外安全高度，防止数值误差导致脚底刚好贴进地面。")]
    public float extraBodyRaiseOffset = 0.003f;

    [Tooltip("单帧最大向上修正。0 表示不限制。")]
    public float maxBodyRaisePerFrame = 0.5f;

    [Tooltip("坡面修正用。越小越容易在陡坡上产生过大修正。")]
    public float minGroundNormalY = 0.25f;

    [Tooltip("穿地超过这个值时，强制 IK 权重为 1。")]
    public float penetrationTolerance = 0.005f;

    [Header("Jump Detection")]
    public bool enableJumpDetection = true;

    [Tooltip("双脚都高于这个距离，并且 hips 有向上速度时，认为进入 Airborne。")]
    public float jumpFootDistance = 0.20f;

    [Tooltip("hips 向上速度超过该值时，允许进入 Airborne。")]
    public float jumpUpSpeed = 0.45f;

    [Tooltip("Airborne 后，脚重新接近地面到该距离内时，允许落地。")]
    public float landingFootDistance = 0.12f;

    [Tooltip("落地时脚的最大水平速度。")]
    public float landingFootSpeed = 0.65f;

    [Header("IK Solver")]
    public int solverIterations = 6;
    public float targetPositionSmoothSpeed = 12f;
    public float targetRotationSmoothSpeed = 10f;

    [Header("Knee Hint")]
    public bool useKneeHint = true;

    [Range(0f, 1f)]
    public float kneeHintWeight = 0.7f;

    [Header("IK Correction Limit")]
    [Tooltip("第一版建议先关闭。确认稳定后再开启角度限制。")]
    public bool limitIKCorrection = false;

    public float maxUpperLegCorrectionAngle = 35f;
    public float maxLowerLegCorrectionAngle = 45f;
    public float maxFootCorrectionAngle = 35f;

    [Header("Knee Bend Protection")]
    public bool protectKneeBend = true;

    [Tooltip("0 means almost straight. Increase if knee flips when fully straight.")]
    public float minKneeBendAngle = 2f;

    [Tooltip("Larger means knee can fold more.")]
    public float maxKneeBendAngle = 150f;

    [Header("Debug")]
    public bool drawDebugRay = true;
    public bool drawDebugTarget = true;
    public bool drawDebugBodyCorrection = true;

    private bool bodyAirborne;
    private Vector3 lastHipsPosition;
    private bool hasLastHipsPosition;
    private float hipsVerticalSpeed;

    private Transform BodyRoot
    {
        get
        {
            return bodyRoot != null ? bodyRoot : transform;
        }
    }

    private void Reset()
    {
        solverIterations = 6;
        rayStartHeight = 0.5f;
        rayLength = 1.3f;
        footGroundOffset = 0.025f;
        extraBodyRaiseOffset = 0.003f;
        maxBodyRaisePerFrame = 0.5f;
        minGroundNormalY = 0.25f;
        limitIKCorrection = false;
    }

    private void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        UpdateHipsSpeed(dt);

        // 第一次采样：读取 Kinect / AvatarController 更新后的姿态。
        SampleLegGround(leftLeg, dt, true);
        SampleLegGround(rightLeg, dt, true);

        UpdateBodyAirborneState();

        // Airborne 时不能把身体吸回地面，否则跳不起来。
        if (!bodyAirborne && enableBodyAntiPenetration && preIKBodyCorrection)
        {
            bool moved = ApplyBodyAntiPenetration();

            if (moved)
            {
                // bodyRoot 被移动后，脚底和地面的关系已经变了，必须重新采样。
                SampleLegGround(leftLeg, dt, false);
                SampleLegGround(rightLeg, dt, false);
            }
        }

        UpdateLegStateAndWeight(leftLeg, dt);
        UpdateLegStateAndWeight(rightLeg, dt);

        ApplyLegIK(leftLeg, dt);
        ApplyLegIK(rightLeg, dt);

        // IK 后最终保险。脚旋转、CCD 限制、骨骼比例问题都可能再次造成穿地。
        if (!bodyAirborne && enableBodyAntiPenetration && postIKBodyCorrection)
        {
            SampleLegGround(leftLeg, dt, false);
            SampleLegGround(rightLeg, dt, false);

            bool moved = ApplyBodyAntiPenetration();

            if (moved)
            {
                // 最终状态再采样一次，方便 Debug 和下一帧状态判断。
                SampleLegGround(leftLeg, dt, false);
                SampleLegGround(rightLeg, dt, false);
            }
        }
    }

    private void UpdateHipsSpeed(float dt)
    {
        if (hips == null)
        {
            hipsVerticalSpeed = 0f;
            return;
        }

        if (!hasLastHipsPosition)
        {
            lastHipsPosition = hips.position;
            hasLastHipsPosition = true;
            hipsVerticalSpeed = 0f;
            return;
        }

        hipsVerticalSpeed = (hips.position.y - lastHipsPosition.y) / dt;
        lastHipsPosition = hips.position;
    }

    private void SampleLegGround(Leg leg, float dt, bool updateSpeed)
    {
        if (!IsLegValid(leg))
        {
            return;
        }

        Transform sole = GetSoleTransform(leg);
        Vector3 solePosition = sole.position;

        if (updateSpeed)
        {
            if (leg.hasLastSolePosition)
            {
                Vector3 delta = solePosition - leg.lastSolePosition;

                // 关键点：只用水平速度判断脚是否在移动。
                // 下蹲造成的 Y 方向变化不应该直接让脚退出 Grounded。
                Vector3 horizontalDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
                leg.soleSpeed = horizontalDelta.magnitude / dt;
            }
            else
            {
                leg.soleSpeed = 0f;
                leg.hasLastSolePosition = true;
            }

            leg.lastSolePosition = solePosition;
        }

        Vector3 rayOrigin = solePosition + Vector3.up * rayStartHeight;

        if (drawDebugRay)
        {
            Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);
        }

        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                rayLength,
                groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            leg.hasGroundHit = true;
            leg.groundPoint = hit.point;
            leg.groundNormal = hit.normal;

            leg.signedHeightToGround = Vector3.Dot(
                solePosition - hit.point,
                hit.normal
            );

            leg.absHeightToGround = Mathf.Abs(leg.signedHeightToGround);

            if (drawDebugTarget)
            {
                Debug.DrawLine(
                    hit.point,
                    hit.point + hit.normal * 0.15f,
                    Color.green
                );
            }
        }
        else
        {
            leg.hasGroundHit = false;
            leg.signedHeightToGround = 999f;
            leg.absHeightToGround = 999f;
            leg.groundNormal = Vector3.up;
        }
    }

    private void UpdateBodyAirborneState()
    {
        if (!enableJumpDetection || hips == null)
        {
            bodyAirborne = false;
            return;
        }

        bool leftHigh = IsFootHighForJump(leftLeg);
        bool rightHigh = IsFootHighForJump(rightLeg);

        if (!bodyAirborne)
        {
            if (leftHigh && rightHigh && hipsVerticalSpeed > jumpUpSpeed)
            {
                bodyAirborne = true;
                ForceReleaseLeg(leftLeg);
                ForceReleaseLeg(rightLeg);
            }
        }
        else
        {
            bool leftCanLand = CanFootLand(leftLeg);
            bool rightCanLand = CanFootLand(rightLeg);

            if (leftCanLand || rightCanLand)
            {
                bodyAirborne = false;
            }
        }
    }

    private bool IsFootHighForJump(Leg leg)
    {
        if (!IsLegValid(leg))
        {
            return false;
        }

        if (!leg.hasGroundHit)
        {
            return true;
        }

        return leg.signedHeightToGround > jumpFootDistance;
    }

    private bool CanFootLand(Leg leg)
    {
        if (!IsLegValid(leg))
        {
            return false;
        }

        if (!leg.hasGroundHit)
        {
            return false;
        }

        // 注意：这里不用 abs。
        // 如果脚已经穿到地面下面，signedHeightToGround 会是负数，也应该允许退出 Airborne。
        return leg.signedHeightToGround <= landingFootDistance &&
               leg.soleSpeed < landingFootSpeed &&
               hipsVerticalSpeed <= jumpUpSpeed;
    }

    private void ForceReleaseLeg(Leg leg)
    {
        if (leg == null)
        {
            return;
        }

        leg.state = FootState.Airborne;
        leg.currentIKWeight = 0f;
        leg.hasPinnedSole = false;
        leg.hasSmoothedTarget = false;
    }

    private void UpdateLegStateAndWeight(Leg leg, float dt)
    {
        if (!IsLegValid(leg))
        {
            return;
        }

        if (!leg.enable)
        {
            leg.currentIKWeight = Mathf.MoveTowards(
                leg.currentIKWeight,
                0f,
                dt * leg.weightDownSpeed
            );

            leg.state = FootState.Swing;
            leg.hasPinnedSole = false;
            return;
        }

        FootState previousState = leg.state;
        float targetWeight = 0f;

        if (bodyAirborne)
        {
            leg.state = FootState.Airborne;
            targetWeight = 0f;
            leg.hasPinnedSole = false;
        }
        else if (!leg.hasGroundHit)
        {
            leg.state = FootState.Swing;
            targetWeight = 0f;
            leg.hasPinnedSole = false;
        }
        else
        {
            bool isPenetratingGround =
                leg.signedHeightToGround < -penetrationTolerance;

            bool canEnterGrounded =
                leg.signedHeightToGround <= leg.enterGroundedDistance &&
                leg.soleSpeed <= leg.maxGroundedFootSpeed;

            bool shouldExitGrounded =
                leg.signedHeightToGround > leg.exitGroundedDistance ||
                leg.soleSpeed >= leg.forceSwingFootSpeed;

            // 关键规则：
            // 只要脚已经穿地，不能让它继续 Swing。
            // 穿地时强制进入 Grounded，并且 IK 给满权重。
            if (isPenetratingGround)
            {
                leg.state = FootState.Grounded;
                targetWeight = 1f;
            }
            else if (leg.state == FootState.Grounded)
            {
                if (shouldExitGrounded)
                {
                    leg.state = FootState.Swing;
                    targetWeight = 0f;
                    leg.hasPinnedSole = false;
                }
                else
                {
                    leg.state = FootState.Grounded;
                    targetWeight = leg.maxIKWeight;
                }
            }
            else
            {
                if (canEnterGrounded)
                {
                    leg.state = FootState.Grounded;
                    targetWeight = leg.maxIKWeight;

                    if (leg.enableFootPinning && !leg.hasPinnedSole)
                    {
                        leg.pinnedSolePosition =
                            leg.groundPoint + leg.groundNormal * footGroundOffset;

                        leg.pinnedGroundNormal = leg.groundNormal;
                        leg.hasPinnedSole = true;
                    }
                }
                else
                {
                    leg.state = FootState.Swing;
                    targetWeight = 0f;
                    leg.hasPinnedSole = false;
                }
            }
        }

        if (previousState != leg.state && leg.state != FootState.Grounded)
        {
            leg.hasSmoothedTarget = false;
        }

        float speed = targetWeight > leg.currentIKWeight
            ? leg.weightUpSpeed
            : leg.weightDownSpeed;

        leg.currentIKWeight = Mathf.MoveTowards(
            leg.currentIKWeight,
            targetWeight,
            dt * speed
        );
    }

    private bool ApplyBodyAntiPenetration()
    {
        Transform root = BodyRoot;

        if (root == null)
        {
            return false;
        }

        float raiseY = 0f;

        raiseY = Mathf.Max(raiseY, GetRequiredBodyRaiseY(leftLeg));
        raiseY = Mathf.Max(raiseY, GetRequiredBodyRaiseY(rightLeg));

        if (maxBodyRaisePerFrame > 0f)
        {
            raiseY = Mathf.Min(raiseY, maxBodyRaisePerFrame);
        }

        if (raiseY <= 0f)
        {
            return false;
        }

        root.position += Vector3.up * raiseY;

        if (drawDebugBodyCorrection)
        {
            Debug.DrawRay(root.position, Vector3.up * raiseY, Color.cyan);
        }

        return true;
    }

    private float GetRequiredBodyRaiseY(Leg leg)
    {
        if (!IsLegValid(leg))
        {
            return 0f;
        }

        if (!leg.hasGroundHit)
        {
            return 0f;
        }

        if (bodyAirborne)
        {
            return 0f;
        }

        // 如果 strictAllFeetAboveGround = true，
        // 那 Grounded 和 Swing 脚都会参与防穿地。
        //
        // 如果为 false，
        // 只有 Grounded 脚参与，Swing 脚只在严重穿地时参与。
        if (!strictAllFeetAboveGround)
        {
            bool isSupportFoot = leg.state == FootState.Grounded;
            bool isBadlyPenetrating = leg.signedHeightToGround < -0.03f;

            if (!isSupportFoot && !isBadlyPenetrating)
            {
                return 0f;
            }
        }

        float desiredSignedHeight = footGroundOffset + extraBodyRaiseOffset;
        float penetration = desiredSignedHeight - leg.signedHeightToGround;

        if (penetration <= 0f)
        {
            return 0f;
        }

        float normalY = Mathf.Max(leg.groundNormal.y, minGroundNormalY);

        // 因为我们只移动 bodyRoot.y，不沿地面法线移动，
        // 所以需要除以 normal.y。
        float requiredRaiseY = penetration / normalY;

        return Mathf.Max(0f, requiredRaiseY);
    }

    private void ApplyLegIK(Leg leg, float dt)
    {
        if (!IsLegValid(leg) || !leg.enable)
        {
            return;
        }

        if (bodyAirborne)
        {
            return;
        }

        if (!leg.hasGroundHit)
        {
            return;
        }

        bool isPenetrating =
            leg.signedHeightToGround < -penetrationTolerance;

        float ikWeight = isPenetrating
            ? 1f
            : leg.currentIKWeight;

        if (ikWeight <= 0.001f)
        {
            return;
        }

        Transform sole = GetSoleTransform(leg);

        Quaternion upperBefore = leg.upperLeg.rotation;
        Quaternion lowerBefore = leg.lowerLeg.rotation;
        Quaternion footBefore = leg.foot.rotation;

        Vector3 targetSolePosition;
        Vector3 targetGroundNormal;

        if (leg.enableFootPinning &&
            leg.hasPinnedSole &&
            leg.state == FootState.Grounded)
        {
            float pinDistance = Vector3.Distance(
                sole.position,
                leg.pinnedSolePosition
            );

            if (pinDistance > leg.pinReleaseDistance)
            {
                leg.hasPinnedSole = false;

                targetSolePosition =
                    leg.groundPoint + leg.groundNormal * footGroundOffset;

                targetGroundNormal = leg.groundNormal;
            }
            else
            {
                targetSolePosition = leg.pinnedSolePosition;
                targetGroundNormal = leg.pinnedGroundNormal;
            }
        }
        else
        {
            targetSolePosition =
                leg.groundPoint + leg.groundNormal * footGroundOffset;

            targetGroundNormal = leg.groundNormal;
        }

        Vector3 footToSoleOffset = leg.foot.position - sole.position;
        Vector3 targetFootPosition = targetSolePosition + footToSoleOffset;

        float positionT = DampFactor(targetPositionSmoothSpeed, dt);

        if (!leg.hasSmoothedTarget)
        {
            leg.smoothedTargetFootPosition = targetFootPosition;
            leg.smoothedTargetFootRotation = leg.foot.rotation;
            leg.hasSmoothedTarget = true;
        }
        else
        {
            leg.smoothedTargetFootPosition = Vector3.Lerp(
                leg.smoothedTargetFootPosition,
                targetFootPosition,
                positionT
            );
        }

        SolveLegCCD(
            leg,
            leg.smoothedTargetFootPosition,
            ikWeight
        );

        if (protectKneeBend)
        {
            ClampKneeBend(leg);
        }

        if (limitIKCorrection)
        {
            leg.upperLeg.rotation = Quaternion.RotateTowards(
                upperBefore,
                leg.upperLeg.rotation,
                maxUpperLegCorrectionAngle
            );

            leg.lowerLeg.rotation = Quaternion.RotateTowards(
                lowerBefore,
                leg.lowerLeg.rotation,
                maxLowerLegCorrectionAngle
            );
        }

        // 穿地时优先保证位置，不要让脚掌旋转再次把 solePoint 压进地面。
        float safeFootRotationWeight = isPenetrating
            ? 0f
            : leg.footRotationWeight;

        if (safeFootRotationWeight <= 0.001f)
        {
            return;
        }

        Quaternion targetFootRotation =
            Quaternion.FromToRotation(sole.up, targetGroundNormal) * leg.foot.rotation;

        float rotationT = DampFactor(targetRotationSmoothSpeed, dt);

        leg.smoothedTargetFootRotation = Quaternion.Slerp(
            leg.smoothedTargetFootRotation,
            targetFootRotation,
            rotationT
        );

        Quaternion wantedFootRotation = Quaternion.Slerp(
            leg.foot.rotation,
            leg.smoothedTargetFootRotation,
            safeFootRotationWeight * ikWeight
        );

        if (limitIKCorrection)
        {
            wantedFootRotation = Quaternion.RotateTowards(
                footBefore,
                wantedFootRotation,
                maxFootCorrectionAngle
            );
        }

        leg.foot.rotation = wantedFootRotation;
    }

    private void SolveLegCCD(Leg leg, Vector3 targetFootPosition, float weight)
    {
        int iterationCount = Mathf.Max(1, solverIterations);
        float clampedWeight = Mathf.Clamp01(weight);

        for (int i = 0; i < iterationCount; i++)
        {
            RotateBoneTowardTarget(
                leg.lowerLeg,
                leg.foot,
                targetFootPosition,
                clampedWeight
            );

            RotateBoneTowardTarget(
                leg.upperLeg,
                leg.foot,
                targetFootPosition,
                clampedWeight
            );

            if (useKneeHint && leg.kneeHint != null)
            {
                ApplyKneeHint(leg, clampedWeight * kneeHintWeight);
            }
        }
    }

    private void RotateBoneTowardTarget(
        Transform bone,
        Transform endEffector,
        Vector3 targetPosition,
        float weight)
    {
        Vector3 boneToEnd = endEffector.position - bone.position;
        Vector3 boneToTarget = targetPosition - bone.position;

        if (boneToEnd.sqrMagnitude < 0.000001f)
        {
            return;
        }

        if (boneToTarget.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Quaternion deltaRotation =
            Quaternion.FromToRotation(boneToEnd, boneToTarget);

        Quaternion weightedDelta = Quaternion.Slerp(
            Quaternion.identity,
            deltaRotation,
            Mathf.Clamp01(weight)
        );

        bone.rotation = weightedDelta * bone.rotation;
    }

    private void ApplyKneeHint(Leg leg, float weight)
    {
        Vector3 hipPosition = leg.upperLeg.position;
        Vector3 footPosition = leg.foot.position;
        Vector3 kneePosition = leg.lowerLeg.position;
        Vector3 hintPosition = leg.kneeHint.position;

        Vector3 hipToFoot = footPosition - hipPosition;

        if (hipToFoot.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Vector3 axis = hipToFoot.normalized;

        Vector3 currentKneeDirection = kneePosition - hipPosition;
        Vector3 desiredKneeDirection = hintPosition - hipPosition;

        Vector3 currentProjected =
            Vector3.ProjectOnPlane(currentKneeDirection, axis);

        Vector3 desiredProjected =
            Vector3.ProjectOnPlane(desiredKneeDirection, axis);

        if (currentProjected.sqrMagnitude < 0.000001f ||
            desiredProjected.sqrMagnitude < 0.000001f)
        {
            return;
        }

        float angle = Vector3.SignedAngle(
            currentProjected,
            desiredProjected,
            axis
        );

        Quaternion rotation = Quaternion.AngleAxis(
            angle * Mathf.Clamp01(weight),
            axis
        );

        leg.upperLeg.rotation = rotation * leg.upperLeg.rotation;
    }

    private void ClampKneeBend(Leg leg)
    {
        Vector3 upperToKnee = leg.lowerLeg.position - leg.upperLeg.position;
        Vector3 kneeToFoot = leg.foot.position - leg.lowerLeg.position;

        if (upperToKnee.sqrMagnitude < 0.000001f ||
            kneeToFoot.sqrMagnitude < 0.000001f)
        {
            return;
        }

        float currentBendAngle = Vector3.Angle(upperToKnee, kneeToFoot);

        float targetBendAngle = Mathf.Clamp(
            currentBendAngle,
            minKneeBendAngle,
            maxKneeBendAngle
        );

        if (Mathf.Abs(currentBendAngle - targetBendAngle) < 0.01f)
        {
            return;
        }

        Vector3 bendAxis = Vector3.Cross(upperToKnee, kneeToFoot);

        if (bendAxis.sqrMagnitude < 0.000001f)
        {
            bendAxis = leg.upperLeg.right;
        }

        bendAxis.Normalize();

        float correctionAngle = targetBendAngle - currentBendAngle;

        Quaternion correction =
            Quaternion.AngleAxis(correctionAngle, bendAxis);

        leg.lowerLeg.rotation = correction * leg.lowerLeg.rotation;
    }

    private Transform GetSoleTransform(Leg leg)
    {
        return leg.solePoint != null ? leg.solePoint : leg.foot;
    }

    private bool IsLegValid(Leg leg)
    {
        return leg != null &&
               leg.upperLeg != null &&
               leg.lowerLeg != null &&
               leg.foot != null;
    }

    private float DampFactor(float speed, float dt)
    {
        if (speed <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-speed * dt);
    }

    public bool IsBodyAirborne()
    {
        return bodyAirborne;
    }

    public FootState GetLeftFootState()
    {
        return leftLeg != null ? leftLeg.state : FootState.Swing;
    }

    public FootState GetRightFootState()
    {
        return rightLeg != null ? rightLeg.state : FootState.Swing;
    }
}
