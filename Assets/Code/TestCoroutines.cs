namespace VoxelWorld
{
	using UnityEngine;
	using System.Collections;
	using Random = System.Random;

	public class TestCoroutines : MonoBehaviour
	{
		// Use this for initialization
		void Start()
		{
			//StartCoroutine(PrintNumbers());
			Random rng = new Random();

			Debug.Log($"{rng.Next()}");
		}

		IEnumerator PrintNumbers()
		{
			for (int i = 1; i <= 10; i++)
			{
				//yield return PrintNumber(i);
				yield return StartCoroutine(PrintNumber(i));
			}
		}

		IEnumerator PrintNumber(int i)
		{
			Debug.Log($"{Time.frameCount}:{i}");
			yield return new WaitForSeconds(0.1f);
		}
	}
}