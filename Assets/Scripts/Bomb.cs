using UnityEngine;

public class Bomb : MonoBehaviour
{
    public void OnMouseDown()
    {
        var blockSpawner = GameObject.Find("spawner").GetComponent<BlockSpawner>();
        blockSpawner.GetBombedBrick(gameObject.transform);
    }
}
