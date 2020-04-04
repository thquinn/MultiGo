using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Background : MonoBehaviour
{
    public GameObject backgroundCirclePrefab;

    Dictionary<Tuple<int, int>, GameObject> circles;
    int xOff, yOff;

    // Start is called before the first frame update
    void Start()
    {
        circles = new Dictionary<Tuple<int, int>, GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        var deletedKeys = circles.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToArray();
        foreach (Tuple<int, int> deletedKey in deletedKeys) {
            if (circles[deletedKey] != null) {
                Destroy(circles[deletedKey]);
            }
            circles.Remove(deletedKey);
        }

        float scale = transform.localScale.x;
        transform.Translate(-.01f, -.0066f, 0);
        if (transform.localPosition.x < -scale) {
            transform.Translate(scale, 0, 0, Space.Self);
            xOff++;
            foreach (GameObject circle in circles.Values) {
                circle.transform.Translate(-scale, 0, 0);
            }
        }
        if (transform.localPosition.y < -transform.localScale.x) {
            transform.Translate(0, scale, 0, Space.Self);
            foreach (GameObject circle in circles.Values) {
                circle.transform.Translate(0, -scale, 0);
            }
            yOff++;
        }

        // Spawn.
        if (UnityEngine.Random.value < .5f) {
            return;
        }
        int x = UnityEngine.Random.Range(-10, 15);
        int y = UnityEngine.Random.Range(-6, 10);
        Tuple<int, int> coor = new Tuple<int, int>(x + xOff, y + yOff);
        if (!circles.ContainsKey(coor)) {
            GameObject circle = Instantiate(backgroundCirclePrefab, transform);
            circle.transform.localPosition = new Vector3(x, y);
            circles[coor] = circle;
        }
    }
}
