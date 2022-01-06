
using UnityEngine;

public class MissileVertical : MonoBehaviour
{
    public void OnMouseDown()
    {
        var blockSpawner = GameObject.Find("spawner").GetComponent<BlockSpawner>();
        blockSpawner.GetMissiledBrickUpside(gameObject.transform);
    }
}