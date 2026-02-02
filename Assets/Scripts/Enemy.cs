using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent m_NavMeshAgent = null;    

    [SerializeField]
    private Transform m_Point1, m_Point2 = null;

    private enum destinationState
    {
        Point1,
        Point2
    }

    [SerializeField]
    private destinationState m_CurrentDestination = destinationState.Point2;

    void Start()
    {   
        if (m_NavMeshAgent == null)
            m_NavMeshAgent = GetComponent<NavMeshAgent>();

       MoveToDestination(m_Point2.position, destinationState.Point2); 
    }

    void Update()
    {
        if (!ReachedDestination()) return;

        if (m_CurrentDestination == destinationState.Point2)
            MoveToDestination(m_Point1.position, destinationState.Point1);
        else
            MoveToDestination(m_Point2.position, destinationState.Point2);
    }

    private bool ReachedDestination()
    {
        if (m_NavMeshAgent.pathPending) 
                return false;
        return m_NavMeshAgent.remainingDistance <= m_NavMeshAgent.stoppingDistance + 0.05f;
    }

    private void MoveToDestination(Vector3 destination, destinationState destinationState)
    {
        m_CurrentDestination = destinationState;
        m_NavMeshAgent.SetDestination(destination);
    }
}
