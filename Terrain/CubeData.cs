using UnityEngine;

// b3agz on Youtube
public static class CubeData
{
	public static readonly Vector3Int[] relativeAdjacentPositions = new Vector3Int[6] {

		new Vector3Int(0, 0, -1),// South Face
		new Vector3Int(0, 0, 1),// North Face
		new Vector3Int(0, 1, 0),// Top Face
		new Vector3Int(0, -1, 0),// Bottom Face
		new Vector3Int(-1, 0, 0), // West Face
		new Vector3Int(1, 0, 0)// East Face
	};


}