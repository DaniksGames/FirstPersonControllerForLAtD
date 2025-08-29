using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float backwardSpeedMultiplier = 0.6f;
    public float sideSpeedMultiplier = 0.8f;
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    [Header("Landing Effects")]
    public float landingSlowDuration = 0.5f;
    public float landingSlowFactor = 0.5f;
    public float maxLandingStun = 1f;

    [Header("Audio Settings")]
    public AudioClip[] footstepSounds;
    public AudioClip jumpSound;
    public AudioClip landingSound;
    public AudioClip wallHitSound;
    public float footstepInterval = 0.4f;

    [Header("Visual Effects")]
    public Camera playerCamera;
    public float stunEffectIntensity = 10f;
    public float stunEffectDuration = 0.3f;

    private Rigidbody rb;
    private bool isGrounded;
    private bool isRunning;
    private float currentSpeed;
    private float footstepTimer;
    private float originalDrag;
    private bool wasGrounded;
    private float fallHeight;
    private Vector3 lastGroundPosition;

    private AudioSource audioSource;
    private bool isStunned;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        originalDrag = rb.linearDamping;

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        CheckGrounded();
        HandleInput();
        HandleJump();
        HandleFootsteps();
        HandleLandingEffects();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit,
                    groundCheckDistance, groundLayer);

        if (!wasGrounded && isGrounded)
        {
            // Calculate fall height
            fallHeight = Mathf.Abs(lastGroundPosition.y - transform.position.y);
            HandleLanding(fallHeight);
        }
        else if (isGrounded)
        {
            lastGroundPosition = transform.position;
        }
        else if (!isGrounded && wasGrounded)
        {
            lastGroundPosition = transform.position;
        }
    }

    void HandleInput()
    {
        // Check for running
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        currentSpeed = isRunning ? runSpeed : walkSpeed;
    }

    void HandleMovement()
    {
        if (isStunned) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            // Calculate movement direction with weighted speeds
            Vector3 movement = CalculateMovementDirection(horizontal, vertical);

            // Apply movement
            Vector3 targetVelocity = movement * currentSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
        else
        {
            // Apply friction when not moving
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    Vector3 CalculateMovementDirection(float horizontal, float vertical)
    {
        Vector3 direction = Vector3.zero;

        // Forward/backward movement
        if (vertical > 0)
            direction += transform.forward;
        else if (vertical < 0)
            direction += transform.forward * backwardSpeedMultiplier;

        // Side movement
        if (horizontal != 0)
            direction += transform.right * horizontal * sideSpeedMultiplier;

        // Normalize only if there's actual movement
        if (direction != Vector3.zero)
            direction.Normalize();

        return direction;
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isStunned)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            PlaySound(jumpSound);
        }
    }

    void HandleFootsteps()
    {
        if (isGrounded && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) && !isStunned)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0)
            {
                PlayRandomFootstep();
                footstepTimer = footstepInterval / (isRunning ? 1.5f : 1f);
            }
        }
    }

    void HandleLandingEffects()
    {
        if (!wasGrounded && isGrounded)
        {
            float stunAmount = Mathf.Clamp(fallHeight / 5f, 0f, maxLandingStun);
            if (stunAmount > 0.1f)
            {
                StartCoroutine(ApplyLandingStun(stunAmount));
                StartCoroutine(StunVisualEffect(stunAmount));
            }
        }
    }

    void HandleLanding(float height)
    {
        if (height > 0.5f)
        {
            PlaySound(landingSound);
        }
    }

    void PlayRandomFootstep()
    {
        if (footstepSounds.Length > 0)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            PlaySound(clip);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    IEnumerator ApplyLandingStun(float stunAmount)
    {
        isStunned = true;
        float slowFactor = Mathf.Lerp(0.7f, landingSlowFactor, stunAmount);
        float duration = Mathf.Lerp(0.2f, landingSlowDuration, stunAmount);

        // Apply slow effect
        currentSpeed *= slowFactor;

        yield return new WaitForSeconds(duration);

        // Restore speed
        currentSpeed = isRunning ? runSpeed : walkSpeed;
        isStunned = false;
    }

    IEnumerator StunVisualEffect(float intensity)
    {
        if (playerCamera == null) yield break;

        float elapsed = 0f;
        float duration = stunEffectDuration * intensity;

        while (elapsed < duration)
        {
            float shakeAmount = Mathf.Lerp(stunEffectIntensity, 0f, elapsed / duration) * intensity;

            // Camera shake effect
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            ) * 0.01f;

            playerCamera.transform.localPosition += shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check for wall hits
        if (!isGrounded && collision.relativeVelocity.magnitude > 2f)
        {
            PlaySound(wallHitSound);
        }
    }

    void OnDrawGizmos()
    {
        // Draw ground check ray
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
}