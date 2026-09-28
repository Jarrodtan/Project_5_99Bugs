using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Adapts a <see cref="NavMeshAgent"/> as an <see cref="IIslandBoundable"/> for logical containment.
/// Keeps NavMesh types out of <see cref="IslandContainment"/> and <see cref="IslandManager"/>.
/// </summary>
public class NavMeshAgentBoundable : MonoBehaviour, IIslandBoundable
{
    [Tooltip("Agent whose transform is clamped. When empty, uses NavMeshAgent on this object.")]
    [SerializeField]
    NavMeshAgent agent;

    /// <inheritdoc />
    public Transform BoundTransform
    {
        get
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
            return agent != null ? agent.transform : transform;
        }
    }
}
