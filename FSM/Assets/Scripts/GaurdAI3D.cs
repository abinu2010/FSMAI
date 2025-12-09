using UnityEngine;
using UnityEngine.AI;

public enum Guard3DState
{
    Patrol,
    Suspicious,
    InvestigateSound,
    InvestigateSmell,
    Chase,
    Attack,
    Search,
    Assist
}

[RequireComponent(typeof(NavMeshAgent))]
public class GuardAI3D : MonoBehaviour
{
    [Header("References")]
    public Transform eyes;
    public Transform[] patrolPoints;
    public GuardStateBillboard3D stateBillboard;

    [Header("Vision")]
    public float sightRange = 18f;
    public float sightAngle = 110f;
    public float visionCloseRange = 2f;
    public bool debugVision = true;

    [Header("Hearing")]
    public float baseSoundSuspicion = 0.4f;
    public float playerSoundMultiplier = 1.3f;
    public float baitSoundMultiplier = 1.0f;
    public float maxSoundSuspicionPerEvent = 1.5f;

    [Header("Smell")]
    public float smellCheckInterval = 0.5f;
    public float smellCheckRadius = 6f;
    public float smellSuspicionThreshold = 0.25f;
    public float smellAlertThreshold = 0.8f;
    public float smellSuspicionMultiplier = 1.0f;
    public bool debugSmell = false;

    [Header("Suspicion and search")]
    public float suspicionDecayRate = 0.25f;
    public float suspiciousTime = 4f;
    public float searchDuration = 6f;
    public float lostSightToSearchTime = 2f;

    [Header("Chase and attack")]
    public float attackRange = 2f;
    public float attackInterval = 1.2f;
    public float assistRadius = 20f;

    [Header("Movement")]
    public float patrolWaitTime = 1f;

    NavMeshAgent agent;
    Transform player;

    Guard3DState currentState = Guard3DState.Patrol;
    float stateTimer;
    float smellCheckTimer;
    float attackTimer;
    float currentSuspicion;

    int patrolIndex;
    Vector3 investigateTarget;
    bool hasInvestigateTarget;
    Vector3 lastKnownPlayerPos;
    bool hasDirectEvidence;

    public Guard3DState CurrentState
    {
        get { return currentState; }
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        Debug.Log("[GuardAI3D] Guard Awake on " + name);
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[GuardAI3D] No Player tagged object found");
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolIndex = 0;
            GoToNextPatrolPoint();
        }

