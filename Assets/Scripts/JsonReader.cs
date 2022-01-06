using System;
using UnityEngine;

public class JsonReader : MonoBehaviour
{
    [SerializeField] private TextAsset dataJson;

    [Serializable]
    public class GameData
    {
        public float brickFallingDelay;
        public float brickWidth;
        public float brickHeight;
        public int createBomb;
        public int createMissile;
        public int maximumShuffles;
        public int isCorruptBricksAllowed;
    }

    [Serializable]
    public class BoomBlocks
	{
        public GameData[] gameData;
	}

    [SerializeField] private BoomBlocks boomBlocks = new BoomBlocks();

    public delegate void DataLoaded(BoomBlocks boomBlocks);
    public event DataLoaded LoadedEvent;

    private void Start()
    {
        InitReader();
    }

    private void InitReader()
	{
        boomBlocks = JsonUtility.FromJson<BoomBlocks>(dataJson.text);
        
        if (LoadedEvent != null)
            LoadedEvent.Invoke(boomBlocks);
	}
}
