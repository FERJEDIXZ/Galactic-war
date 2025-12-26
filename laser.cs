using UnityEngine;

public class Laser : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad de respaldo si no hay Rigidbody.")]
    public float speed = 1300f;

    [Header("Referencias")]
    [Tooltip("Raíz del objeto que disparó el láser (para ignorar sus colliders).")]
    public Transform ownerRoot;

    [Tooltip("Objetivo principal de este láser (Player o Alien). Puede ser null.")]
    public Transform targetRoot;

    [Header("Colisión")]
    [Tooltip("Distancia máxima al objetivo para considerarlo un impacto.")]
    public float hitRadius = 1.5f;

    [Header("Homing (giro cerca del destino)")]
    [Tooltip("Distancia al punto de destino a partir de la cual el láser empieza a girar hacia él.")]
    public float homingDistance = 15f;

    [Tooltip("Qué tan rápido gira el láser hacia el destino (grados aproximados por segundo).")]
    public float homingTurnSpeed = 8f;

    [Header("Seguridad")]
    [Tooltip("Tiempo máximo de vida del láser, en segundos (por seguridad).")]
    public float maxFlightTime = 5f;
    private float timeAlive = 0f;

    // Posición fija donde estaba el objetivo en el momento del disparo
    private bool hasTargetPosition = false;
    private Vector3 targetPosition;

    // Datos para saber cuándo hemos pasado el punto de destino
    private Vector3 startPosition;
    private Vector3 travelDir;
    private Vector3 pathDir; // dirección fija desde el inicio hasta el destino

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        startPosition = transform.position;

        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
        {
            travelDir = rb.linearVelocity.normalized;
        }
        else
        {
            travelDir = transform.forward;
        }

        // Definir pathDir en base al destino si ya lo tenemos,
        // o en base a la dirección inicial de viaje como respaldo.
        if (hasTargetPosition)
        {
            Vector3 dir = targetPosition - startPosition;
            pathDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : travelDir;
        }
        else
        {
            pathDir = travelDir;
        }
    }

    /// <summary>
    /// Indica una posición fija que el láser debe considerar como "destino".
    /// Cuando la bala sobrepase esa posición a lo largo de su dirección de viaje,
    /// se destruirá aunque el objetivo real ya no esté ahí.
    /// </summary>
    public void useTargetPosition(Vector3 pos)
    {
        hasTargetPosition = true;
        targetPosition = pos;

        // Definimos el punto de partida y la dirección fija hacia el destino
        startPosition = transform.position;
        Vector3 dir = targetPosition - startPosition;
        pathDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
    }

    void Update()
    {
        // Seguridad: destruir el láser si lleva demasiado tiempo en escena
        timeAlive += Time.deltaTime;
        if (timeAlive >= maxFlightTime)
        {
            Destroy(gameObject);
            return;
        }

        // 0) Homing suave cuando estamos cerca del destino fijo
        if (hasTargetPosition)
        {
            Vector3 toDest = targetPosition - transform.position;
            float distToDest = toDest.magnitude;

            if (distToDest <= homingDistance && distToDest > 0.01f)
            {
                Vector3 desiredDir = toDest.normalized;

                // Giramos travelDir suavemente hacia el destino
                travelDir = Vector3.Slerp(travelDir, desiredDir, homingTurnSpeed * Time.deltaTime);
                if (travelDir.sqrMagnitude > 0.0001f)
                    travelDir.Normalize();

                // Si tenemos Rigidbody, actualizamos su velocidad en la nueva dirección
                if (rb != null)
                {
                    float currentSpeed = rb.linearVelocity.magnitude;
                    if (currentSpeed < 0.01f)
                        currentSpeed = speed;

                    rb.linearVelocity = travelDir * currentSpeed;
                }
                else
                {
                    // Sin Rigidbody, orientamos el transform para que coincida con travelDir
                    transform.forward = travelDir;
                }
            }
        }

        // 1) Si no hay Rigidbody, movemos manualmente usando travelDir y speed
        if (rb == null)
        {
            transform.position += travelDir * speed * Time.deltaTime;
        }

        // 2) Si tenemos un targetRoot, destruir al estar suficientemente cerca
        if (targetRoot != null)
        {
            float distToTarget = Vector3.Distance(transform.position, targetRoot.position);
            if (distToTarget <= hitRadius)
            {
                Destroy(gameObject);
                return;
            }
        }

        // 3) Si tenemos una posición de destino fija, destruir cuando la hayamos sobrepasado
        if (hasTargetPosition)
        {
            Vector3 fromStartToTarget = targetPosition - startPosition;
            float totalDistance = fromStartToTarget.magnitude;

            if (totalDistance > 0.001f)
            {
                Vector3 fromStartToLaser = transform.position - startPosition;
                float projected = Vector3.Dot(fromStartToLaser, pathDir);

                // Cuando la proyección sea mayor o igual a la distancia total,
                // significa que ya pasamos el punto de destino.
                if (projected >= totalDistance)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 1) Ignorar cualquier collider que pertenezca al que disparó
        if (ownerRoot != null && other.transform.IsChildOf(ownerRoot))
            return;

        // 2) Si tenemos un targetRoot concreto, solo reaccionar cuando choquemos con él
        if (targetRoot != null)
        {
            if (other.transform.IsChildOf(targetRoot))
            {
                Destroy(gameObject);
            }
            // Ignorar cualquier otro collider (otros players, aliens, etc.)
            return;
        }

        // 3) Si no hay targetRoot (disparo al vacío), podemos destruir al primer impacto
        Destroy(gameObject);
    }
}