        SetState(Guard3DState.Patrol);
    }

    void OnEnable()
    {
        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.OnSoundHeard += OnSoundHeard;
            Debug.Log("[GuardAI3D] " + name + " subscribed to SoundBus");
        }
        else
        {
            Debug.LogWarning("[GuardAI3D] SoundBus3D.Instance is null!");
        }

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.OnAlertRaised += OnAlertReceived;
            Debug.Log("[GuardAI3D] " + name + " subscribed to AlertBus");
        }
        else
        {
            Debug.LogWarning("[GuardAI3D] AlertBus3D.Instance is null!");
        }
    }

    void OnDisable()
    {
        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.OnSoundHeard -= OnSoundHeard;
        }

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.OnAlertRaised -= OnAlertReceived;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        attackTimer -= dt;
        stateTimer += dt;
        smellCheckTimer += dt;

        if (currentState == Guard3DState.Patrol || currentState == Guard3DState.Suspicious)
        {
            currentSuspicion = Mathf.Max(0f, currentSuspicion - dt * suspicionDecayRate);
        }

        if (player != null)
        {
            if (CanSeePlayer())
            {
                lastKnownPlayerPos = player.position;
                hasDirectEvidence = true;
                currentSuspicion = Mathf.Max(currentSuspicion, 2f);

                float distance = Vector3.Distance(transform.position, player.position);

                if (distance <= attackRange)
                {
                    SetState(Guard3DState.Attack);
                }
                else
                {
                    SetState(Guard3DState.Chase);
                }
            }
        }

        if (smellCheckTimer >= smellCheckInterval)
        {
            smellCheckTimer = 0f;
            CheckSmell();
        }

        switch (currentState)
        {
            case Guard3DState.Patrol:
                UpdatePatrol();
                break;
            case Guard3DState.Suspicious:
                UpdateSuspicious();
                break;
            case Guard3DState.InvestigateSound:
                UpdateInvestigateSound();
                break;
            case Guard3DState.InvestigateSmell:
                UpdateInvestigateSmell();
                break;
            case Guard3DState.Chase:
                UpdateChase();
                break;
            case Guard3DState.Attack:
                UpdateAttack();
                break;
            case Guard3DState.Search:
                UpdateSearch();
                break;
            case Guard3DState.Assist:
                UpdateAssist();
                break;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = eyes != null ? eyes.position : transform.position + Vector3.up * 1.7f;
        Vector3 toPlayer = (player.position + Vector3.up * 1.0f) - origin;
        float distance = toPlayer.magnitude;

        if (distance < 0.01f) return false;

        if (distance <= visionCloseRange)
        {
            if (debugVision)
            {
                Debug.Log("[GuardAI3D] " + name + " sees player by close range");
            }
            return true;
        }

        if (distance > sightRange)
        {
            return false;
        }

        Vector3 dir = toPlayer.normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        if (angle > sightAngle * 0.5f)
        {
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, distance + 0.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player"))
                {
                    if (debugVision)
                    {
                        Debug.Log("[GuardAI3D] " + name + " sees player at " + distance.ToString("F1") + " m");
                    }
                    return true;
                }
            }
        }

        return false;
    }

    void SetState(Guard3DState newState)
    {
        if (currentState == newState) return;

        Guard3DState previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        if (stateBillboard != null)
        {
            stateBillboard.SetText(newState.ToString());
        }

        Debug.Log("[GuardAI3D] " + name + " state " + previousState + " to " + newState + " suspicion " + currentSuspicion.ToString("F2"));

        if (agent != null)
        {
            agent.isStopped = (newState == Guard3DState.Attack || newState == Guard3DState.Suspicious);
        }

        if (newState == Guard3DState.Chase && hasDirectEvidence && AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.RaiseAlert(lastKnownPlayerPos, AlertLevel.High, gameObject);
        }
        else if (newState == Guard3DState.Attack && hasDirectEvidence && AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.RaiseAlert(lastKnownPlayerPos, AlertLevel.Critical, gameObject);
        }

        if (newState == Guard3DState.Patrol)
        {
            hasInvestigateTarget = false;
        }
    }

    void GoToNextPatrolPoint()
    {
        if (agent == null) return;
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        agent.destination = patrolPoints[patrolIndex].position;
        Debug.Log("[GuardAI3D] " + name + " patrol to " + agent.destination);

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    void UpdatePatrol()
    {
        if (agent == null) return;
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (stateTimer >= patrolWaitTime)
            {
                GoToNextPatrolPoint();
            }
        }
    }

    void UpdateSuspicious()
    {
        currentSuspicion = Mathf.Max(0f, currentSuspicion - Time.deltaTime * suspicionDecayRate);

        if (stateTimer >= suspiciousTime)
        {
            if (currentSuspicion >= 1.5f)
            {
                SetState(Guard3DState.Search);
            }
            else
            {
                SetState(Guard3DState.Patrol);
            }
        }
    }

    void UpdateInvestigateSound()
    {
        if (!hasInvestigateTarget || agent == null)
        {
            SetState(Guard3DState.Patrol);
            return;
        }

        agent.destination = investigateTarget;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            Debug.Log("[GuardAI3D] " + name + " reached sound location");
            hasInvestigateTarget = false;

            if (currentSuspicion >= 1.2f)
            {
                SetState(Guard3DState.Search);
            }
            else
            {
                SetState(Guard3DState.Suspicious);
            }
        }
    }

    void UpdateInvestigateSmell()
    {
        if (!hasInvestigateTarget || agent == null)
        {
            SetState(Guard3DState.Patrol);
            return;
        }

        agent.destination = investigateTarget;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            Debug.Log("[GuardAI3D] " + name + " reached smell location");
            hasInvestigateTarget = false;

            if (currentSuspicion >= smellAlertThreshold)
            {
                SetState(Guard3DState.Search);
            }
            else
            {
                SetState(Guard3DState.Suspicious);
            }
        }
    }

    void UpdateChase()
    {
        if (player == null || agent == null)
        {
            SetState(Guard3DState.Patrol);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        agent.destination = player.position;

        if (distance <= attackRange)
        {
            SetState(Guard3DState.Attack);
            return;
        }

        if (!CanSeePlayer())
        {
            if (stateTimer >= lostSightToSearchTime)
            {
                SetState(Guard3DState.Search);
            }
        }
    }

    void UpdateAttack()
    {
        if (player == null)
        {
            SetState(Guard3DState.Patrol);
            return;
        }

        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookPos - transform.position), Time.deltaTime * 10f);

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange * 1.2f)
        {
            SetState(Guard3DState.Chase);
            return;
        }

        if (!CanSeePlayer())
        {
            if (stateTimer >= lostSightToSearchTime)
            {
                SetState(Guard3DState.Search);
            }
            return;
        }

        if (attackTimer <= 0f)
        {
            Debug.Log("[GuardAI3D] " + name + " attacks player");
            attackTimer = attackInterval;
        }
    }

    void UpdateSearch()
    {
        if (agent == null)
        {
            SetState(Guard3DState.Patrol);
            return;
        }

        if (stateTimer < searchDuration * 0.5f)
        {
            agent.destination = lastKnownPlayerPos;
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                SetState(Guard3DState.Patrol);
            }
        }

        if (stateTimer >= searchDuration)
        {
            SetState(Guard3DState.Patrol);
        }
    }

    void UpdateAssist()
    {
        if (agent == null)
        {
            SetState(Guard3DState.Patrol);
            return;
        }

        agent.destination = lastKnownPlayerPos;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            SetState(Guard3DState.Search);
        }
    }

    void OnSoundHeard(Vector3 position, float radius, float intensity, GameObject source)
    {
        if (!isActiveAndEnabled) return;

        float distance = Vector3.Distance(transform.position, position);
        if (distance > radius) return;

        bool fromPlayer = source != null && source.CompareTag("Player");
        float urgency = Mathf.Clamp01((radius - distance) / radius);
        float suspicionGain = baseSoundSuspicion * urgency * intensity;

        if (fromPlayer)
        {
            suspicionGain *= playerSoundMultiplier;
        }
        else
        {
            suspicionGain *= baitSoundMultiplier;
        }

        suspicionGain = Mathf.Min(suspicionGain, maxSoundSuspicionPerEvent);
        currentSuspicion += suspicionGain;

        Debug.Log("[GuardAI3D] " + name + " heard sound at " + position + " dist " + distance.ToString("F1") + " urgency " + urgency.ToString("F2") + " player=" + fromPlayer + " suspicionGain " + suspicionGain.ToString("F2") + " total " + currentSuspicion.ToString("F2"));

        investigateTarget = position;
        hasInvestigateTarget = true;

        if (currentState == Guard3DState.Patrol || currentState == Guard3DState.Suspicious || currentState == Guard3DState.Search)
        {
            SetState(Guard3DState.InvestigateSound);
        }
    }

    void OnAlertReceived(Vector3 position, AlertLevel level, GameObject sourceGuard)
    {
        if (!isActiveAndEnabled) return;
        if (sourceGuard == gameObject) return;

        float dist = Vector3.Distance(transform.position, position);
        if (dist > assistRadius) return;

        string guardName = sourceGuard != null ? sourceGuard.name : "unknown";
        Debug.Log("[GuardAI3D] " + name + " received alert " + level + " from " + guardName + " at distance " + dist.ToString("F1"));

        lastKnownPlayerPos = position;

        if (level == AlertLevel.High || level == AlertLevel.Critical)
        {
            SetState(Guard3DState.Assist);
        }
        else if (level == AlertLevel.Medium)
        {
            if (currentState == Guard3DState.Patrol)
            {
                SetState(Guard3DState.Suspicious);
            }
        }
    }

    void CheckSmell()
    {
        if (ScentNode3D.AllNodes == null || ScentNode3D.AllNodes.Count == 0) return;

        float bestScore = 0f;
        ScentNode3D bestNode = null;

        foreach (var node in ScentNode3D.AllNodes)
        {
            if (node == null) continue;

            float dist = Vector3.Distance(transform.position, node.transform.position);
            if (dist > smellCheckRadius) continue;

            float score = node.GetStrengthAtPosition(transform.position) * smellSuspicionMultiplier;
            if (score > bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        if (bestNode == null || bestScore <= 0.001f) return;

        currentSuspicion += bestScore;

        if (debugSmell)
        {
            Debug.Log("[GuardAI3D] " + name + " smell score " + bestScore.ToString("F2") + " at node " + bestNode.name + " suspicion " + currentSuspicion.ToString("F2"));
        }

        if (currentSuspicion >= smellSuspicionThreshold && (currentState == Guard3DState.Patrol || currentState == Guard3DState.Suspicious))
        {
            investigateTarget = bestNode.transform.position;
            hasInvestigateTarget = true;
            SetState(Guard3DState.InvestigateSmell);
        }

        if (currentSuspicion >= smellAlertThreshold)
        {
            hasDirectEvidence = true;
            lastKnownPlayerPos = bestNode.transform.position;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, smellCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 leftBoundary = Quaternion.Euler(0, -sightAngle * 0.5f, 0) * transform.forward * sightRange;
        Vector3 rightBoundary = Quaternion.Euler(0, sightAngle * 0.5f, 0) * transform.forward * sightRange;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}