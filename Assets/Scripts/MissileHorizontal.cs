using UnityEngine;

public class MissileHorizontal : MonoBehaviour
{
    public void OnMouseDown()
    { 
        var blockSpawner = GameObject.Find("spawner").GetComponent<BlockSpawner>();
        blockSpawner.GetMissiledBrick(gameObject.transform);
    }
}
