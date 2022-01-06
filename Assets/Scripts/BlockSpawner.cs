using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BlockSpawner : MonoBehaviour
{
    [HideInInspector]
    public JsonReader jsonReader;
    
    [SerializeField] private GameObject[] blocks;
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private GameObject shuffleText;
    
    private const int Width = 8;
    private const int Height = 5;
    private const int Undefined = -1;
    private const int NumberOfColoredBricks = 4;
    private const int CorruptBrickCreate = 4;
    
    private float _brickFallingDelay;
    private float _brickHeight;
    private float _brickWidth;
    private int _corruptBrickNum;
    private int _move;
    private int _score;
    private int _blockCounter;
    private int _shuffles;
    private int _maximumShuffles;
    private int _createBomb;
    private static int _createMissile;
    
    private bool _gameOver;
    private bool _crRunning;
    private bool _elementsAreCreated;
    private bool _changeHappen;
    private bool _isShufflingAllowed;
    private bool _isCorruptBlocksAllowed;
    
    private static readonly Transform[,] Grid = new Transform[Width, Height];
    private IEnumerator _coroutine;
    private List<string> _colorNames;
    private List<Point> _visitedBomb = new List<Point>();
    
    private void OnEnable()
    {
        InitSpawner();
    }
    
    private void InitSpawner()
    {
        if (jsonReader == null)
            jsonReader = FindObjectOfType<JsonReader>();

        jsonReader.LoadedEvent += GetDataFromJson;
        
        _colorNames = new List<string>
        {
            "blue",
            "pink",
            "green",
            "yellow"
        };

        _score = 0;
        _corruptBrickNum = 6;
    }
    
    private void GetDataFromJson(JsonReader.BoomBlocks boomBlocks)
    {
        _brickFallingDelay = boomBlocks.gameData[0].brickFallingDelay;
        _brickWidth = boomBlocks.gameData[0].brickWidth;
        _brickHeight = boomBlocks.gameData[0].brickHeight;
        _createBomb = boomBlocks.gameData[0].createBomb;
        _createMissile = boomBlocks.gameData[0].createMissile;
        _maximumShuffles = boomBlocks.gameData[0].maximumShuffles;
        _isCorruptBlocksAllowed = Convert.ToBoolean(boomBlocks.gameData[0].isCorruptBricksAllowed);
        
        FillContainer();
    }
    
    private void FillContainer()
    {
        for (var i = 0; i < Width; i++)
        {
            for (var j = 0; j < Height; j++)
            {
                var position = transform.position + new Vector3(i * _brickWidth, j * _brickHeight, 0);
                var newBlock = Instantiate(blocks[Random.Range(0, NumberOfColoredBricks)], position, Quaternion.identity);
                newBlock.transform.SetParent(gameObject.transform, true);
                newBlock.name = _blockCounter++.ToString();
                Grid[i, j] = newBlock.transform;
            }
        }
    }

    private void Update()
    {
        UpdateGridState();
    }
    
    private void UpdateGridState()
    {
        var dictionary = new Dictionary<int, int>();
        var gridFull = true;
        
        if (!_crRunning)
            gridFull = CheckGrid(dictionary);

        if (!gridFull && !_crRunning)
        {
            _elementsAreCreated = false;
            _visitedBomb = new List<Point>();
            _crRunning = true;
            
            _coroutine = FallElementsDown(dictionary);
            StartCoroutine(_coroutine);
            
            _move++;
            _changeHappen = true;
        }
        
        if (_move% CorruptBrickCreate == 0 && _elementsAreCreated)
        {
            _elementsAreCreated = false;
            
            if (_isCorruptBlocksAllowed)
                MakeRandomBricksCorrupted();
        }

        if (!_crRunning && _changeHappen)
        {
            _isShufflingAllowed = !CheckAvailableMove();
            _changeHappen = false;

            if (!_gameOver)
            {
                if (_isShufflingAllowed)
                {
                    StartCoroutine(ShuffleGrid());

                    _isShufflingAllowed = false;    
                }
            }
        }
        
        if (_gameOver) 
            RestartGame();
    }

    private IEnumerator ShuffleGrid()
    {
        _shuffles++;
        if (_shuffles > _maximumShuffles)
            EndGame();
        else
        {
            shuffleText.SetActive(true);
        
            yield return new WaitForSeconds(3f);
        
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    Destroy(Grid[i, j].gameObject);
                }
            }

            FillContainer();
        
            shuffleText.SetActive(false);    
        }
    }

    private static void RestartGame()
    {
        if (Input.GetMouseButtonDown(0))
            SceneManager.LoadScene("Game");
    }
    
    private static bool CheckGrid(IDictionary<int, int> dictionary)
    {
        var counter = 0;
        for (var i = 0; i < Width; i++)
        {
            for (var j = 0; j < Height; j++)
            {
                if (!ReferenceEquals(Grid[i, j], null)) 
                    continue;
                
                counter++;
                if (dictionary.ContainsKey(i))
                    dictionary[i] += 1;
                else
                    dictionary.Add(i, 1);
            }
        }
        
        return counter < 2;
    }
    
    private IEnumerator FallElementsDown(IDictionary<int, int> dictionary)
    {
        while (true)
        {
            var stillFalling = true;
            var allFilled = !dictionary.Where((t, i) => dictionary.ElementAt(i).Value != 0).Any();

            if (allFilled)
            {
                _elementsAreCreated = true;
                break;
            }

            while (stillFalling)
            {
                yield return new WaitForSeconds(_brickFallingDelay);
                stillFalling = false;
                
                for (var x = 0; x < Width; x++)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        if (!ReferenceEquals(Grid[x, y], null)) 
                            continue;
                        
                        for (var indexY = y + 1; indexY < Height; indexY++)
                        {
                            if (ReferenceEquals(Grid[x, indexY], null)) 
                                continue;
                            
                            stillFalling = true;
                            Grid[x, indexY - 1] = Grid[x, indexY];
                            Vector2 vector = Grid[x, indexY - 1].transform.position;
                            vector.y -= _brickHeight;
                            Grid[x, indexY - 1].transform.position = vector;
                            Grid[x, indexY] = null;
                        }
                    }
                }
            }

            BringNewBricks(dictionary);
        }
        
        _crRunning = false;
    }
    
    public void GetClickedBrick(Transform clickedTransform)
    {
        if (!_crRunning && !_gameOver && !_isShufflingAllowed)
            FindAndDeleteElements(clickedTransform);
    }
    
    private bool NoColorFulLeft()
    {
        for (var i = 0; i < Width; i++)
        {
            for (var j = 0; j < Height; j++)
            {
                if (_colorNames.Contains(Grid[i, j].transform.gameObject.tag))
                    return false;
            }
        }
            
        return true;
    }
    
    private void MakeRandomBricksCorrupted()
    {
        var corruptedSelected = 0;
        while(corruptedSelected < _corruptBrickNum)
        {
            if (NoColorFulLeft())
            {
                _isShufflingAllowed = true;
                break;
            }

            var randomHeight = Random.Range(0, Height);
            var randomWidth = Random.Range(0, Width);
            
            if (!_colorNames.Contains(Grid[randomWidth, randomHeight].gameObject.tag)) 
                continue;
            
            corruptedSelected++;
            
            var vector = Grid[randomWidth, randomHeight].position;
            Destroy(Grid[randomWidth, randomHeight].gameObject);
            
            var corruptedObject = Instantiate(blocks[7], vector, Quaternion.identity);
            corruptedObject.transform.SetParent(gameObject.transform, true);
            corruptedObject.name = "corrupted" + _blockCounter;
            
            _blockCounter++;
            Grid[randomWidth, randomHeight] = corruptedObject.transform;
        }
        
        if (_corruptBrickNum < 20)
            _corruptBrickNum++;
    }
    
    private static bool CheckAvailableMove()
    {
        for (var i = 0; i < Width; i++)
        {
            for (var j = 0; j < Height; j++)
            {
                var clickedColor = Grid[i, j];
                
                if (clickedColor.CompareTag("bomb") || clickedColor.CompareTag("missile") || clickedColor.CompareTag("upmissile"))
                    return true;
                
                if (clickedColor.CompareTag("corrupted"))
                    continue;
                
                var elementsToBeTraversed = new List<Point>();
                var elementsToBeDeleted = new List<Point>();
                
                elementsToBeTraversed.Add(new Point(i, j));
                elementsToBeDeleted.Add(new Point(i, j));

                var missingBricksAtColumns = new Dictionary<int, int> {{i, 1}};

                TraverseNew(elementsToBeTraversed, clickedColor.tag, elementsToBeDeleted, missingBricksAtColumns);

                if (elementsToBeDeleted.Count > 1) 
                    return true;
            }
        }
        
        return false;
    }
    
    private void FindAndDeleteElements(Transform clickedObject)
    {
        int clickedBlockX = Undefined, clickedBlockY = Undefined;

        GetClickedGrid(ref clickedBlockX, ref clickedBlockY, clickedObject);

        if (clickedBlockX == Undefined) 
            return;
        
        var clickedColor = Grid[clickedBlockX, clickedBlockY].tag;
        
        var elementsToBeTraversed = new List<Point>();
        var elementsToBeDeleted = new List<Point>();
        
        elementsToBeTraversed.Add(new Point(clickedBlockX, clickedBlockY));
        elementsToBeDeleted.Add(new Point(clickedBlockX, clickedBlockY));

        var missingBricksAtColumns = new Dictionary<int, int> {{clickedBlockX, 1}};

        TraverseNew(elementsToBeTraversed, clickedColor, elementsToBeDeleted, missingBricksAtColumns);

        if (elementsToBeDeleted.Count < 2)
            return;
            
        AddScore(elementsToBeDeleted.Count);
        DeleteElements(elementsToBeDeleted, ShouldMissileBeCreated(elementsToBeDeleted), missingBricksAtColumns);
        
        AudioPlayer.Instance.PlayAudio(2);
        AudioPlayer.Instance.PlayAudio(3);
    }
    
    private static bool ShouldMissileBeCreated(ICollection elementsToBeDeleted)
    {
        return elementsToBeDeleted.Count > _createMissile;
    }

    private void BringNewBricks(IDictionary<int, int> dictionary)
    {
        var mylist = new List<int>();
        for (var i = 0; i < dictionary.Count; i++)
        {
            var item = dictionary.ElementAt(i);
            if (item.Value == 0) 
                continue;
            
            mylist.Add(item.Key);
            dictionary[item.Key] -= 1;
        }

        CreateColumns(mylist);
    }
    
    private void CreateColumns(IEnumerable<int> mc)
    {
        foreach (var brick in mc)
        {
            Transform transform1;
            UnityEngine.Object newBlock = Instantiate(blocks[Random.Range(0, 4)], new Vector3((transform1 = transform).position.x + brick * _brickWidth, transform1.position.y + (Height-1)*_brickHeight, 0), Quaternion.identity);
            var gameObjectBlock = (GameObject) newBlock;
            gameObjectBlock.transform.SetParent(gameObject.transform, true);
            newBlock.name = "" + _blockCounter++;
            Grid[brick, Height-1] = gameObjectBlock.transform;
        }
    }
    
    private void DeleteElements(IReadOnlyCollection<Point> elementsToBeDeleted, bool createBomb, IDictionary<int, int> dictionary)
    {
        foreach (var point in elementsToBeDeleted)
        {
            if (createBomb)
            {
                createBomb = false;
                
                var pos = Grid[point.GetX(), point.GetY()].gameObject.transform.position;
                Destroy(Grid[point.GetX(), point.GetY()].gameObject);
                var bombObject = elementsToBeDeleted.Count > _createBomb ? Instantiate(blocks[4], pos, Quaternion.identity) : Instantiate(blocks[Random.Range(5,7)], pos, Quaternion.identity);
                bombObject.transform.SetParent(gameObject.transform, true);
                bombObject.name = _blockCounter++.ToString();
                Grid[point.GetX(), point.GetY()] = bombObject.transform;
                dictionary[point.GetX()] -= 1;
            }
            else
            {
                var brickId = Grid[point.GetX(), point.GetY()].gameObject.name;
                var bombOrBrick = GameObject.Find(brickId).GetComponent<BombAndBrick>();
                bombOrBrick.Trigger(point.GetX(), point.GetY());
            }
        }
    }
    
    public static void DeleteFromGrid(int x, int y)
    {
        if (x != -1)
            Grid[x, y] = null;
    }

    private static void GetClickedGrid(ref int x, ref int y, Transform clickedObject)
    {
        for (var i = 0; i < Width; i++)
        {
            for (var j = 0; j < Height; j++)
            {
                if (ReferenceEquals(Grid[i, j], null))
                    continue;

                if (!Grid[i, j].Equals(clickedObject)) 
                    continue;
                
                x = i;
                y = j;
                
                break;
            }
        }
    }
    
    private void AddScore(int count)
    {
        var score = (int) Math.Pow(count, 2);
        _score += score;
        var text = GameObject.Find("score").GetComponent<Text>();
        text.text = "Score: " + _score;
    }

    private static void TraverseNew(IList<Point> elementsToBeTraversed, string color, ICollection<Point> elementsToBeDeleted, IDictionary<int, int> dictionary)
    {
        while (elementsToBeTraversed.Count > 0)
        {
            var curX = elementsToBeTraversed[0].GetX();
            var curY = elementsToBeTraversed[0].GetY();
            
            CheckElement(curX - 1, curY, elementsToBeTraversed, color, elementsToBeDeleted, dictionary);
            CheckElement(curX + 1, curY, elementsToBeTraversed, color, elementsToBeDeleted, dictionary);
            CheckElement(curX, curY + 1, elementsToBeTraversed, color, elementsToBeDeleted, dictionary);
            CheckElement(curX, curY - 1, elementsToBeTraversed, color, elementsToBeDeleted, dictionary);

            elementsToBeTraversed.Remove(elementsToBeTraversed[0]);
        }
    }
    
    private static void CheckElement(int x, int y, ICollection<Point> elementsToBeTraversed, string color, ICollection<Point> elementsToBeDeleted, IDictionary<int, int> dictionary)
    {
        if (x <= -1 || x >= Width || y <= -1 || y >= Height) 
            return;

        if (ReferenceEquals(Grid[x, y], null) || !Grid[x, y].CompareTag(color)) 
            return;
        
        var newCur = new Point(x, y);
        if (elementsToBeDeleted.Contains(newCur) || elementsToBeTraversed.Contains(newCur)) 
            return;
        
        if (dictionary.ContainsKey(newCur.GetX()))
            dictionary[newCur.GetX()] += 1;
        else
            dictionary.Add(newCur.GetX(), 1);

        elementsToBeDeleted.Add(newCur);
        elementsToBeTraversed.Add(newCur);
    }
    
    public void GetBombedBrick(Transform gameObjectTransform)
    {
        if (!_crRunning && !_gameOver && !_isShufflingAllowed)
            BombIt(gameObjectTransform);
    }
    
    public void GetMissiledBrick(Transform gameObjectTransform)
    {
        if (!_crRunning && !_gameOver && !_isShufflingAllowed)
            MissileIt(gameObjectTransform);
    }
    
    public void GetMissiledBrickUpside(Transform gameObjectTransform)
    {
        if (!_crRunning && !_gameOver && !_isShufflingAllowed)
            MissileItUpside(gameObjectTransform);
    }
    
    private void MissileIt(Transform gameObjectTransform)
    {
        int x = Undefined, y = Undefined;
        GetClickedGrid(ref x, ref y, gameObjectTransform);
        
        var elementsToDelete = new List<Point>();

        var dictionary = new Dictionary<int, int>();

        var listBomb = new List<Point> {new Point(x, y)};
        
        FindMissiledElements(listBomb, dictionary, elementsToDelete);

        DeleteElements(elementsToDelete, false, dictionary);
        AddScore(elementsToDelete.Count);
        
        AudioPlayer.Instance.PlayAudio(1);
        AudioPlayer.Instance.PlayAudio(3);
    }

    private void MissileItUpside(Transform gameObjectTransform)
    {
        int x = Undefined, y = Undefined;
        GetClickedGrid(ref x, ref y, gameObjectTransform);
        
        var elementsToDelete = new List<Point>();

        var dictionary = new Dictionary<int, int>();

        var listBomb = new List<Point> {new Point(x, y)};

        FindMissiledElementsUpside(listBomb, dictionary, elementsToDelete);

        DeleteElements(elementsToDelete, false, dictionary);
        AddScore(elementsToDelete.Count);
        
        AudioPlayer.Instance.PlayAudio(1);
        AudioPlayer.Instance.PlayAudio(3);
    }
    
    private void BombIt(Transform gameObjectTransform)
    {
        int x = Undefined, y = Undefined;
        GetClickedGrid(ref x, ref y, gameObjectTransform);
        
        var elementsToDelete = new List<Point>();

        var dictionary = new Dictionary<int, int>();

        var listBomb = new List<Point> {new Point(x, y)};
        
        FindBombedElements(listBomb, dictionary, elementsToDelete);

        DeleteElements(elementsToDelete, false, dictionary);
        AddScore(elementsToDelete.Count);
        
        AudioPlayer.Instance.PlayAudio(0);
        AudioPlayer.Instance.PlayAudio(3);
    }

    private void FindMissiledElements(IReadOnlyList<Point> listBomb, IDictionary<int, int> dictionary, ICollection<Point> elementsToDelete)
    {
        var y = listBomb[0].GetY();
        _visitedBomb.Add(new Point(listBomb[0].GetX(),listBomb[0].GetY()));
        
        for (var i = 0; i < Width; i++)
        {
            if (!_visitedBomb.Contains(new Point(i, y)))
            {
                if (Grid[i, y].transform.gameObject.CompareTag("bomb"))
                {
                    var bombId = Grid[i, y].transform.gameObject.name;
                    var bomb = GameObject.Find(bombId).GetComponent<Bomb>();
                    bomb.OnMouseDown();
                }

                if (Grid[i, y].transform.gameObject.CompareTag("missile"))
                {
                    var bombId = Grid[i, y].transform.gameObject.name;
                    var bomb = GameObject.Find(bombId).GetComponent<MissileHorizontal>();
                    bomb.OnMouseDown();
                }

                if (Grid[i, y].transform.gameObject.CompareTag("upmissile"))
                {
                    var bombId = Grid[i, y].transform.gameObject.name;
                    var bomb = GameObject.Find(bombId).GetComponent<MissileVertical>();
                    bomb.OnMouseDown();
                }
            }

            AddElement(i, y, elementsToDelete, dictionary);
        }
    }
    
    private void FindMissiledElementsUpside(IReadOnlyList<Point> listBomb, IDictionary<int, int> dictionary, ICollection<Point> elementsToDelete)
    {
        _visitedBomb.Add(new Point(listBomb[0].GetX(),listBomb[0].GetY()));
        
        var x = listBomb[0].GetX();
        for (var i = 0; i < Height; i++)
        {
            if (!_visitedBomb.Contains(new Point(x,i)))
            {
                if ((Grid[x, i].transform.gameObject.CompareTag("bomb")))
                {
                    var bombId = Grid[x, i].transform.gameObject.name;
                    var bomb = GameObject.Find(bombId).GetComponent<Bomb>();
                    bomb.OnMouseDown();
                }
                else if (Grid[x, i].transform.gameObject.CompareTag("missile"))
                {
                    var bombId = Grid[x, i].transform.gameObject.name;
                    var bomb = GameObject.Find(bombId).GetComponent<MissileHorizontal>();
                    bomb.OnMouseDown();
                }
                else if (Grid[x, i].transform.gameObject.CompareTag("upmissile"))
                {
                    var bombId = Grid[x, i].transform.gameObject.name;
                    var bomb = GameObject.Find(bombId).GetComponent<MissileVertical>();
                    bomb.OnMouseDown();
                }
            }
            
            AddElement(x, i, elementsToDelete, dictionary);
        }
    }
    
    private void FindBombedElements(IList<Point> listBomb, IDictionary<int, int> dictionary, ICollection<Point> elementsToDelete)
    {
        _visitedBomb.Add(listBomb[0]);
        
        var deletedBombs = new List<Point>();
        while (listBomb.Count > 0)
        {
            for(var i = listBomb[0].GetX()-1; i<= listBomb[0].GetX()+1; i++)
            {
                for (var j = listBomb[0].GetY() - 1; j <= listBomb[0].GetY() + 1; j++)
                {
                    if (i < 0 || i >=Width || j<0 || j>=Height)
                        continue;
                    
                    AddElement(i, j, elementsToDelete, dictionary);
                    
                    var point = new Point(i, j);
                    
                    if (_visitedBomb.Contains(point)) 
                        continue;
                    
                    if (Grid[i, j].transform.gameObject.CompareTag("bomb") && !listBomb.Contains(point) &&
                        !deletedBombs.Contains(point))
                    {
                        var bombId = Grid[i, j].transform.gameObject.name;
                        var bomb = GameObject.Find(bombId).GetComponent<Bomb>();
                        bomb.OnMouseDown();
                        
                        AudioPlayer.Instance.PlayAudio(0);
                        AudioPlayer.Instance.PlayAudio(3);
                    }
                    else if (Grid[i, j].transform.gameObject.CompareTag("missile"))
                    {
                        var bombId = Grid[i, j].transform.gameObject.name;
                        var bomb = GameObject.Find(bombId).GetComponent<MissileHorizontal>();
                        bomb.OnMouseDown();
                        
                        AudioPlayer.Instance.PlayAudio(1);
                        AudioPlayer.Instance.PlayAudio(3);
                    }
                    else if (Grid[i, j].transform.gameObject.CompareTag("upmissile"))
                    {
                        var bombId = Grid[i, j].transform.gameObject.name;
                        var bomb = GameObject.Find(bombId).GetComponent<MissileVertical>();
                        bomb.OnMouseDown();
                        
                        AudioPlayer.Instance.PlayAudio(1);
                        AudioPlayer.Instance.PlayAudio(3);
                    }
                } 
            }

            if (listBomb.Count <= 0) 
                continue;
            
            var deletePoint = listBomb[0];
            listBomb.RemoveAt(0);
            deletedBombs.Add(deletePoint);
        }
    }
    
    private static void AddElement(int x, int y, ICollection<Point> deleteList, IDictionary<int, int> dictionary)
    {
        if (x <= -1 || x >= Width || y <= -1 || y >= Height) 
            return;
        
        var toAdd = new Point(x,y);
        
        if (ReferenceEquals(Grid[x, y], null) || deleteList.Contains(toAdd)) 
            return;
            
        deleteList.Add(toAdd);
        if (dictionary.ContainsKey(x))
            dictionary[x] += 1;
        else
            dictionary.Add(x, 1);
    }
    
    private void EndGame()
    {
        gameOverText.SetActive(true);
        _gameOver = true;
    }
}

internal readonly struct Point
{
    private readonly int _x;
    private readonly int _y;

    public Point(int x, int y)
    {
        _x = x;
        _y = y;
    }
    public int GetX()
    {
        return _x;
    }
    public int GetY()
    {
        return _y;
    }
}