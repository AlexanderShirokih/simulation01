using System.Collections;
using UnityEngine;

public class TestDestroying : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        var d = new Destoryable
        {
            version = 1,
            target = gameObject
        };

        StartCoroutine(DestroyTimer(d, 5f));
        StartCoroutine(Destruct(d));
    }

    class Destoryable
    {
        public int version;
        public GameObject target;
    }

    private IEnumerator Destruct(Destoryable go)
    {
        yield return new WaitForSeconds(2f);
        go.version++;
    }

    private IEnumerator DestroyTimer(Destoryable go, float duration)
    {
        var originalVersion = go.version;
        Debug.Log($"We're about to destroy; Orig={originalVersion}");
        yield return new WaitForSeconds(duration);
        var currentVersion = go.version;
        Debug.Log($"We're about to destroy; Current={currentVersion}");

        if (originalVersion != currentVersion) yield break;

        Destroy(go.target);
    }
}