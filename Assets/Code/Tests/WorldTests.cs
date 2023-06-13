using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using VoxelWorld;

public class WorldTests
{
    static object[] GetTestCaseDataForWorldPositionToCoordinates()
    {
        return new[] {
            new object[]{ new Vector3(1, 1, 1), new Vector3Int(0, 0, 0), new Vector3Int(1, 1, 1) },
            new object[]{ new Vector3(9, 9, 9), new Vector3Int(0, 0, 0), new Vector3Int(9, 9, 9) },
            new object[]{ new Vector3(10, 10, 10), new Vector3Int(10, 10, 10), new Vector3Int(0, 0, 0) },
            new object[]{ new Vector3(-1, -1, -1), new Vector3Int(-10, -10, -10), new Vector3Int(9, 9, 9) },
            new object[]{ new Vector3(-10, -10, -10), new Vector3Int(-10, -10, -10), new Vector3Int(0, 0, 0) },
            new object[]{ new Vector3(-11, -11, -11), new Vector3Int(-20, -20, -20), new Vector3Int(9, 9, 9) }
        };
    }

    [SetUp]
    public void SetUp()
    {
        WorldBuilder.chunkDimensions = new Vector3Int(10, 10, 10);
    }

    [TestCaseSource(nameof(GetTestCaseDataForWorldPositionToCoordinates))]
    public void World_position_to_coordinates_returns_correct_coordinates(
        Vector3 toBeTested,
        Vector3Int expectedChunkCoordinates,
        Vector3Int expectedBlockCoordinates)
    {
        (Vector3Int chunkCoordinates, Vector3Int blockCoordinates) = WorldUtils.FromWorldPosToCoordinates(toBeTested);

        Assert.That(chunkCoordinates, Is.EqualTo(expectedChunkCoordinates).Using(Vector3EqualityComparer.Instance));
        Assert.That(blockCoordinates, Is.EqualTo(expectedBlockCoordinates).Using(Vector3EqualityComparer.Instance));
    }
}
