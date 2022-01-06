using UnityEngine;

public class Brick : MonoBehaviour
{
    private void OnMouseDown()
    {
        var blockSpawner = GameObject.Find("spawner").GetComponent<BlockSpawner>();
        blockSpawner.GetClickedBrick(gameObject.transform);
    }
}
