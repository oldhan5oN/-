using UnityEngine;
[DefaultExecutionOrder(10000)]
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
        public float maxIKWeight = 0.65f;

        [Range(0f, 1f)]
        public float footRotationWeight = 0.5f;

        [Header("Contact State")]
        public float enterGroundedDistance = 0.06f;
        public float exitGroundedDistance = 0.18f;
        public float maxGroundedFootSpeed = 0.25f;
        public float forceSwingFootSpeed = 0.65f;

        [Header("Weight Smoothing")]
        public float weightUpSpeed = 4f;
        public float weightDownSpeed = 10f;

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
    public Transform hips;

    [Header("Legs")]
    public Leg leftLeg;
    public Leg rightLeg;

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("Raycast")]
    public float rayStartHeight = 0.5f;
    public float rayLength = 1.3f;
    public float footGroundOffset = 0.025f;

    [Header("Jump Detection")]
    public bool enableJumpDetection = true;
    public float jumpFootDistance = 0.20f;
    public float jumpUpSpeed = 0.45f;
    public float landingFootDistance = 0.10f;
    public float landingFootSpeed = 0.45f;

    [Header("IK Solver")]
    public int solverIterations = 3;
    public float targetPositionSmoothSpeed = 8f;
    public float targetRotationSmoothSpeed = 8f;

    [Header("Knee Hint")]
    public bool useKneeHint = true;

    [Range(0f, 1f)]
    public float kneeHintWeight = 0.7f;

    [Header("IK Correction Limit")]
    public bool limitIKCorrection = true;
    public float maxUpperLegCorrectionAngle = 12f;
    public float maxLowerLegCorrectionAngle = 18f;
    public float maxFootCorrectionAngle = 20f;

    [Header("Knee Bend Protection")]
    public bool protectKneeBend = true;

    [Tooltip("0 means almost straight. Increase if knee flips when fully straight.")]
    public float minKneeBendAngle = 2f;

    [Tooltip("Larger means knee can fold more.")]
    public float maxKneeBendAngle = 150f;

    [Header("Debug")]
    public bool drawDebugRay = true;
    public bool drawDebugTarget = true;

    private bool bodyAirborne;
    private Vector3 lastHipsPosition;
    private bool hasLastHipsPosition;
    private float hipsVerticalSpeed;

    private void Reset()
    {
        solverIterations = 3;
        rayStartHeight = 0.5f;
        rayLength = 1.3f;
        footGroundOffset = 0.025f;
    }

    private void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        UpdateHipsSpeed(dt);

        SampleLegGround(leftLeg, dt);
        SampleLegGround(rightLeg, dt);

        UpdateBodyAirborneState();

        UpdateLegStateAndWeight(leftLeg, dt);
        UpdateLegStateAndWeight(rightLeg, dt);

        ApplyLegIK(leftLeg, dt);
        ApplyLegIK(rightLeg, dt);
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

    private void SampleLegGround(Leg leg, float dt)
    {
        if (!IsLegValid(leg))
        {
            return;
        }

        Transform sole = GetSoleTransform(leg);
        Vector3 solePosition = sole.position;

        if (leg.hasLastSolePosition)
        {
            leg.soleSpeed = (solePosition - leg.lastSolePosition).magnitude / dt;
        }
        else
        {
            leg.soleSpeed = 0f;
            leg.hasLastSolePosition = true;
        }

        leg.lastSolePosition = solePosition;

        Vector3 rayOrigin = solePosition + Vector3.up * rayStartHeight;

        if (drawDebugRay)
        {
            Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);
        }

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
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

        return leg.absHeightToGround < landingFootDistance &&
               leg.soleSpeed < landingFootSpeed &&
               hipsVerticalSpeed <= jumpUpSpeed;
    }

    private void UpdateLegStateAndWeight(Leg leg, float dt)
    {
        if (!IsLegValid(leg) || !leg.enable)
        {
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
            bool canEnterGrounded =
                leg.absHeightToGround <= leg.enterGroundedDistance &&
                leg.soleSpeed <= leg.maxGroundedFootSpeed;

            bool shouldExitGrounded =
                leg.signedHeightToGround > leg.exitGroundedDistance ||
                leg.soleSpeed >= leg.forceSwingFootSpeed;

            if (leg.state == FootState.Grounded)
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

    private void ApplyLegIK(Leg leg, float dt)
    {
        if (!IsLegValid(leg) || !leg.enable)
        {
            return;
        }

        if (!leg.hasGroundHit)
        {
            return;
        }

        if (leg.currentIKWeight <= 0.001f)
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
            leg.currentIKWeight
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
            leg.footRotationWeight * leg.currentIKWeight
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

        for (int i = 0; i < iterationCount; i++)
        {
            RotateBoneTowardTarget(
                leg.lowerLeg,
                leg.foot,
                targetFootPosition,
                weight
            );

            RotateBoneTowardTarget(
                leg.upperLeg,
                leg.foot,
                targetFootPosition,
                weight
            );

            if (useKneeHint && leg.kneeHint != null)
            {
                ApplyKneeHint(leg, weight * kneeHintWeight);
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