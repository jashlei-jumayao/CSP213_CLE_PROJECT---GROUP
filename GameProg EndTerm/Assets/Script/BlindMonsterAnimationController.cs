using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public class BlindMonsterAnimationController : MonoBehaviour
{
    private Animator anim;
    private NavMeshAgent agent;

    [Header("Animation Settings")]
    [Tooltip("Smooths out the transition between walk and run")]
    public float speedDampTime = 0.1f;

    private void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // Get the actual physical speed of the NavMeshAgent
        float currentSpeed = agent.velocity.magnitude;

        // Pass the speed to the Animator's "Speed" parameter.
        // The damp time prevents the animation from snapping instantly.
        anim.SetFloat("Speed", currentSpeed, speedDampTime, Time.deltaTime);
    }

    // --- Call this from your Touch/Drain scripts when it catches the player ---
    public void PlayAttackAnimation()
    {
        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }
    }
}