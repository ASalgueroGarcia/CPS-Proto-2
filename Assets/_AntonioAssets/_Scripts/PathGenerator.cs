using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    [Header("Map Variables")] 
    [SerializeField] private float minLayerNodes;
    [SerializeField] private float maxLayerNodes;
    [SerializeField] private float maxLayers;
    
    [Header("Map Layers")] 
    [SerializeField] private Node[][] map;

    private Node _startingNode;
    private Node _currNode;
    private Node _prevNode;
}
