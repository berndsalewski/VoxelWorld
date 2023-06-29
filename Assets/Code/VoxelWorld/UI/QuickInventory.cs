using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VoxelWorld;

public class QuickInventory : MonoBehaviour
{
    private const int MAX_SLOTS = 10;
    [FormerlySerializedAs("blockTypes")]
    public BlockType[] placeableBlocks;
    public Sprite[] blockSprites;
    public ToggleGroup toggleGroup;

    public GameObject blockPrefab;
    public Player player;

    private int _numSlots;
    private Toggle[] _blockToggles;

    private void OnValidate()
    {
        if (placeableBlocks.Length > MAX_SLOTS || blockSprites.Length > MAX_SLOTS)
        {
            Debug.LogError($"Maximum number of slots in QuickInventory is {MAX_SLOTS}");
        }
    }

    private void Awake()
    {
        _numSlots = placeableBlocks.Length;
        _blockToggles = new Toggle[_numSlots];
    }

    void Start()
    {
        for (int i = 0; i < _numSlots; i++)
        {
            GameObject blockGO = Instantiate(blockPrefab);
            Image img = blockGO.GetComponent<Image>();
            img.sprite = blockSprites[i];
            _blockToggles[i] = blockGO.GetComponent<Toggle>();
            _blockToggles[i].group = toggleGroup;
            blockGO.transform.SetParent(transform, false);
        }
        _blockToggles[0].Select();
    }

    void Update()
    {
        for (int i = 0; i < _numSlots; i++)
        {
            // handle input with keycode 48 (0)
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                _blockToggles[i].Select();
                player.SetBuildType(placeableBlocks[MAX_SLOTS - 1]);
                break;
            }

            //check keycodes 49-57 -> 1-9
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                _blockToggles[i].Select();
                player.SetBuildType(placeableBlocks[i]);
                break;
            }
        }
    }
}
