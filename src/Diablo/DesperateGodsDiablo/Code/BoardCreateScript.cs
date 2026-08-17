using UnityEngine;

public class BoardCreateScript : MonoBehaviour
{
    void Start()
    {
        foreach (Transform tile in transform.FindChild("DisplayTiles"))
        {
            tile.FindChild("default").GetComponent<Renderer>().material = TileManagerScript.Instance().GetMaterial(int.Parse(tile.gameObject.name) - 1);
        }
    }

    void Update()
    {

    }
}
