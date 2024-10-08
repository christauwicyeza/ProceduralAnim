using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProceduralAnimation : MonoBehaviour
{
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform leftFootTargetRig;
    public Transform rightFootTargetRig;

    public float smoothness = 2f;
    public float stepHeight = 0.2f;
    public float stepLength = 1f;
    public float velocityMultiplier = 80f;
    public float bounceAmplitude = 0.05f;
    public float movementSpeed = 5f;
    public float bodyTurnSpeed = 10f;
    public string obstacleTag = "Obstacle";
    public float obstacleDetectionRange = 1.5f;
    public float turnForce = 300f; // Force applied during turning

    private Vector3 initLeftFootPos;
    private Vector3 initRightFootPos;

    private Vector3 lastLeftFootPos;
    private Vector3 lastRightFootPos;

    private Vector3 lastBodyPos;
    private Vector3 initBodyPos;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private bool avoidingObstacle = false;
    private Vector3 avoidDirection = Vector3.zero;
    private bool shouldTurnRight = false;
    private float turnCooldownTimer = 0f;
    private float turnResetDelay = 0.2f;

    private Rigidbody _rigidbody;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();

        initLeftFootPos = leftFootTarget.localPosition;
        initRightFootPos = rightFootTarget.localPosition;

        lastLeftFootPos = leftFootTarget.position;
        lastRightFootPos = rightFootTarget.position;

        lastBodyPos = transform.position;
        initBodyPos = transform.localPosition;
    }

    void FixedUpdate()
    {
        Vector3 movement = Vector3.zero;

        if (!avoidingObstacle)
        {
            movement = ProceduralStep() * movementSpeed * Time.deltaTime;
        }
        else
        {
            movement = avoidDirection * movementSpeed * Time.deltaTime;
        }

        movement = -movement;

        _rigidbody.MovePosition(transform.position + movement);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, obstacleDetectionRange))
        {
            if (hit.collider.CompareTag(obstacleTag))
            {
                avoidingObstacle = true;
                shouldTurnRight = true;
                avoidDirection = transform.right;
                turnCooldownTimer = 0f;
            }
        }

        if (shouldTurnRight)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0, 90, 0), Time.deltaTime * bodyTurnSpeed);

            // Apply force to the right during turning to simulate a drift or forceful turn
            _rigidbody.AddForce(transform.right * turnForce);

            turnCooldownTimer += Time.deltaTime;

            if (turnCooldownTimer > turnResetDelay)
            {
                shouldTurnRight = false;
                avoidingObstacle = false;
                avoidDirection = Vector3.zero;
            }
        }

        lastBodyPos = transform.position;
    }

    private Vector3 ProceduralStep()
    {
        velocity = transform.position - lastBodyPos;
        velocity *= velocityMultiplier;
        velocity = (velocity + smoothness * lastVelocity) / (smoothness + 1f);

        if (velocity.magnitude < 0.000025f * velocityMultiplier)
            velocity = lastVelocity;
        lastVelocity = velocity;

        int sign = (Vector3.Dot(velocity.normalized, transform.forward) < 0 ? -1 : 1);

        Vector3 desiredPositionLeft = leftFootTarget.position;
        Vector3 desiredPositionRight = rightFootTarget.position;

        Vector3[] posNormLeft = CastOnSurface(desiredPositionLeft, 2f, Vector3.up);
        if (posNormLeft[0].y > desiredPositionLeft.y)
        {
            leftFootTargetRig.position = posNormLeft[0];
        }
        else
        {
            leftFootTargetRig.position = desiredPositionLeft;
        }
        if (posNormLeft[1] != Vector3.zero)
        {
            leftFootTargetRig.rotation = Quaternion.LookRotation(sign * velocity.normalized, posNormLeft[1]);
        }

        Vector3[] posNormRight = CastOnSurface(desiredPositionRight, 2f, Vector3.up);
        if (posNormRight[0].y > desiredPositionRight.y)
        {
            rightFootTargetRig.position = posNormRight[0];
        }
        else
        {
            rightFootTargetRig.position = desiredPositionRight;
        }
        if (posNormRight[1] != Vector3.zero)
        {
            rightFootTargetRig.rotation = Quaternion.LookRotation(sign * velocity.normalized, posNormRight[1]);
        }

        lastLeftFootPos = leftFootTargetRig.position;
        lastRightFootPos = rightFootTargetRig.position;
        float feetDistance = Mathf.Clamp01(Mathf.Abs(leftFootTargetRig.localPosition.z - rightFootTargetRig.localPosition.z) / (stepLength / 4f));

        Vector3 bodyMovement = (leftFootTargetRig.position + rightFootTargetRig.position) / 2f;

        return bodyMovement - lastBodyPos;
    }

    static Vector3[] CastOnSurface(Vector3 point, float halfRange, Vector3 up)
    {
        Vector3[] res = new Vector3[2];
        RaycastHit hit;
        Ray ray = new Ray(new Vector3(point.x, point.y + halfRange, point.z), -up);

        if (Physics.Raycast(ray, out hit, 2f * halfRange))
        {
            res[0] = hit.point;
            res[1] = hit.normal;
        }
        else
        {
            res[0] = point;
        }
        return res;
    }

}
